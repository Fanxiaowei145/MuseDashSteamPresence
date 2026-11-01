using System;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

internal static class InstallationFixtures
{
    internal static void Touch(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path,"isolated fixture; never executed");
    }
    internal static string Create(string home)
    {
        string game = Path.Combine(home,Guid.NewGuid().ToString("N"));
        Touch(Path.Combine(game,"musedash.exe"));
        string support = Path.Combine(game,"UserData","MuseDashSteamPresence");
        Touch(Path.Combine(support,"PresenceController.exe"));
        Emit(Path.Combine(game,"Mods","MuseDashSteamPresence.dll"),"0.4.2.0");
        Emit(Path.Combine(game,"Plugins","MuseDashSteamPresence.Bootstrap.dll"),"0.4.2.0");
        return support;
    }
    internal static void Emit(string path, string version)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var provider = new CSharpCodeProvider())
        {
            var options = new CompilerParameters { GenerateExecutable = false, GenerateInMemory = false, OutputAssembly = path };
            var result = provider.CompileAssemblyFromSource(options,"using System.Reflection; [assembly: AssemblyVersion(\"" + version + "\")] public class Fixture {}");
            if (result.Errors.HasErrors) throw new Exception("Could not compile fixture: " + path);
        }
    }
}
