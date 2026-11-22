using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace MuseDashSteamPresence
{
    internal static class SteamBuild
    {
        internal const string SupportedHash = "253E0A11A4415B38C78A42F7BDD6ECC426455F06C1BA6453A48090DF3142DBBC";
        internal const string NovemberHash = "669FA08E259277FBF5A1DFE15CE88AFE734E89151194304B44D92F418DCC985B";
        internal sealed class Profile
        {
            internal readonly long[] Shared, Engine, Friends;
            internal Profile(long[] shared, long[] engine, long[] friends)
            { Shared = shared; Engine = engine; Friends = friends; }
        }
        // Explicit profiles preserve old builds without allowing unknown native ABIs.
        internal static Profile FindProfile(string hash)
        {
            if (hash == SupportedHash) return new Profile(new long[] {0x7379c0,0x85d820,0x8692b0},
                new long[] {0x981a10,0x982d10}, new long[] {0x781580,0x781720});
            if (hash == NovemberHash) return new Profile(new long[] {0x74e460,0x881400,0x88d020},
                new long[] {0x9a8850,0x9a9b50}, new long[] {0x798060,0x798200});
            return null;
        }
        // File-only check. This does not load Steam or call a native interface.
        internal static bool IsSupported(out string reason)
        {
            try
            {
                string path;
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                    path = key == null ? null : key.GetValue("SteamPath") as string;
                if (String.IsNullOrWhiteSpace(path)) { reason = "未找到 Steam 安装目录"; return false; }
                using (var file = File.OpenRead(Path.Combine(path, "steamclient64.dll")))
                using (var sha = SHA256.Create())
                {
                    string hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
                    if (FindProfile(hash) == null) { reason = "Steam 已更新，需更新本模组的兼容适配（无需重装游戏或更改设置）"; return false; }
                }
                reason = ""; return true;
            }
            catch { reason = "无法核对 Steam 版本，请检查安装或文件权限"; return false; }
        }
    }
}
