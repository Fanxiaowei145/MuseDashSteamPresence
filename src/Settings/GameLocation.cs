using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

internal static class GameLocation
{
    internal static string Find()
    {
        var libraries = new List<string>();
        using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
        {
            string steam = key == null ? null : key.GetValue("SteamPath") as string;
            if (!String.IsNullOrEmpty(steam))
            {
                libraries.Add(steam);
                try
                {
                    string vdf = File.ReadAllText(Path.Combine(steam, "steamapps", "libraryfolders.vdf"));
                    foreach (Match match in Regex.Matches(vdf, "\"path\"\\s*\"([^\"]+)\""))
                        libraries.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
                }
                catch {}
            }
        }
        foreach (string library in libraries)
        {
            string game = Path.Combine(library, "steamapps", "common", "Muse Dash");
            if (File.Exists(Path.Combine(game, "musedash.exe"))) return game;
        }
        return null;
    }
    internal static string SettingsRoot()
    {
        string local = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(local);
        if (dir.Name.Equals("MuseDashSteamPresence", StringComparison.OrdinalIgnoreCase) && dir.Parent != null && dir.Parent.Name.Equals("UserData", StringComparison.OrdinalIgnoreCase)) return local;
        string game = Find();
        if (game != null)
        {
            string installed = Path.Combine(game, "UserData", "MuseDashSteamPresence");
            if (File.Exists(Path.Combine(installed, "PresenceController.exe"))) return installed;
        }
        return local;
    }
}
