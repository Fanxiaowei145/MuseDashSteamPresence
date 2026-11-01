using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MuseDashSteamPresence
{
    // Evaluate on actual song starts or changes to the non-playing state.
    // There is deliberately no timer or pending-song queue.
    public sealed class PublicationPolicy
    {
        public const long MinimumIntervalMilliseconds = 0;
        public string DisplayedSong { get; private set; }
        public long LastPublicationMilliseconds { get; private set; }
        public bool HasPublication { get; private set; }
        public bool IsPlaying { get; private set; }

        public static string Normalize(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "";
            value = Regex.Replace(value, "<[^>]*>", "");
            value = Regex.Replace(value.Normalize(NormalizationForm.FormKC), @"\s+", " ").Trim();
            return value;
        }

        public bool IsEligibleAtSongStart(string song, long nowMilliseconds)
        {
            song = Normalize(song);
            if (song.Length == 0) return false;
            if (!HasPublication) return true;
            if (IsPlaying && String.Equals(song, DisplayedSong, StringComparison.OrdinalIgnoreCase)) return false;
            return nowMilliseconds >= LastPublicationMilliseconds &&
                nowMilliseconds - LastPublicationMilliseconds >= MinimumIntervalMilliseconds;
        }

        public void CommitSuccessfulPublication(string song, long nowMilliseconds)
        {
            song = Normalize(song);
            if (song.Length == 0) throw new ArgumentException("A song name is required.");
            DisplayedSong = song;
            LastPublicationMilliseconds = nowMilliseconds;
            HasPublication = true;
            IsPlaying = true;
        }
        public bool IsEligibleForSelection(long nowMilliseconds)
        {
            return !HasPublication || (IsPlaying && nowMilliseconds >= LastPublicationMilliseconds);
        }
        public void CommitSelection(long nowMilliseconds)
        {
            DisplayedSong = "";
            LastPublicationMilliseconds = nowMilliseconds;
            HasPublication = true;
            IsPlaying = false;
        }
        public void InvalidatePublication() { HasPublication = false; }
    }
}
