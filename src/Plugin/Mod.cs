using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MelonLoader;
using Il2CppFormulaBase;
using Il2CppAssets.Scripts.Database;
using UnityEngine.SceneManagement;

[assembly: AssemblyVersion("0.5.1.0")]
[assembly: AssemblyFileVersion("0.5.1.0")]
[assembly: MelonInfo(typeof(MuseDashSteamPresence.Entry), "Muse Dash Steam Song Presence", "0.5.1", "樊小胃")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]

namespace MuseDashSteamPresence
{
    public sealed class Entry : MelonMod
    {
        static Entry instance;
        string session;
        string root, stem;
        Process controller;
        bool stopping, ready, hasState, playing;
        StageBattleComponent battle;
        string battleScene, song = "", author = "", lastSong = "";
        long nextBattleCheck;
        public override void OnEarlyInitializeMelon()
        {
            try
            {
                using (var game = Process.GetCurrentProcess())
                    if (PayloadRuntime.Prepare(game,LoggerInstance.Msg)) PayloadRuntime.StartSelection(game);
            }
            catch (Exception e) { LoggerInstance.Warning("First-run setup deferred: " + e.Message); }
        }
        public override void OnInitializeMelon()
        {
            instance = this;
            string game = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            root = Path.Combine(game, "UserData", "MuseDashSteamPresence");
            using (var current = Process.GetCurrentProcess())
                if (!PayloadRuntime.Prepare(current,LoggerInstance.Msg)) return;
            Directory.CreateDirectory(Path.Combine(root, "state"));
            using (var current = Process.GetCurrentProcess()) session = SessionFiles.JoinOrCreate(root, current);
            stem = Path.Combine(root, "state", session);
            if (!File.Exists(Path.Combine(root, "PresenceController.exe")))
            { LoggerInstance.Error("Supporting controller missing; disabled."); return; }
            Patch("GameStart", "SongStarted", false);
            Patch("End", "BattleEnded", true);
            Patch("Exit", "BattleEnded", true);
            Patch("GameRestart", "BattleEnded", true);
            ready = true;
            SetSelecting("startup");
            LoggerInstance.Msg("Ready: default playing=song only; non-playing=ordinary Muse Dash. Optional text/author follow settings; real Steam AppID 774171.");
        }
        void Patch(string targetName, string callbackName, bool prefix)
        {
            var target = AccessTools.Method(typeof(StageBattleComponent), targetName);
            if (target == null) throw new MissingMethodException(typeof(StageBattleComponent).FullName, targetName);
            var callback = new HarmonyMethod(typeof(Entry).GetMethod(callbackName, BindingFlags.Static | BindingFlags.NonPublic));
            HarmonyInstance.Patch(target, prefix: prefix ? callback : null, postfix: prefix ? null : callback);
        }
        static void SongStarted(StageBattleComponent __instance)
        {
            var self = instance;
            if (self == null || !self.ready || self.stopping) return;
            try
            {
                var db = GlobalDataBase.s_DbBattleStage;
                if (db == null) return;
                string name, artist;
                bool custom = EuterpeSongResolver.TryGetDetails(db.musicUid, out name, out artist);
                if (!custom)
                {
                    var info = db.selectedMusicInfo;
                    if (info != null) { name = info.name; artist = info.author; }
                }
                if (String.IsNullOrWhiteSpace(name)) name = db.selectedMusicName;
                name = PublicationPolicy.Normalize(name);
                if (name.Length == 0)
                {
                    self.battle = null;
                    self.SetSelecting("song metadata unavailable");
                    return;
                }
                self.battle = __instance;
                self.battleScene = SceneManager.GetActiveScene().name;
                self.song = name;
                self.author = PublicationPolicy.Normalize(artist);
                self.SendState(true, custom ? "Euterpe start" : "game start");
            }
            catch (Exception e)
            {
                self.battle = null;
                self.SetSelecting("start could not be read");
                self.LoggerInstance.Warning("Song presence skipped: " + e.Message);
            }
        }
        static void BattleEnded()
        {
            var self = instance;
            if (self == null || !self.ready || self.stopping) return;
            self.battle = null;
            self.battleScene = null;
            self.SetSelecting("end/exit/restart");
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (ready && battle == null) SetSelecting("scene initialized");
        }
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (!ready || stopping) return;
            if (battle != null && String.Equals(sceneName, battleScene, StringComparison.Ordinal)) BattleEnded();
            else if (battle == null) SetSelecting("scene unloaded");
        }
        public override void OnUpdate()
        {
            if (!ready || stopping || battle == null) return;
            long now = Environment.TickCount64;
            if (now < nextBattleCheck) return;
            nextBattleCheck = now + 250;
            // Observe gameplay/pause flags on Unity's main thread. This timer
            // detects transitions only; it never republishes an unchanged state.
            try { SendState(battle.isInGame && !battle.isPause, "gameplay/pause flags"); }
            catch
            {
                battle = null;
                SetSelecting("battle object released");
            }
        }
        void SetSelecting(string reason) { SendState(false, reason); }
        void SendState(bool inSong, string reason)
        {
            if (!ready || stopping) return;
            if (hasState && playing == inSong && (!inSong || String.Equals(lastSong, song, StringComparison.OrdinalIgnoreCase))) return;
            try
            {
                if (controller == null || controller.HasExited)
                {
                    var current = Process.GetCurrentProcess();
                    controller = SessionFiles.EnsureController(root, session, current);
                }
                SessionFiles.WriteRequest(stem, inSong, song, author);
                hasState = true;
                playing = inSong;
                lastSong = inSong ? song : "";
                LoggerInstance.Msg("Requested [" + reason + "]: " + (inSong ? "playing metadata: " + song : "non-playing") + "; displayed text follows settings.");
            }
            catch (Exception e) { LoggerInstance.Warning("Presence request skipped: " + e.Message); }
        }
        public override void OnApplicationQuit()
        {
            stopping = true;
            try { if (stem != null) File.WriteAllText(stem + ".stop", "stop"); } catch {}
        }
    }
}
