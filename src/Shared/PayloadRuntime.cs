using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MuseDashSteamPresence
{
    internal static class PayloadRuntime
    {
        static void StopOwnOldController(string root,Process game)
        {
            string session = SessionFiles.JoinOrCreate(root,game);
            string stem = Path.Combine(root,"state",session);
            try
            {
                var saved = File.ReadAllLines(stem+".controller");
                using (var process = Process.GetProcessById(Int32.Parse(saved[0])))
                {
                    if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != Int64.Parse(saved[1]) ||
                        !String.Equals(process.MainModule.FileName,Path.Combine(root,"PresenceController.exe"),StringComparison.OrdinalIgnoreCase)) return;
                    File.WriteAllText(stem+".stop","Updating this plugin's embedded controller.");
                    if (!process.WaitForExit(1800)) throw new IOException("Close the old presence controller and restart the game to finish updating.");
                }
            }
            catch (FileNotFoundException) {}
            catch (DirectoryNotFoundException) {}
            catch (ArgumentException) {}
            if (File.Exists(stem+".stop")) File.Delete(stem+".stop");
        }
        internal static bool Prepare(Process game,Action<string> log)
        {
            string gameRoot = Path.GetDirectoryName(game.MainModule.FileName);
            return PrepareAt(gameRoot,game,log);
        }
        // Explicit root permits an isolated file/process-identity probe. The
        // two production callbacks always use Prepare(game), never a supplied root.
        internal static bool PrepareAt(string gameRoot,Process game,Action<string> log)
        {
            string root = Path.Combine(gameRoot,"UserData","MuseDashSteamPresence");
            string warning = Path.Combine(root,"deployment-warning.txt");
            try
            {
                string main = Path.Combine(gameRoot,"Mods","MuseDashSteamPresence.dll");
                var plan = PayloadDeployment.Validate(PayloadArchiveReader.Read(main));
                if (PayloadDeployment.NeedsFile(gameRoot,plan,"UserData/MuseDashSteamPresence/PresenceController.exe")) StopOwnOldController(root,game);
                int changed = PayloadDeployment.Install(gameRoot,plan);
                if (File.Exists(warning)) File.Delete(warning);
                if (changed > 0) log("Prepared " + changed + " bundled files locally; no network download.");
                return true;
            }
            catch (Exception e)
            {
                try { Directory.CreateDirectory(root); File.WriteAllText(warning,"Close the settings window and restart Muse Dash to finish preparing bundled files.\n"+e.Message); } catch {}
                log("Bundled setup deferred: " + e.Message); return false;
            }
        }
        internal static void StartSelection(Process game)
        {
            string root = Path.Combine(Path.GetDirectoryName(game.MainModule.FileName),"UserData","MuseDashSteamPresence");
            string session = SessionFiles.JoinOrCreate(root,game);
            SessionFiles.WriteRequest(Path.Combine(root,"state",session),false,"","");
            using (SessionFiles.EnsureController(root,session,game)) {}
        }
    }
}
