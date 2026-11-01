using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MuseDashSteamPresence
{
    // Shared by the early plugin and regular mod without loading game/Unity types.
    internal static class SessionFiles
    {
        internal static string JoinOrCreate(string root, Process game)
        {
            string id = game.Id + "-" + game.StartTime.ToUniversalTime().Ticks;
            Directory.CreateDirectory(Path.Combine(root, "state"));
            using (var gate = new Mutex(false, @"Local\MuseDashSteamPresence.Session." + id))
            {
                try { gate.WaitOne(); } catch (AbandonedMutexException) {}
                try
                {
                    string path = Path.Combine(root, "state", "launch-" + id + ".txt");
                    Guid existing;
                    if (File.Exists(path) && Guid.TryParseExact(File.ReadAllText(path).Trim(), "N", out existing)) return existing.ToString("N");
                    string session = Guid.NewGuid().ToString("N");
                    AtomicWrite(path, session);
                    return session;
                }
                finally { gate.ReleaseMutex(); }
            }
        }
        internal static Process EnsureController(string root, string session, Process game)
        {
            using (var gate = new Mutex(false, @"Local\MuseDashSteamPresence.Start." + session))
            {
                try { gate.WaitOne(); } catch (AbandonedMutexException) {}
                try
                {
                    string path = Path.Combine(root, "state", session + ".controller");
                    string exe = Path.Combine(root, "PresenceController.exe");
                    try
                    {
                        var saved = File.ReadAllLines(path);
                        var active = Process.GetProcessById(Int32.Parse(saved[0]));
                        if (!active.HasExited && active.StartTime.ToUniversalTime().Ticks == Int64.Parse(saved[1]) &&
                            String.Equals(active.MainModule.FileName, exe, StringComparison.OrdinalIgnoreCase)) return active;
                        active.Dispose();
                    }
                    catch {}
                    var process = Process.Start(new ProcessStartInfo {
                        FileName = exe,
                        Arguments = game.Id + " " + game.StartTime.ToUniversalTime().Ticks + " " + session,
                        WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    AtomicWrite(path, process.Id + "\n" + process.StartTime.ToUniversalTime().Ticks);
                    return process;
                }
                finally { gate.ReleaseMutex(); }
            }
        }
        internal static void WriteRequest(string stem, bool playing, string song, string author)
        {
            string request = Guid.NewGuid().ToString("N") + "\n" + Environment.TickCount64 + "\n" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(playing ? song : "")) + "\n" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(playing ? author : "")) + "\n" + (playing ? "playing" : "selecting");
            AtomicWrite(stem + ".request", request);
        }
        internal static void AtomicWrite(string path, string text)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, text, Encoding.UTF8);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
