using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MuseDashSteamPresence;

// This executable compiles the REAL controller loop with a fake transport.
// SteamNative.cs is deliberately not linked: no Steam DLL/API is loaded.
namespace MuseDashSteamPresence
{
    internal sealed class SteamNative : IDisposable
    {
        internal static readonly List<string> Sent = new List<string>();
        internal static string Cached = "";
        internal static bool RejectNext;
        public SteamNative(int pid) {}
        public void PumpCallbacks() {}
        public ulong ReadCurrentGameId() { return 774171; }
        public string ReadCurrentTitle() { lock (Sent) return Cached; }
        public bool TrySend(string title)
        {
            lock (Sent)
            {
                Sent.Add(title);
                if (RejectNext) { RejectNext = false; Cached = "uncertain previous state"; return false; }
                Cached = title == SongDisplay.SelectingTitle ? title : "Muse Dash : truncated cache";
                return true;
            }
        }
        public void Dispose() {}
    }
}
internal static class ControllerRegressionTests
{
    [DllImport("kernel32")] static extern ulong GetTickCount64();
    static string stem;
    static Exception failure;
    static string[] controllerArgs;
    static string configPath;
    static int Count { get { lock (SteamNative.Sent) return SteamNative.Sent.Count; } }
    static void Push(bool playing, string song)
    {
        string text = Guid.NewGuid().ToString("N") + "\n" + GetTickCount64() + "\n" +
            Convert.ToBase64String(Encoding.UTF8.GetBytes(song)) + "\n" +
            Convert.ToBase64String(Encoding.UTF8.GetBytes("Author")) + "\n" + (playing ? "playing" : "selecting");
        string temp = stem + ".test.tmp";
        File.WriteAllText(temp, text, Encoding.UTF8);
        if (File.Exists(stem + ".request")) File.Replace(temp, stem + ".request", null);
        else File.Move(temp, stem + ".request");
    }
    static Thread Start()
    {
        failure = null;
        var thread = new Thread(() => {
            try { typeof(PresenceController).GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { controllerArgs }); }
            catch (Exception e) { failure = e; }
        });
        thread.IsBackground = true; thread.Start(); return thread;
    }
    static void WaitFor(int expected)
    {
        var watch = Stopwatch.StartNew();
        while (Count < expected && watch.ElapsedMilliseconds < 1800 && failure == null) Thread.Sleep(10);
        if (failure != null) throw failure;
        if (Count != expected) throw new Exception("Expected " + expected + " submissions, got " + Count + "; controller blocked or discarded transition.");
    }
    static void Stop(Thread thread)
    {
        File.WriteAllText(stem + ".stop", "test stop");
        if (!thread.Join(2000)) throw new Exception("Controller did not stop promptly.");
        if (failure != null) throw failure;
    }
    static void Main()
    {
        string session = Guid.NewGuid().ToString("N");
        string root = AppDomain.CurrentDomain.BaseDirectory;
        configPath = Path.Combine(root, PresenceSettings.FileName);
        if (File.Exists(configPath)) throw new Exception("Tests require an isolated config directory.");
        // Explicitly opt in for legacy/full-format transition coverage. The
        // no-config new-install path is exercised separately below.
        new PresenceSettings { ShowIdle = true, ShowAuthor = true }.Save(configPath);
        Directory.CreateDirectory(Path.Combine(root, "state"));
        stem = Path.Combine(root, "state", session);
        using (var parent = Process.GetCurrentProcess())
            controllerArgs = new[] { parent.Id.ToString(), parent.StartTime.ToUniversalTime().Ticks.ToString(), session };
        Thread worker = null;
        try
        {
            Push(false, ""); worker = Start(); WaitFor(1);
            lock (SteamNative.Sent) SteamNative.Cached = "";
            WaitFor(2); // Native SDK's ordinary-name startup override is repaired.
            Push(true, new string('中', 40)); WaitFor(3);
            lock (SteamNative.Sent) SteamNative.Cached = "";
            Thread.Sleep(1200);
            if (Count != 3) throw new Exception("Startup repair continued after playing began.");
            // Its cache never confirms. Selecting must still be delivered in
            // under 1.8s, rather than waiting 6s then deduplicating against menu.
            Push(false, ""); WaitFor(4);
            Push(false, ""); Thread.Sleep(350);
            if (Count != 4) throw new Exception("Repeated selecting was republished.");
            lock (SteamNative.Sent) SteamNative.RejectNext = true;
            Push(true, "Uncertain transport"); WaitFor(5);
            Push(false, ""); WaitFor(6);
            Push(true, "Same song after menu"); WaitFor(7);
            Push(false, ""); WaitFor(8);
            Stop(worker); worker = null;
            File.Delete(stem + ".stop");
            worker = Start(); Thread.Sleep(350);
            if (Count != 8) throw new Exception("Restored selecting ledger was not deduplicated.");
            Stop(worker); worker = null;
            File.Delete(stem + ".stop");
            worker = Start(); Thread.Sleep(350);
            var settings = new PresenceSettings { ShowIdle = true, ShowAuthor = true, IdleText = "摸鱼中…… 😀" };
            settings.Save(configPath); WaitFor(9);
            lock (SteamNative.Sent) if (SteamNative.Sent[8] != "Muse Dash : 摸鱼中…… 😀") throw new Exception("Idle settings were not applied without a game event.");
            settings.ShowIdle = false; settings.Save(configPath); WaitFor(10);
            lock (SteamNative.Sent) if (SteamNative.Sent[9] != "Muse Dash") throw new Exception("Idle off did not restore base title.");
            Push(true, "KiLLKiSS"); WaitFor(11);
            settings.ShowSong = false; settings.Save(configPath); WaitFor(12);
            lock (SteamNative.Sent) if (SteamNative.Sent[11] != "Muse Dash : Author") throw new Exception("Author-only live update failed.");
            settings.ShowSong = true; settings.Separator = " / "; settings.Save(configPath); WaitFor(13);
            lock (SteamNative.Sent) if (SteamNative.Sent[12] != "Muse Dash : KiLLKiSS / Author") throw new Exception("Custom separator live update failed.");
            settings.ShowSong = false; settings.ShowAuthor = false; settings.Save(configPath); WaitFor(14);
            Push(true, "Another song"); Thread.Sleep(350);
            Push(false, ""); Thread.Sleep(350);
            if (Count != 14) throw new Exception("Identical rendered base titles were republished.");
            settings.ShowIdle = true; settings.Save(configPath); WaitFor(15);
            lock (SteamNative.Sent) if (SteamNative.Sent[14] != "Muse Dash : 摸鱼中…… 😀") throw new Exception("Hidden transitions did not retain actual non-playing state.");
            File.WriteAllText(configPath, "broken XML"); Thread.Sleep(700);
            if (Count != 15) throw new Exception("Malformed config changed state.");
            settings.Save(configPath); Thread.Sleep(650);
            if (Count != 15) throw new Exception("Saving identical settings republished state.");
            Stop(worker); worker = null;
            File.Delete(configPath);
            string defaultSession = Guid.NewGuid().ToString("N");
            stem = Path.Combine(root,"state",defaultSession); controllerArgs[2] = defaultSession;
            lock (SteamNative.Sent) { SteamNative.Sent.Clear(); SteamNative.Cached = ""; }
            Push(false, ""); worker = Start(); WaitFor(1);
            lock (SteamNative.Sent) if (SteamNative.Sent[0] != "Muse Dash") throw new Exception("New install must show ordinary game title while idle.");
            Thread.Sleep(1200);
            if (Count != 1) throw new Exception("Default ordinary idle title must not cause startup repair spam.");
            Push(true,"Default song"); WaitFor(2);
            lock (SteamNative.Sent) if (SteamNative.Sent[1] != "Muse Dash : Default song") throw new Exception("New install must omit author by default.");
            Push(false,""); WaitFor(3);
            lock (SteamNative.Sent) if (SteamNative.Sent[2] != "Muse Dash") throw new Exception("Return from a song must restore the default ordinary title.");
            Stop(worker); worker = null;
            Console.WriteLine("PASS: real controller loop handles startup override repair, first-play repair cutoff, unconfirmed names, immediate selecting, rejected sends, duplicate menus and restart persistence; no live Steam calls.");
            Console.WriteLine("PASS: live idle/song/artist/separator settings, both-off dedup, hidden state transitions, malformed config recovery; no live Steam calls.");
            Console.WriteLine("PASS: fresh no-config install renders ordinary idle title and song only, avoids idle repair spam, returns to base title after song.");
        }
        finally { if (worker != null) Stop(worker); File.Delete(configPath); File.Delete(configPath + ".bak"); }
    }
}
