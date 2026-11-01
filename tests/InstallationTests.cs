using System;
using System.IO;
using MuseDashSteamPresence;

internal static class InstallationTests
{
    static int assertions;
    static void Check(bool ok, string reason) { assertions++; if (!ok) throw new Exception(reason); }
    static void Main()
    {
        string home = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"installation-fixtures");
        string support = InstallationFixtures.Create(home);
        string game = PluginInstallation.GameDirectory(support);
        string main = Path.Combine(game,"Mods","MuseDashSteamPresence.dll");
        string early = Path.Combine(game,"Plugins","MuseDashSteamPresence.Bootstrap.dll");
        string loader = Path.Combine(game,"MelonLoader","net6","MelonLoader.dll");
        Check(!PluginInstallation.Inspect(support).Ready,"No loader must not be ready.");
        InstallationFixtures.Touch(Path.Combine(game,"downloads","MelonLoader.Installer.exe"));
        Check(!PluginInstallation.Inspect(support).Ready,"Downloading an installer is not installation.");
        InstallationFixtures.Touch(Path.Combine(game,"downloads","Euterpe.exe"));
        Check(!PluginInstallation.Inspect(support).Ready,"Downloading another app is not loader installation.");
        InstallationFixtures.Emit(loader,"0.7.2.0");
        Check(!PluginInstallation.Inspect(support).Ready,"Untested loader must not be labelled verified.");
        InstallationFixtures.Emit(loader,"0.7.3.0");
        Check(PluginInstallation.Inspect(support).Ready,"Complete tested layout should pass file checks.");
        Check(PluginInstallation.CanStartEarly(game),"Complete install may start early.");
        PluginInstallation.SetEnabled(support,false);
        Check(File.Exists(main+".disabled") && File.Exists(early+".disabled"),"Independent disable affects both components.");
        Check(!PluginInstallation.CanStartEarly(game),"Disabled main must prevent bootstrap publication.");
        Check(!PluginInstallation.Inspect(support).Ready,"Disabled install must not be ready.");
        PluginInstallation.SetEnabled(support,true);
        Check(PluginInstallation.Inspect(support).Ready,"Independent enable restores both components.");
        File.Move(main,main+".disabled");
        Check(!PluginInstallation.CanStartEarly(game),"A mod-manager-only main disable must suppress early startup.");
        Check(PluginInstallation.Inspect(support).CanEnable,"Partial manager disable can be repaired independently.");
        PluginInstallation.SetEnabled(support,true);
        File.Copy(main,main+".disabled");
        bool rejected = false;
        try { PluginInstallation.SetEnabled(support,false); } catch { rejected = true; }
        Check(rejected && File.Exists(early) && !File.Exists(early+".disabled"),"Conflict must preflight before changing any component.");
        File.Delete(main+".disabled");
        File.Delete(main);
        Check(!PluginInstallation.Inspect(support).Ready && !PluginInstallation.CanStartEarly(game),"Missing main cannot publish early or pass readiness.");
        Check(!PluginInstallation.Inspect(Path.Combine(home,"wrong-location")).Ready,"Nested/incorrect extraction must be detected.");
        Console.WriteLine("PASS: " + assertions + " independent installation, loader, manager-toggle, both-component control and preflight checks; no Steam APIs.");
    }
}
