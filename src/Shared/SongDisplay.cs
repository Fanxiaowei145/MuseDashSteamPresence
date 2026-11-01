using System;
using System.Text;

namespace MuseDashSteamPresence
{
    public static class SongDisplay
    {
        public const string SelectingTitle = "Muse Dash : 选歌中……";
        public const string GameTitle = "Muse Dash";
        static string Limit(string value)
        {
            while (Encoding.UTF8.GetByteCount(value) > 240)
            {
                int length = value.Length - 1;
                if (length > 0 && Char.IsHighSurrogate(value[length - 1])) length--;
                value = value.Substring(0, length);
            }
            return value;
        }
        public static string Create(string song, string author)
        {
            return Create(song, author, new PresenceSettings());
        }
        public static string Idle(PresenceSettings settings)
        {
            // User text keeps its punctuation, including the two ellipses.
            string text = CleanCustom(settings.IdleText).Trim();
            return settings.ShowIdle && text.Length > 0 ? Limit(GameTitle + " : " + text) : GameTitle;
        }
        public static string CleanCustom(string text)
        {
            if (text == null) return "";
            var b = new StringBuilder();
            foreach (char c in text) b.Append(Char.IsControl(c) ? ' ' : c);
            return b.ToString();
        }
        public static string Create(string song, string author, PresenceSettings settings)
        {
            song = PublicationPolicy.Normalize(song);
            author = PublicationPolicy.Normalize(author);
            song = settings.ShowSong ? song : "";
            author = settings.ShowAuthor ? author : "";
            if (song.Length == 0 && author.Length == 0) return GameTitle;
            if (song.Length == 0) return Limit(GameTitle + " : " + author);
            string basic = GameTitle + " : " + song;
            string separator = CleanCustom(settings.Separator);
            string value = Limit(basic + (author.Length == 0 ? "" : separator + author));
            // Do not leave an orphan separator if the author cannot fit at all.
            return author.Length > 0 && separator.Length > 0 && value.Length > basic.Length &&
                value.Length <= basic.Length + separator.Length ? Limit(basic) : value;
        }
    }
}
