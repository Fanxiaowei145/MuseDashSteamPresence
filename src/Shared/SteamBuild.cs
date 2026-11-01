using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace MuseDashSteamPresence
{
    internal static class SteamBuild
    {
        internal const string SupportedHash = "253E0A11A4415B38C78A42F7BDD6ECC426455F06C1BA6453A48090DF3142DBBC";
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
                    if (hash != SupportedHash) { reason = "当前 Steam 版本尚未适配，状态功能会停止发送"; return false; }
                }
                reason = ""; return true;
            }
            catch { reason = "无法核对 Steam 版本，请检查安装或文件权限"; return false; }
        }
    }
}
