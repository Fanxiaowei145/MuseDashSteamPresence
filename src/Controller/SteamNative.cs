using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace MuseDashSteamPresence
{
    internal sealed class SteamNative : IDisposable
    {
        [DllImport("kernel32", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr LoadLibraryExW(string path, IntPtr reserved, uint flags);
        [DllImport("kernel32", CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module, string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr CreateInterface([MarshalAs(UnmanagedType.LPStr)] string version, IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CreatePipe(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int ConnectUser(IntPtr self, int pipe);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void ReleaseUser(IntPtr self, int pipe, int user);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] delegate bool ReleasePipe(IntPtr self, int pipe);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr GetInterface(IntPtr self, int user, int pipe);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate uint AllocateShared(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void ReleaseShared(IntPtr self, uint connection);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] delegate bool SendMessage(IntPtr self, uint connection, IntPtr bytes, uint length);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr GetExtra(IntPtr self, ulong steamid);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] delegate bool GetPlayed(IntPtr self, ulong steamid, IntPtr info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] delegate bool GetCallback(int pipe, IntPtr message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void FreeCallback(int pipe);

        IntPtr client, shared, friends, callbackBuffer;
        int pipe, user;
        uint connection;
        readonly uint gameProcessId;
        ulong steamid;
        GetCallback getCallback;
        FreeCallback freeCallback;
        bool disposed;
        public SteamNative(int parentGamePid)
        {
            gameProcessId = checked((uint)parentGamePid);
            using (var game = Process.GetProcessById(parentGamePid))
                if (game.HasExited || !String.Equals(Path.GetFileName(game.MainModule.FileName), "MuseDash.exe", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Actual Muse Dash process is required.");
            try
            {
                string steamPath;
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                    steamPath = Convert.ToString(key == null ? null : key.GetValue("SteamPath"));
                if (String.IsNullOrWhiteSpace(steamPath)) throw new InvalidOperationException("Steam path not found.");
                string dll = Path.Combine(steamPath, "steamclient64.dll");
                string hash;
                using (var file = File.OpenRead(dll)) using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
                if (hash != SteamBuild.SupportedHash) throw new InvalidOperationException("Steam build changed; adapter disabled. SHA256=" + hash);
                var module = LoadLibraryExW(dll, IntPtr.Zero, 0x100 | 0x1000);
                if (module == IntPtr.Zero) throw new InvalidOperationException("Cannot load Steam IPC runtime.");
                getCallback = (GetCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module, "Steam_BGetCallback"), typeof(GetCallback));
                freeCallback = (FreeCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module, "Steam_FreeLastCallback"), typeof(FreeCallback));
                var create = (CreateInterface)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module, "CreateInterface"), typeof(CreateInterface));
                client = create("SteamClient020", IntPtr.Zero);
                var engine = create("CLIENTENGINE_INTERFACE_VERSION005", IntPtr.Zero);
                if (client == IntPtr.Zero || engine == IntPtr.Zero) throw new InvalidOperationException("Steam interface unavailable.");
                pipe = Fn<CreatePipe>(client, 0)(client);
                user = Fn<ConnectUser>(client, 2)(client, pipe);
                if (pipe == 0 || user == 0) throw new InvalidOperationException("Steam must be running and signed in.");
                shared = Fn<GetInterface>(engine, 69)(engine, user, pipe);
                friends = Fn<GetInterface>(engine, 13)(engine, user, pipe);
                if (shared == IntPtr.Zero || friends == IntPtr.Zero) throw new InvalidOperationException("Steam interfaces unavailable.");
                // The three wrappers were independently verified on disk and in
                // a successful external-process round-trip test. No guessed ABI.
                long[] expected = { 0x7379c0, 0x85d820, 0x8692b0 };
                for (int slot = 0; slot < expected.Length; slot++)
                    if (Marshal.ReadIntPtr(Marshal.ReadIntPtr(shared), slot * IntPtr.Size).ToInt64() - module.ToInt64() != expected[slot])
                        throw new InvalidOperationException("Shared connection ABI mismatch; no native message sent.");
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess"))
                {
                    uint account = Convert.ToUInt32(key.GetValue("ActiveUser"));
                    if (account == 0) throw new InvalidOperationException("Steam account is not signed in.");
                    steamid = 76561197960265728UL + account;
                }
                callbackBuffer = Marshal.AllocHGlobal(32);
                connection = Fn<AllocateShared>(shared, 0)(shared);
                if (connection == 0) throw new InvalidOperationException("Shared connection allocation failed.");
                PumpCallbacks();
            }
            catch { Dispose(); throw; }
        }
        static T Fn<T>(IntPtr obj, int index) where T : class
        {
            var address = Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj), index * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
        }
        public void PumpCallbacks()
        {
            if (callbackBuffer == IntPtr.Zero || pipe == 0) return;
            // Own pipe only; drain while idle to avoid callback-queue assertions.
            for (int count = 0; count < 1024 && getCallback(pipe, callbackBuffer); count++) freeCallback(pipe);
        }
        public ulong ReadCurrentGameId()
        {
            IntPtr info = Marshal.AllocHGlobal(24);
            try { return Fn<GetPlayed>(friends, 23)(friends, steamid, info) ? unchecked((ulong)Marshal.ReadInt64(info)) : 0; }
            finally { Marshal.FreeHGlobal(info); }
        }
        public string ReadCurrentTitle()
        {
            IntPtr value = Fn<GetExtra>(friends, 24)(friends, steamid);
            if (value == IntPtr.Zero) return "";
            int count = 0;
            while (count < 4096 && Marshal.ReadByte(value, count) != 0) count++;
            var bytes = new byte[count]; Marshal.Copy(value, bytes, 0, count);
            return Encoding.UTF8.GetString(bytes);
        }
        public bool TrySend(string title)
        {
            ulong current = ReadCurrentGameId();
            if (current != 0 && current != GamePresencePacket.AppId)
                throw new InvalidOperationException("Another game is currently displayed; leaving its state untouched.");
            byte[] packet = GamePresencePacket.Create(title, gameProcessId);
            IntPtr bytes = Marshal.AllocHGlobal(packet.Length);
            try
            {
                Marshal.Copy(packet, 0, bytes, packet.Length);
                return Fn<SendMessage>(shared, 2)(shared, connection, bytes, (uint)packet.Length);
            }
            finally { Marshal.FreeHGlobal(bytes); }
            // Confirmation is observed asynchronously by the controller. Waiting
            // here would block a newer selecting/playing event for up to 6s.
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                PumpCallbacks();
                if (connection != 0) { Fn<ReleaseShared>(shared, 1)(shared, connection); connection = 0; }
            }
            finally
            {
                if (client != IntPtr.Zero && pipe != 0)
                {
                    if (user != 0) Fn<ReleaseUser>(client, 4)(client, pipe, user);
                    Fn<ReleasePipe>(client, 1)(client, pipe); pipe = 0;
                }
                if (callbackBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(callbackBuffer); callbackBuffer = IntPtr.Zero; }
            }
            // Game exit is tracked by Steam's original game process. No fake
            // host, temporary shortcut, forced logon, or timer state publication.
        }
    }
}
