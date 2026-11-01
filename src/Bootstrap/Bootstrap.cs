using System;
using System.Diagnostics;
using System.IO;
using MelonLoader;

[assembly: MelonInfo(typeof(MuseDashSteamPresence.EarlyPresence), "Muse Dash Steam Presence Early Start", "0.5.1", "樊小胃")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]

namespace MuseDashSteamPresence
{
    public sealed class EarlyPresence : MelonPlugin
    {
        bool initialized;
        public override void OnPreInitialization() { StartEarly(); }
        public override void OnEarlyInitializeMelon() { StartEarly(); }
        public override void OnApplicationEarlyStart() { StartEarly(); }
        void StartEarly()
        {
            if (initialized) return;
            try
            {
                using (var game = Process.GetCurrentProcess())
                {
                    string gameDirectory = Path.GetDirectoryName(game.MainModule.FileName);
                    string root = Path.Combine(gameDirectory, "UserData", "MuseDashSteamPresence");
                    // A mod manager may disable only the main Mods DLL. Do not
                    // leave a standalone early component publishing menu text.
                    if (!File.Exists(Path.Combine(gameDirectory,"Mods","MuseDashSteamPresence.dll"))) return;
                    if (!PayloadRuntime.Prepare(game,LoggerInstance.Msg) || !PluginInstallation.CanStartEarly(gameDirectory)) return;
                    string session = SessionFiles.JoinOrCreate(root, game);
                    // Write before starting the helper so its first request is
                    // available without waiting for any Unity scene or game DB.
                    SessionFiles.WriteRequest(Path.Combine(root, "state", session), false, "", "");
                    using (SessionFiles.EnsureController(root, session, game)) {}
                    initialized = true;
                    LoggerInstance.Msg("Early non-playing state requested; display follows settings. No Unity/game database dependency.");
                }
            }
            catch (Exception e) { LoggerInstance.Warning("Early presence deferred: " + e.Message); }
        }
    }
}
