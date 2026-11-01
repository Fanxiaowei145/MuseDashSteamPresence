using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MuseDashSteamPresence
{
    // File-only deployment; no Steam calls, network downloads or process launch.
    internal static class PayloadDeployment
    {
        internal const string ResourceName = "MuseDashSteamPresence.Payload.zip";
        const string Prefix = "UserData/MuseDashSteamPresence/";
        internal sealed class Entry { internal string Name, Hash; internal byte[] Data; }
        internal static string Hash(byte[] data)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();
        }
        static bool Allowed(string name)
        {
            if (String.IsNullOrWhiteSpace(name) || name.StartsWith("/") || name.Contains("\\") || name.Contains(":") || name.IndexOf('\0') >= 0) return false;
            foreach (string part in name.Split('/')) if (part == "." || part == ".." || part.Length == 0 || part.EndsWith(".") || part.EndsWith(" ")) return false;
            if (name == "Plugins/MuseDashSteamPresence.Bootstrap.dll") return true;
            if (name == "MuseDash-Settings.cmd" || name == "MuseDash-Disable.cmd" || name == "MuseDash-Enable.cmd" || name == "MuseDashPresence-ReadMe.txt") return true;
            if (!name.StartsWith(Prefix,StringComparison.Ordinal)) return false;
            string rest = name.Substring(Prefix.Length);
            if (rest.Equals("settings.xml",StringComparison.OrdinalIgnoreCase) || rest.StartsWith("state/",StringComparison.OrdinalIgnoreCase) || rest.StartsWith("backups/",StringComparison.OrdinalIgnoreCase)) return false;
            // No executable/script under arbitrary subdirectories, and no
            // game/Steam/third-party binaries. All active files are allowlisted.
            if (rest == "PresenceController.exe" || rest == "MuseDashPresenceSettings.exe") return true;
            if (rest == "GameDirectory.ps1" || rest == "disable.ps1" || rest == "enable.ps1" || rest == "Disable.cmd" || rest == "Enable.cmd") return true;
            string ext = Path.GetExtension(rest).ToLowerInvariant();
            return ext == ".md" || ext == ".txt" || ext == ".png" || rest == "LICENSE" || rest == "VERSION";
        }
        internal static List<Entry> Validate(byte[] archive)
        {
            if (archive == null || archive.Length > 32 * 1024 * 1024) throw new InvalidDataException("Embedded payload missing or too large.");
            using (var input = new MemoryStream(archive,false))
            using (var zip = new ZipArchive(input,ZipArchiveMode.Read))
            {
                if (zip.Entries.Count > 150) throw new InvalidDataException("Too many payload files.");
                var files = new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
                long total = 0;
                foreach (var item in zip.Entries)
                {
                    if (item.Length > 16 * 1024 * 1024 || (total += item.Length) > 32 * 1024 * 1024) throw new InvalidDataException("Payload size limit exceeded.");
                    if (item.FullName != "payload-manifest.tsv" && !Allowed(item.FullName)) throw new InvalidDataException("Disallowed payload path: " + item.FullName);
                    if (files.ContainsKey(item.FullName)) throw new InvalidDataException("Duplicate payload path.");
                    using (var source = item.Open()) using (var target = new MemoryStream()) { source.CopyTo(target); files.Add(item.FullName,target.ToArray()); }
                }
                byte[] manifest;
                if (!files.TryGetValue("payload-manifest.tsv",out manifest)) throw new InvalidDataException("Payload manifest missing.");
                var result = new List<Entry>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string line in Encoding.UTF8.GetString(manifest).Split(new[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = line.Split('\t'); byte[] data;
                    if (parts.Length != 2 || parts[0].Length != 64 || !Allowed(parts[1]) || !seen.Add(parts[1]) || !files.TryGetValue(parts[1],out data) || Hash(data) != parts[0])
                        throw new InvalidDataException("Payload hash/manifest mismatch.");
                    result.Add(new Entry { Name = parts[1], Hash = parts[0], Data = data });
                }
                if (result.Count != files.Count - 1) throw new InvalidDataException("Unlisted payload file.");
                foreach (string required in new[] { "Plugins/MuseDashSteamPresence.Bootstrap.dll", Prefix+"PresenceController.exe", Prefix+"MuseDashPresenceSettings.exe", "MuseDash-Settings.cmd" })
                    if (!seen.Contains(required)) throw new InvalidDataException("Required payload missing: " + required);
                return result;
            }
        }
        static string Target(string game,string name)
        {
            string full = Path.GetFullPath(Path.Combine(game,name.Replace('/',Path.DirectorySeparatorChar)));
            if (!full.StartsWith(game + Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Payload escaped game folder.");
            for (string current = full; current != null && !current.Equals(game,StringComparison.OrdinalIgnoreCase); current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("Refusing a reparse-point payload target.");
            return full;
        }
        internal static bool NeedsFile(string game, List<Entry> entries,string name)
        {
            foreach (var entry in entries) if (entry.Name == name)
            {
                string path = Target(Path.GetFullPath(game).TrimEnd(Path.DirectorySeparatorChar),name);
                return !File.Exists(path) || Hash(File.ReadAllBytes(path)) != entry.Hash;
            }
            throw new InvalidDataException("Requested payload file absent.");
        }
        internal static int Install(string game,List<Entry> entries)
        {
            game = Path.GetFullPath(game).TrimEnd(Path.DirectorySeparatorChar);
            if (!File.Exists(Path.Combine(game,"musedash.exe"))) throw new DirectoryNotFoundException("Muse Dash folder required.");
            var pending = new List<Entry>();
            foreach (var entry in entries)
            {
                if (!Allowed(entry.Name) || Hash(entry.Data) != entry.Hash) throw new InvalidDataException("Invalid deployment plan.");
                string target = Target(game,entry.Name);
                // Do not undo an intentional independent disable of the early
                // component just because the main mod is being repaired.
                if (entry.Name == "Plugins/MuseDashSteamPresence.Bootstrap.dll" && !File.Exists(target) && File.Exists(target+".disabled")) continue;
                if (!File.Exists(target) || Hash(File.ReadAllBytes(target)) != entry.Hash) pending.Add(entry);
            }
            if (pending.Count == 0) return 0;
            string backupRoot = Target(game,Prefix+"backups/deploy-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
            var done = new List<string[]>();
            var staged = new List<string>();
            try
            {
                // Stage all bytes before committing any file.
                foreach (var entry in pending)
                {
                    string target = Target(game,entry.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    string temp = target+".deploy-"+Guid.NewGuid().ToString("N");
                    File.WriteAllBytes(temp,entry.Data); staged.Add(temp);
                }
                for (int i=0;i<pending.Count;i++)
                {
                    string target = Target(game,pending[i].Name);
                    string old = Path.Combine(backupRoot,i.ToString("D3")+"-"+Path.GetFileName(pending[i].Name));
                    Directory.CreateDirectory(backupRoot);
                    File.AppendAllText(Path.Combine(backupRoot,"restore-map.tsv"),Path.GetFileName(old)+"\t"+pending[i].Name+"\n",Encoding.UTF8);
                    if (File.Exists(target)) { Directory.CreateDirectory(Path.GetDirectoryName(old)); File.Replace(staged[i],target,old); }
                    else { File.Move(staged[i],target); old = null; }
                    done.Add(new[] {target,old});
                }
                return pending.Count;
            }
            catch
            {
                for (int i=done.Count-1;i>=0;i--) { if (done[i][1] == null) File.Delete(done[i][0]); else File.Copy(done[i][1],done[i][0],true); }
                throw;
            }
            finally { foreach (string temp in staged) if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
