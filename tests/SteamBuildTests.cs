using System;
using MuseDashSteamPresence;
class SteamBuildTests
{
    static void Main()
    {
        var old = SteamBuild.FindProfile(SteamBuild.SupportedHash);
        var current = SteamBuild.FindProfile(SteamBuild.NovemberHash);
        if (old == null || current == null || old.Shared[2] == current.Shared[2]) throw new Exception("Build profiles missing or mixed.");
        if (current.Shared[2] != 0x88d020 || current.Engine[1] != 0x9a9b50 || current.Friends[0] != 0x798060) throw new Exception("Audited profile mismatch.");
        foreach (string unknown in new string[] {null, "", new string('0',64), SteamBuild.NovemberHash.Substring(1)})
            if (SteamBuild.FindProfile(unknown) != null) throw new Exception("Unknown build was accepted.");
        Console.WriteLine("PASS: both Steam profiles selected; unknown/truncated hashes rejected.");
    }
}
