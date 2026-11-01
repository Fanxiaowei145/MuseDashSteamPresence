using System;
using System.IO;
using System.Threading;
internal static class FakeOldController
{
    static void Main(string[] args)
    {
        if (args.Length != 1) return;
        DateTime until = DateTime.UtcNow.AddSeconds(12);
        while (!File.Exists(args[0]) && DateTime.UtcNow < until) Thread.Sleep(20);
    }
}
