using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MuseDashSteamPresence;

internal static class PresenceController
{
    [DllImport("kernel32")] static extern ulong GetTickCount64();
    static string log;
    static void Log(string text) { try { File.AppendAllText(log, DateTime.Now.ToString("s") + " " + text + Environment.NewLine, Encoding.UTF8); } catch {} }
    static string Text(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
    static string B64(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? "")); }
    static void AtomicWrite(string path, string contents)
    {
        string temp = path + ".tmp";
        File.WriteAllText(temp, contents, Encoding.UTF8);
        if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
    }
    static void Ledger(string stem, PublicationPolicy policy, long attempt, string delivery, string author, string title, string fingerprint)
    {
        AtomicWrite(stem + ".ledger", attempt + "\n" + policy.LastPublicationMilliseconds + "\n" + B64(policy.DisplayedSong) +
            "\n" + (policy.IsPlaying ? "playing" : "selecting") + "\n" + delivery + "\n" + B64(author) + "\n" + B64(title) + "\n" + fingerprint);
    }
    static bool ParentAlive(int pid, long started)
    {
        try { using (var p = Process.GetProcessById(pid)) return !p.HasExited && p.StartTime.ToUniversalTime().Ticks == started; }
        catch { return false; }
    }
    [STAThread]
    static void Main(string[] args)
    {
        int pid; long started; Guid session;
        if (args.Length != 3 || !Int32.TryParse(args[0], out pid) || !Int64.TryParse(args[1], out started) || !Guid.TryParseExact(args[2], "N", out session)) return;
        string root = AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(Path.Combine(root, "state"));
        string stem = Path.Combine(root, "state", session.ToString("N"));
        log = stem + ".log";
        bool owns;
        using (var singleton = new Mutex(true, @"Local\MuseDashSteamPresence.Controller." + session.ToString("N"), out owns))
        {
            if (!owns) return;
            SteamNative steam = null;
            try
            {
                var policy = new PublicationPolicy();
                long lastAttempt = -1;
                string lastEvent = "", pendingTitle = null;
                long pendingUntil = 0, nextCacheCheck = 0;
                bool wantedSelecting = false;
                var settings = new PresenceSettings();
                string configPath = Path.Combine(root, PresenceSettings.FileName);
                try { settings = PresenceSettings.Load(configPath); } catch (Exception e) { Log("Settings unavailable; using defaults: " + e.Message); }
                string fingerprint = settings.Fingerprint, savedFingerprint = fingerprint;
                long nextSettingsCheck = 0;
                string currentSong = "", currentAuthor = "", lastTitle = null;
                bool hasState = false;
                try
                {
                    var saved = File.ReadAllLines(stem + ".ledger", Encoding.UTF8);
                    lastAttempt = Int64.Parse(saved[0]);
                    long committed = Int64.Parse(saved[1]);
                    if (saved.Length > 3 && saved[3] == "selecting") policy.CommitSelection(committed);
                    else if (saved.Length > 2 && saved[2].Length > 0) policy.CommitSuccessfulPublication(Text(saved[2]), committed);
                    if (saved.Length > 4 && saved[4] == "uncertain") policy.InvalidatePublication();
                    currentSong = policy.DisplayedSong ?? "";
                    currentAuthor = saved.Length > 5 ? Text(saved[5]) : "";
                    hasState = policy.HasPublication;
                    wantedSelecting = !policy.IsPlaying;
                    lastTitle = saved.Length > 6 ? Text(saved[6]) :
                        (wantedSelecting ? SongDisplay.Idle(settings) : SongDisplay.Create(currentSong, currentAuthor, settings));
                    if (saved.Length > 7) savedFingerprint = saved[7];
                }
                catch {}
                bool sawPlaying = policy.HasPublication && policy.IsPlaying;
                long age = Math.Max(0, (DateTime.UtcNow.Ticks - started) / TimeSpan.TicksPerMillisecond);
                long bootUntil = (long)GetTickCount64() + Math.Max(0, 60000 - age);
                int bootRepairs = 0;
                bool refreshSettings = savedFingerprint != fingerprint;
                Log("Controller 0.5.1-beta.1 started; configurable titles, live settings, nonblocking state delivery.");
                while (ParentAlive(pid, started) && !File.Exists(stem + ".stop"))
                {
                    Thread.Sleep(100);
                    long now = (long)GetTickCount64();
                    if (now >= nextSettingsCheck)
                    {
                        nextSettingsCheck = now + 500;
                        try
                        {
                            var updated = PresenceSettings.Load(configPath);
                            if (updated.Fingerprint != fingerprint)
                            {
                                settings = updated; fingerprint = updated.Fingerprint;
                                refreshSettings = true; Log("Settings reloaded.");
                            }
                        }
                        catch { /* Keep the last valid configuration during an incomplete/manual edit. */ }
                    }
                    if (steam != null)
                    {
                        steam.PumpCallbacks();
                        bool bootSync = !sawPlaying && wantedSelecting && now < bootUntil;
                        if ((pendingTitle != null || bootSync) && now >= nextCacheCheck)
                        {
                            nextCacheCheck = now + 400;
                            ulong app = steam.ReadCurrentGameId();
                            string cached = steam.ReadCurrentTitle();
                            if (pendingTitle != null && app == GamePresencePacket.AppId && cached == pendingTitle)
                            {
                                AtomicWrite(stem + ".status", "published\n" + pendingTitle);
                                Log("Confirmed real Steam AppID 774171: " + pendingTitle);
                                pendingTitle = null;
                            }
                            else if (pendingTitle != null && now >= pendingUntil)
                            {
                                Log("Submitted state not fully reflected in local cache; state transitions remain enabled. Cached=" + cached);
                                AtomicWrite(stem + ".status", "sent-unconfirmed\n" + pendingTitle);
                                pendingTitle = null;
                            }
                            // Steam may replace the early custom name with its
                            // ordinary game name during native SDK initialization.
                            // Repair only that plain-name startup override, at
                            // most five times, and stop permanently on first play.
                            if (bootSync && bootRepairs < 5 && now - lastAttempt >= 1000 && app == GamePresencePacket.AppId &&
                                (cached.Length == 0 || cached == "Muse Dash" || cached == "MuseDash"))
                            {
                                bootRepairs++; lastAttempt = now;
                                string idleTitle = SongDisplay.Idle(settings);
                                if (idleTitle != SongDisplay.GameTitle && steam.TrySend(idleTitle))
                                {
                                    pendingTitle = idleTitle; pendingUntil = now + 6000;
                                    Log("Restored selecting after Steam's plain-name startup override.");
                                }
                            }
                        }
                    }
                    bool stateEvent = false;
                    long eventTime = policy.LastPublicationMilliseconds;
                    string[] request = null;
                    try { request = File.ReadAllLines(stem + ".request", Encoding.UTF8); } catch {}
                    if (request != null && request.Length >= 3 && request[0] != lastEvent)
                    {
                        lastEvent = request[0];
                        long requestedTime;
                        try
                        {
                            string song = PublicationPolicy.Normalize(Text(request[2]));
                            string author = request.Length > 3 ? Text(request[3]) : "";
                            string mode = request.Length > 4 ? request[4] : "playing";
                            if (Int64.TryParse(request[1], out requestedTime) && requestedTime <= now && now - requestedTime <= 30000 &&
                                requestedTime >= policy.LastPublicationMilliseconds && (mode == "selecting" || (mode == "playing" && song.Length > 0)))
                            {
                                bool selecting = mode == "selecting";
                                if (!selecting) sawPlaying = true;
                                bool eligible = selecting ? policy.IsEligibleForSelection(requestedTime) : policy.IsEligibleAtSongStart(song, requestedTime);
                                string rendered = selecting ? SongDisplay.Idle(settings) : SongDisplay.Create(song, author, settings);
                                stateEvent = eligible || rendered != lastTitle;
                                wantedSelecting = selecting; currentSong = song; currentAuthor = author;
                                hasState = true; eventTime = requestedTime;
                            }
                        }
                        catch { Log("Ignored invalid state request."); }
                    }
                    if (!hasState || (!stateEvent && !refreshSettings)) continue;
                    refreshSettings = false;
                    string title = wantedSelecting ? SongDisplay.Idle(settings) : SongDisplay.Create(currentSong, currentAuthor, settings);
                    // Commit the actual game state even when display switches
                    // cause different events to render to the same title.
                    if (title == lastTitle && policy.HasPublication)
                    {
                        if (wantedSelecting) policy.CommitSelection(eventTime);
                        else policy.CommitSuccessfulPublication(currentSong, eventTime);
                        Ledger(stem, policy, lastAttempt, "submitted", currentAuthor, lastTitle, fingerprint);
                        continue;
                    }
                    if (steam == null) steam = new SteamNative(pid);
                    lastAttempt = now;
                    Ledger(stem, policy, lastAttempt, "uncertain", currentAuthor, lastTitle, fingerprint);
                    if (steam.TrySend(title))
                    {
                        // Order and deduplicate by accepted events, not by an
                        // old confirmed cache entry. A long/truncated game name
                        // must never suppress the following selecting event.
                        // Event timestamp also preserves a newer queued event
                        // even if the previous send/confirmation finished later.
                        if (wantedSelecting) policy.CommitSelection(eventTime);
                        else policy.CommitSuccessfulPublication(currentSong, eventTime);
                        lastTitle = title;
                        Ledger(stem, policy, lastAttempt, "submitted", currentAuthor, lastTitle, fingerprint);
                        AtomicWrite(stem + ".status", "sent\n" + title);
                        pendingTitle = title; pendingUntil = now + 6000; nextCacheCheck = now;
                        Log("Submitted real Steam AppID 774171: " + title);
                    }
                    else
                    {
                        policy.InvalidatePublication();
                        Ledger(stem, policy, lastAttempt, "uncertain", currentAuthor, lastTitle, fingerprint);
                        pendingTitle = null;
                        AtomicWrite(stem + ".status", "send-failed\n" + title);
                        Log("Steam did not accept state; later transitions will still be sent.");
                    }
                }
            }
            catch (Exception e) { Log("Stopped: " + e); }
            finally
            {
                if (steam != null) { try { steam.Dispose(); } catch (Exception e) { Log("Cleanup: " + e.Message); } }
                Log("Controller stopped; original game remains tracked by Steam.");
                singleton.ReleaseMutex();
            }
        }
    }
}
