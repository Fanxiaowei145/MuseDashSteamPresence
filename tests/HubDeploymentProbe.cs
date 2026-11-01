using System;
using System.IO;
using System.Diagnostics;
using MuseDashSteamPresence;

internal static class HubDeploymentProbe
{
    static void Check(bool value,string why) { if (!value) throw new Exception(why); }
    static void Main(string[] args)
    {
        // Run under a stock .NET host. Read the actual built mod as PE data;
        // never load its code, MelonLoader, game assemblies or Steam.
        byte[] archive = PayloadArchiveReader.Read(args[0]);
        var entries = PayloadDeployment.Validate(archive);
        string game = Path.GetFullPath(args[1]);
        if (Directory.Exists(game) || File.Exists(game)) throw new InvalidOperationException("A new isolated fixture directory is required; never pass an actual game installation.");
        Directory.CreateDirectory(Path.Combine(game,"Mods"));
        File.WriteAllText(Path.Combine(game,"musedash.exe"),"fixture game; never execute");
        File.Copy(args[0],Path.Combine(game,"Mods","MuseDashSteamPresence.dll"));
        string support = Path.Combine(game,"UserData","MuseDashSteamPresence");
        Directory.CreateDirectory(support); File.WriteAllText(Path.Combine(support,"settings.xml"),"user config sentinel");
        Check(!File.Exists(Path.Combine(support,"PresenceController.exe")),"Fixture must model Hub's main-DLL-only install.");
        int installed = PayloadDeployment.Install(game,entries);
        Check(installed>3 && File.Exists(Path.Combine(game,"Plugins","MuseDashSteamPresence.Bootstrap.dll")) &&
            File.Exists(Path.Combine(support,"PresenceController.exe")) && File.Exists(Path.Combine(support,"MuseDashPresenceSettings.exe")) &&
            File.Exists(Path.Combine(game,"MuseDash-Settings.cmd")),"Main-DLL-only install must prepare the complete independent application.");
        Check(File.ReadAllText(Path.Combine(support,"settings.xml"))=="user config sentinel","Existing settings must survive Hub deployment.");
        Check(PayloadDeployment.Install(game,entries)==0,"Second preparation must not rewrite unchanged files.");
        foreach (var entry in entries)
            Check(PayloadDeployment.Hash(File.ReadAllBytes(Path.Combine(game,entry.Name)))==entry.Hash,"Deployed file differs from embedded manifest.");
        using (var parent = Process.GetCurrentProcess())
        {
            string session = SessionFiles.JoinOrCreate(support,parent);
            string stem = Path.Combine(support,"state",session);
            string helperPath = Path.Combine(support,"PresenceController.exe");
            File.Copy(args[2],helperPath,true);
            using (var old = Process.Start(new ProcessStartInfo { FileName=helperPath,Arguments="\""+stem+".stop\"",UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden }))
            {
                try
                {
                    File.WriteAllText(stem+".controller",old.Id+"\n"+old.StartTime.ToUniversalTime().Ticks);
                    Check(PayloadRuntime.PrepareAt(game,parent,Console.WriteLine),"A validated old own-controller stub must stop gracefully and update.");
                    Check(old.WaitForExit(2500),"Old own-controller stub did not exit after its stop marker.");
                    Check(!File.Exists(stem+".stop"),"Update must clear its old stop marker before the next real start.");
                }
                finally { if (!old.HasExited) { File.WriteAllText(stem+".stop","test cleanup"); old.WaitForExit(3000); } }
            }
            Check(!PayloadRuntime.PrepareAt(game+"-missing",parent,Console.WriteLine),"Invalid roots must return a controlled deployment failure.");
        }
        foreach (var entry in entries) Check(PayloadDeployment.Hash(File.ReadAllBytes(Path.Combine(game,entry.Name)))==entry.Hash,"Updated payload differs from manifest.");
        Console.WriteLine("PASS: actual Hub main DLL read as PE data; " + installed + " files prepared, hashes match, config preserved, second run idempotent, own old stub gracefully stopped, failure reported; no real game, production controller or Steam APIs executed.");
    }
}
