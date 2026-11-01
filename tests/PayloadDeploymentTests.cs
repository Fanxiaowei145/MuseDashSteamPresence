using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using MuseDashSteamPresence;

internal static class PayloadDeploymentTests
{
    const string Prefix = "UserData/MuseDashSteamPresence/";
    static int count;
    static void Check(bool value,string why) { count++; if (!value) throw new Exception(why); }
    static byte[] Bundle(string controller,string gui,string extra,bool tamper)
    {
        var entries = new List<KeyValuePair<string,byte[]>> {
            new KeyValuePair<string,byte[]>(Prefix+"PresenceController.exe",Encoding.UTF8.GetBytes(controller)),
            new KeyValuePair<string,byte[]>(Prefix+"MuseDashPresenceSettings.exe",Encoding.UTF8.GetBytes(gui)),
            new KeyValuePair<string,byte[]>("Plugins/MuseDashSteamPresence.Bootstrap.dll",new byte[]{1,2,3}),
            new KeyValuePair<string,byte[]>("MuseDash-Settings.cmd",Encoding.UTF8.GetBytes("fixture entry"))
        };
        if (extra != null) entries.Add(new KeyValuePair<string,byte[]>(extra,new byte[]{7}));
        using (var buffer = new MemoryStream())
        {
            using (var zip = new ZipArchive(buffer,ZipArchiveMode.Create,true))
            {
                var manifest = new StringBuilder();
                foreach (var entry in entries)
                {
                    using (var stream = zip.CreateEntry(entry.Key).Open()) stream.Write(entry.Value,0,entry.Value.Length);
                    manifest.Append((tamper ? new string('0',64) : PayloadDeployment.Hash(entry.Value))+"\t"+entry.Key+"\n");
                }
                using (var writer = new StreamWriter(zip.CreateEntry("payload-manifest.tsv").Open(),new UTF8Encoding(false))) writer.Write(manifest.ToString());
            }
            return buffer.ToArray();
        }
    }
    static void Reject(byte[] bytes,string why)
    {
        bool failed = false; try { PayloadDeployment.Validate(bytes); } catch (InvalidDataException) { failed = true; }
        Check(failed,why);
    }
    static void Main()
    {
        try { Run(); }
        catch (Exception e) { Console.WriteLine("FAIL: " + e.GetType().Name + ": " + e.Message); Environment.ExitCode=1; }
    }
    static void Run()
    {
        string game = Path.Combine(Path.GetTempPath(),"mdsp-deploy-"+Guid.NewGuid().ToString("N").Substring(0,12));
        string support = Path.Combine(game,"UserData","MuseDashSteamPresence");
        Directory.CreateDirectory(support); Directory.CreateDirectory(Path.Combine(game,"Mods"));
        File.WriteAllText(Path.Combine(game,"musedash.exe"),"game sentinel");
        File.WriteAllText(Path.Combine(game,"Mods","other-mod.dll"),"other sentinel");
        File.WriteAllText(Path.Combine(support,"settings.xml"),"my config");
        var plan = PayloadDeployment.Validate(Bundle("controller one","gui one",null,false));
        Check(PayloadDeployment.Install(game,plan)==4,"First single-DLL deployment prepares all required files.");
        Check(File.ReadAllText(Path.Combine(support,"settings.xml"))=="my config","User configuration must be preserved.");
        Check(File.ReadAllText(Path.Combine(game,"musedash.exe"))=="game sentinel" && File.ReadAllText(Path.Combine(game,"Mods","other-mod.dll"))=="other sentinel","Game and other mods must be untouched.");
        Check(PayloadDeployment.Install(game,plan)==0,"Unchanged payload must be idempotent.");
        Reject(Bundle("a","b","../escape.exe",false),"Traversal must be rejected.");
        Reject(Bundle("a","b",Prefix+"settings.xml",false),"Config overwrite must be rejected.");
        Reject(Bundle("a","b",Prefix+"state/evil.txt",false),"Session-data overwrite must be rejected.");
        Reject(Bundle("a","b",Prefix+"docs/unlisted.exe",false),"Arbitrary executable paths must be rejected.");
        Reject(Bundle("a","b",null,true),"Hash mismatch must reject the entire plan.");
        var next = PayloadDeployment.Validate(Bundle("controller two","gui two",null,false));
        string gui = Path.Combine(support,"MuseDashPresenceSettings.exe");
        bool blocked = false;
        using (var locked = File.Open(gui,FileMode.Open,FileAccess.Read,FileShare.Read))
            try { PayloadDeployment.Install(game,next); } catch (IOException) { blocked = true; }
        Check(blocked,"Locked GUI must produce a controlled deployment error.");
        Check(File.ReadAllText(Path.Combine(support,"PresenceController.exe"))=="controller one" && File.ReadAllText(gui)=="gui one","A commit error must roll back previously replaced files.");
        Check(PayloadDeployment.Install(game,next)==2,"Closed GUI allows an atomic upgrade.");
        Check(Directory.GetFiles(Path.Combine(support,"backups"),"*PresenceController.exe",SearchOption.AllDirectories).Length>0,"Replaced payload must be recoverably backed up.");
        string early = Path.Combine(game,"Plugins","MuseDashSteamPresence.Bootstrap.dll");
        File.Move(early,early+".disabled");
        Check(PayloadDeployment.Install(game,next)==0 && !File.Exists(early),"An intentional early-component disable must not be undone.");
        Console.WriteLine("PASS: " + count + " embedded deployment first-install, repair, hashes, path boundaries, config preservation, lock rollback, backups and disabled-state checks; no Steam or process launch.");
    }
}
