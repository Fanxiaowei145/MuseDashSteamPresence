using System;
using MuseDashSteamPresence;
internal static class PacketTests
{
    static void Main()
    {
        // Independent fixture from the protobuf field schema and verified wire envelope.
        string expected = "22150080000000000A1A111BD00B00000000003A0D4D7573652044617368203A2041482A";
        string actual = BitConverter.ToString(GamePresencePacket.Create("Muse Dash : A", 42)).Replace("-", "");
        if (actual != expected) throw new Exception("Original-app packet fixture mismatch: " + actual);
        bool rejected = false;
        try { GamePresencePacket.Create("Muse Dash : A", 0); } catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new Exception("A real game PID is required.");
        rejected = false;
        try { GamePresencePacket.Create(new string('中', 81), 42); } catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new Exception("UTF-8 byte limit is required.");
        Console.WriteLine("PASS: original-app protobuf fixture, actual PID and UTF-8 bounds.");
    }
}
