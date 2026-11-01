using System;
using System.Text;
using MuseDashSteamPresence;
internal static class SongDisplayTests
{
    static void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
    static void Main()
    {
        var defaults = new PresenceSettings();
        Check(!defaults.ShowIdle && defaults.ShowSong && !defaults.ShowAuthor, "New-install defaults: base idle title and song only.");
        Check(SongDisplay.Idle(defaults) == "Muse Dash", "Default non-playing title is the ordinary game name.");
        Check(SongDisplay.Create("ものふぉびあ", "とりぴよ feat. 凛々咲") == "Muse Dash : ものふぉびあ", "Default playing title omits the artist.");
        var full = new PresenceSettings { ShowIdle = true, ShowAuthor = true };
        Check(SongDisplay.Create("KiLLKiSS", "Ave Mujica", full) == "Muse Dash : KiLLKiSS_Ave Mujica", "Opt-in song and author example.");
        Check(SongDisplay.SelectingTitle == "Muse Dash : 选歌中\u2026\u2026", "Selecting keeps the exact two ellipsis characters.");
        Check(SongDisplay.Create("曲名", null) == "Muse Dash : 曲名", "Missing author adds no separator.");
        Check(SongDisplay.Create("曲名", "  ") == "Muse Dash : 曲名", "Blank author adds no separator.");
        Check(SongDisplay.Create("<b>KiLLKiSS</b>", " Ave\nMujica ", full) == "Muse Dash : KiLLKiSS_Ave Mujica", "Formatting and line breaks are normalized.");
        string longText = SongDisplay.Create(new string('中', 100), "作者");
        Check(Encoding.UTF8.GetByteCount(longText) <= 240, "Steam UTF-8 byte limit.");
        string boundary = SongDisplay.Create(new string('a', 227), "A", full);
        Check(!boundary.EndsWith("_"), "Truncated author leaves no orphan separator.");
        string emoji = SongDisplay.Create("Song", String.Concat(new string[] {new string('a', 220), "😀😀😀😀"}), full);
        new UTF8Encoding(false, true).GetBytes(emoji);
        var policy = new PublicationPolicy();
        policy.CommitSuccessfulPublication("KiLLKiSS", 10);
        Check(!policy.IsEligibleAtSongStart("KiLLKiSS", 11), "Author display does not change repeat-song identity.");
        var s = new PresenceSettings { ShowIdle = true, ShowAuthor = true };
        Check(SongDisplay.Idle(s) == SongDisplay.SelectingTitle, "Opt-in selecting text keeps exact punctuation.");
        s.IdleText = "休息中♪…… <3 & 😀";
        Check(SongDisplay.Idle(s) == "Muse Dash : 休息中♪…… <3 & 😀", "Custom punctuation and text are preserved.");
        s.ShowIdle = false; Check(SongDisplay.Idle(s) == "Muse Dash", "Idle disabled shows real game name.");
        s.ShowSong = false;
        Check(SongDisplay.Create("KiLLKiSS", "Ave Mujica", s) == "Muse Dash : Ave Mujica", "Artist only has no separator.");
        s.ShowAuthor = false;
        Check(SongDisplay.Create("KiLLKiSS", "Ave Mujica", s) == "Muse Dash", "Both off show base title.");
        s.ShowSong = true;
        Check(SongDisplay.Create("KiLLKiSS", "Ave Mujica", s) == "Muse Dash : KiLLKiSS", "Song only.");
        s.ShowAuthor = true;
        foreach (string sep in new[] { "", " ", "_", " / ", "🎵" })
        {
            s.Separator = sep;
            Check(SongDisplay.Create("KiLLKiSS", "Ave Mujica", s) == "Muse Dash : KiLLKiSS" + sep + "Ave Mujica", "Exact separator: " + sep);
            Check(SongDisplay.Create("KiLLKiSS", "", s) == "Muse Dash : KiLLKiSS", "Missing artist never leaves a separator.");
        }
        s.ShowIdle = true; s.IdleText = new string('中', 200) + "😀";
        string idleLong = SongDisplay.Idle(s);
        Check(Encoding.UTF8.GetByteCount(idleLong) <= 240, "Idle UTF-8 limit.");
        new UTF8Encoding(false, true).GetBytes(idleLong);
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MuseDash-settings-test-" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            s.IdleText = "摸鱼中…… <3 & 😀"; s.Separator = "  "; s.Save(path);
            Check(PresenceSettings.Load(path).Fingerprint == s.Fingerprint, "XML roundtrip preserves Unicode and spaces.");
            s.ShowSong = false; s.Save(path);
            Check(!PresenceSettings.Load(path).ShowSong, "Atomic replacement roundtrip.");
            System.IO.File.WriteAllText(path, "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///C:/Windows/win.ini'>]><MuseDashPresence version='1'><IdleText>&e;</IdleText></MuseDashPresence>");
            bool rejected = false; try { PresenceSettings.Load(path); } catch { rejected = true; }
            Check(rejected, "External entities and DTDs rejected.");
        }
        finally { System.IO.File.Delete(path); System.IO.File.Delete(path + ".bak"); }
        Console.WriteLine("PASS: song_author example, missing metadata, formatting, UTF-8 limits and repeat identity.");
    }
}
