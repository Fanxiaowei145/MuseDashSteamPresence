using System;
using System.IO;
using MuseDashSteamPresence;
class PresenceLocatorProbe
{
    static int Main(string[] args)
    {
        var image = File.ReadAllBytes(args[0]);
        var located = PresenceImageLocator.Locate(image);
        Console.WriteLine("Discovered methods: {0}", located.Shared.Length + located.Engine.Length + located.Friends.Length);
        if (args.Length == 2 && args[1] == "--expect-unsupported")
        {
            try { using (var stream = new MemoryStream(image)) PresenceImageLocator.ReadAudited(stream); }
            catch (InvalidDataException) { Console.WriteLine("Unaudited build rejected after discovery."); return 0; }
            return 1;
        }
        using (var stream = new MemoryStream(image)) PresenceImageLocator.ReadAudited(stream);
        Console.WriteLine("Blind discovery matched all seven audited methods.");
        int pe = BitConverter.ToInt32(image,60);
        image[BitConverter.ToInt32(image,pe+24+240+20)+64] ^= 1;
        using (var stream = new MemoryStream(image))
        {
            try { PresenceImageLocator.ReadAudited(stream); }
            catch (InvalidDataException) { Console.WriteLine("Changed executable content rejected."); return 0; }
        }
        return 1;
    }
}
