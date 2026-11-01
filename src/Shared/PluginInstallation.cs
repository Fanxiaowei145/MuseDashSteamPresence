using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MuseDashSteamPresence
{
    internal sealed class InstallationState
    {
        internal string GameDirectory, Message;
        internal bool Ready, CanEnable, CanDisable;
    }
    internal static class PluginInstallation
    {
        internal static string GameDirectory(string support)
        {
            var dir = new DirectoryInfo(Path.GetFullPath(support));
            if (!dir.Name.Equals("MuseDashSteamPresence", StringComparison.OrdinalIgnoreCase) || dir.Parent == null ||
                !dir.Parent.Name.Equals("UserData", StringComparison.OrdinalIgnoreCase) || dir.Parent.Parent == null) return null;
            string game = dir.Parent.Parent.FullName;
            return File.Exists(Path.Combine(game, "musedash.exe")) ? game : null;
        }
        internal static bool CanStartEarly(string game)
        {
            return File.Exists(Path.Combine(game, "Mods", "MuseDashSteamPresence.dll")) &&
                File.Exists(Path.Combine(game, "UserData", "MuseDashSteamPresence", "PresenceController.exe"));
        }
        static string[] Plugins(string game)
        {
            return new[] { Path.Combine(game,"Mods","MuseDashSteamPresence.dll"), Path.Combine(game,"Plugins","MuseDashSteamPresence.Bootstrap.dll") };
        }
        internal static InstallationState Inspect(string support)
        {
            var s = new InstallationState { GameDirectory = GameDirectory(support) };
            if (s.GameDirectory == null) { s.Message = "目录不正确：请把全部文件解压到 musedash.exe 同级"; return s; }
            string[] plugins = Plugins(s.GameDirectory);
            bool disabled = false, active = false;
            foreach (string path in plugins)
            {
                bool yes = File.Exists(path), no = File.Exists(path + ".disabled");
                if (yes && no) { s.Message = "存在重复启用／停用副本，请先备份并整理插件文件"; return s; }
                if (!yes && !no) { s.Message = "文件不完整：缺少 " + Path.GetFileName(path); return s; }
                active |= yes; disabled |= no;
            }
            if (!File.Exists(Path.Combine(support,"PresenceController.exe"))) { s.Message = "文件不完整：缺少 PresenceController.exe"; return s; }
            s.CanEnable = disabled; s.CanDisable = active;
            if (disabled) { s.Message = active ? "插件未完全启用；可点击“启用插件”恢复两个组件" : "插件已停用；仍可保存设置"; return s; }
            string loader = Path.Combine(s.GameDirectory,"MelonLoader","net6","MelonLoader.dll");
            if (!File.Exists(loader)) { s.Message = "未找到 MelonLoader net6；请先安装加载器（设置仍可保存）"; return s; }
            try
            {
                var name = AssemblyName.GetAssemblyName(loader);
                if (name.Name != "MelonLoader" || name.Version.Major != 0 || name.Version.Minor != 7 || name.Version.Build != 3)
                { s.Message = "MelonLoader " + name.Version + " 尚未验证；已验证版本为 0.7.3"; return s; }
            }
            catch { s.Message = "无法识别 MelonLoader 文件；请检查加载器是否完整"; return s; }
            s.Ready = true; s.Message = "文件检查通过（MelonLoader 0.7.3）；不代表已同步到 Steam"; return s;
        }
        internal static bool GameRunning(string game)
        {
            foreach (var p in Process.GetProcessesByName("musedash"))
            {
                using (p)
                {
                    try { if (!p.HasExited && String.Equals(Path.GetDirectoryName(p.MainModule.FileName), game, StringComparison.OrdinalIgnoreCase)) return true; }
                    catch { return true; }
                }
            }
            return false;
        }
        internal static void SetEnabled(string support, bool enabled)
        {
            var state = Inspect(support);
            if (state.GameDirectory == null) throw new InvalidOperationException(state.Message);
            if (GameRunning(state.GameDirectory)) throw new InvalidOperationException("请先退出 Muse Dash；本操作不能热卸载运行中的插件。");
            var moves = new List<string[]>();
            string[] expected = { "MuseDashSteamPresence", "MuseDashSteamPresence.Bootstrap" };
            var paths = Plugins(state.GameDirectory);
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i], off = path + ".disabled";
                if (File.Exists(path) && File.Exists(off)) throw new InvalidOperationException("存在重复副本；未更改文件。" );
                string existing = File.Exists(path) ? path : File.Exists(off) ? off : null;
                if (existing == null) throw new InvalidOperationException("缺少插件文件：" + Path.GetFileName(path));
                if (AssemblyName.GetAssemblyName(existing).Name != expected[i]) throw new InvalidOperationException("无法识别插件文件：" + Path.GetFileName(existing));
                string source = enabled ? off : path, target = enabled ? path : off;
                if (File.Exists(source)) moves.Add(new[] { source, target });
            }
            int done = 0;
            try { foreach (var move in moves) { File.Move(move[0],move[1]); done++; } }
            catch
            {
                for (int i = done - 1; i >= 0; i--) File.Move(moves[i][1],moves[i][0]);
                throw;
            }
        }
        internal static string RuntimeMessage(string support, string game)
        {
            foreach (var p in Process.GetProcessesByName("musedash"))
            {
                using (p)
                {
                    try
                    {
                        if (p.HasExited || !String.Equals(Path.GetDirectoryName(p.MainModule.FileName),game,StringComparison.OrdinalIgnoreCase)) continue;
                        string launch = Path.Combine(support,"state","launch-" + p.Id + "-" + p.StartTime.ToUniversalTime().Ticks + ".txt");
                        Guid id;
                        if (File.Exists(launch) && Guid.TryParseExact(File.ReadAllText(launch).Trim(),"N",out id))
                        {
                            string stem = Path.Combine(support,"state",id.ToString("N"));
                            string livePid = stem + ".controller";
                            var identity = File.ReadAllLines(livePid);
                            using (var helper = Process.GetProcessById(Int32.Parse(identity[0])))
                            {
                                if (helper.HasExited || helper.StartTime.ToUniversalTime().Ticks != Int64.Parse(identity[1]) ||
                                    !String.Equals(helper.MainModule.FileName,Path.Combine(support,"PresenceController.exe"),StringComparison.OrdinalIgnoreCase)) return "游戏运行中；辅助程序已退出，请检查日志";
                            }
                            string status = File.ReadAllLines(stem + ".status")[0];
                            if (status == "published") return "已收到 Steam 本机缓存确认（好友显示请以实机为准）";
                            if (status == "send-failed") return "Steam 未接受状态；请检查日志";
                            return "状态已提交；等待完整缓存确认";
                        }
                        return "游戏运行中；等待插件回报";
                    }
                    catch { return "游戏运行中；尚未确认状态，请检查日志"; }
                }
            }
            return "游戏未启动；可先保存设置，再从 Steam 启动";
        }
    }
}
