using System;
using MuseDashSteamPresence;

internal static class PolicyTests
{
    static int assertions;
    static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception(message);
    }
    static void Main()
    {
        var policy = new PublicationPolicy();
        Check(!policy.IsEligibleAtSongStart("", 0), "No blank song may publish.");
        Check(policy.IsEligibleAtSongStart("A", 0), "First song may publish.");
        policy.CommitSuccessfulPublication("A", 0);
        Check(!policy.IsEligibleAtSongStart("A", 1000), "A retry may not publish.");
        Check(!policy.IsEligibleAtSongStart("A", 181000), "Same song after cooldown may not publish.");
        Check(policy.IsEligibleAtSongStart("B", 1), "A different song is immediately eligible on its actual start.");
        Check(policy.DisplayedSong == "A", "Eligibility alone must not change displayed A.");
        Check(policy.LastPublicationMilliseconds == 0, "Eligibility alone must not commit a publication.");
        // Advancing time causes no action: the policy has no timer or queue.
        Check(policy.DisplayedSong == "A", "Time passing alone cannot publish an unrequested state.");
        Check(policy.IsEligibleAtSongStart("B", 0), "There is no timed cooldown, even at the same tick.");
        Check(policy.IsEligibleAtSongStart("C", 100), "Latest actual new start is evaluated, no queued B.");
        policy.CommitSuccessfulPublication("C", 100);
        Check(policy.IsEligibleAtSongStart("B", 100), "A new different song may publish without waiting.");
        Check(policy.IsEligibleAtSongStart("B", 101), "A new different song one millisecond later is eligible.");
        Check(policy.DisplayedSong == "C", "Failed publication must not be committed.");
        Check(!policy.IsEligibleAtSongStart("Ｃ", 361000), "Equivalent unicode spelling is the same song.");
        Check(!policy.IsEligibleAtSongStart("<b>C</b>", 361000), "Formatting does not make a new song.");
        Check(!policy.IsEligibleAtSongStart("B", 99), "A stale event before the last publication is still rejected.");
        var states = new PublicationPolicy();
        Check(states.IsEligibleForSelection(0), "Startup may display selecting without a previous song.");
        states.CommitSelection(0);
        Check(!states.IsEligibleForSelection(1), "Duplicate menu/loading states do not republish.");
        Check(states.IsEligibleAtSongStart("A", 1), "Selecting to playing is allowed immediately.");
        states.CommitSuccessfulPublication("A", 1);
        Check(!states.IsEligibleAtSongStart("A", 2), "Repeated playing event is ignored.");
        Check(states.IsEligibleForSelection(2), "End, quit or pause may switch to selecting.");
        states.CommitSelection(2);
        Check(!states.IsPlaying && states.DisplayedSong == "", "Selecting does not retain a playing state.");
        Check(!states.IsEligibleForSelection(3), "Repeated selection is ignored.");
        Check(states.IsEligibleAtSongStart("A", 3), "Resume/replay of the same song after selecting restores its title.");
        states.CommitSuccessfulPublication("A", 3);
        Check(!states.IsEligibleForSelection(2), "A stale non-playing event cannot replace newer playing state.");
        Check(states.IsEligibleAtSongStart("B", 4), "Different songs still update without cooldown.");
        Console.WriteLine("PASS: " + assertions + " publication-policy assertions.");
    }
}
