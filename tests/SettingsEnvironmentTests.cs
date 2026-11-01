using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MuseDashSteamPresence;

// Real GUI logic, unshown forms, deterministic hash check. Never link SteamNative.
namespace MuseDashSteamPresence
{
    internal static class SteamBuild
    {
        internal static bool Supported = true;
        internal static bool IsSupported(out string reason) { reason = Supported ? "" : "当前 Steam 版本尚未适配"; return Supported; }
    }
}
internal static class SettingsEnvironmentTests
{
    static object Field(object o,string name) { return o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o); }
    static void Check(bool ok,string why) { if (!ok) throw new Exception(why); }
    static string Inspect(string support)
    {
        using (var form = new SettingsWindow(support))
        {
            Check(((Label)Field(form,"songPreview")).Text.Contains("ものふぉびあ"),"Preview should use the requested official song.");
            ((TextBox)Field(form,"idleText")).Text = "先保存，再开游戏……";
            bool saved = (bool)form.GetType().GetMethod("SaveSettings",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);
            Check(saved && PresenceSettings.Load(Path.Combine(support,"settings.xml")).IdleText == "先保存，再开游戏……","Offline settings save must work in every environment.");
            return ((Label)Field(form,"connection")).Text;
        }
    }
    [STAThread] static void Main()
    {
        Application.EnableVisualStyles();
        string home = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"gui-fixtures");
        string support = InstallationFixtures.Create(home);
        string game = PluginInstallation.GameDirectory(support);
        Check(Inspect(support).Contains("未找到 MelonLoader"),"GUI must explain missing loader, never falsely ready.");
        InstallationFixtures.Touch(Path.Combine(game,"downloads","Euterpe.exe"));
        Check(Inspect(support).Contains("未找到 MelonLoader"),"Another downloaded app is not sufficient.");
        InstallationFixtures.Touch(Path.Combine(game,"downloads","MelonLoader.Installer.exe"));
        Check(Inspect(support).Contains("未找到 MelonLoader"),"Downloaded-only installer must not pass.");
        InstallationFixtures.Emit(Path.Combine(game,"MelonLoader","net6","MelonLoader.dll"),"0.7.3.0");
        Check(Inspect(support).Contains("文件检查通过") && Inspect(support).Contains("游戏未启动"),"Complete environment must distinguish file readiness and game runtime.");
        PluginInstallation.SetEnabled(support,false);
        Check(Inspect(support).Contains("插件已停用"),"GUI must show independently disabled state.");
        PluginInstallation.SetEnabled(support,true);
        string main = Path.Combine(game,"Mods","MuseDashSteamPresence.dll");
        File.Move(main,main+".disabled");
        Check(Inspect(support).Contains("未完全启用"),"Partial external-manager disable must be visible.");
        PluginInstallation.SetEnabled(support,true);
        SteamBuild.Supported = false;
        Check(Inspect(support).Contains("尚未适配"),"Unsupported Steam must be visible without blocking settings save.");
        SteamBuild.Supported = true;
        File.Delete(Path.Combine(game,"Plugins","MuseDashSteamPresence.Bootstrap.dll"));
        Check(Inspect(support).Contains("文件不完整"),"Missing early plugin must be detected.");
        Console.WriteLine("PASS: real settings window handles pure Steam, downloaded-only apps, tested loader, independent toggles, missing files and unsupported Steam; settings save before game; no windows shown or Steam APIs.");
    }
}
