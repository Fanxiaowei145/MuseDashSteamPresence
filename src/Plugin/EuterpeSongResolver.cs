using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MuseDashSteamPresence
{
    internal static class EuterpeSongResolver
    {
        static MethodInfo lookup;
        static bool resolved;
        static readonly OpCode[] single = new OpCode[256];
        static readonly OpCode[] extended = new OpCode[256];
        static EuterpeSongResolver()
        {
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (field.FieldType != typeof(OpCode)) continue;
                var code = (OpCode)field.GetValue(null);
                ushort value = (ushort)code.Value;
                if (value < 256) single[value] = code;
                else if ((value & 0xff00) == 0xfe00) extended[value & 0xff] = code;
            }
        }
        // Find the same chart lookup already used by Euterpe's NowPlaying logger.
        // Obfuscated method names are not hardcoded and no Euterpe files are changed.
        static void Resolve()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Euterpe.Core");
            if (assembly == null) return;
            resolved = true;
            var type = assembly.GetType("Euterpe.Charts.BattleChartLogPatch");
            var entry = type == null ? null : type.GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var body = entry == null ? null : entry.GetMethodBody();
            if (body == null) return;
            byte[] il = body.GetILAsByteArray();
            int p = 0;
            while (p < il.Length)
            {
                byte first = il[p++];
                OpCode code = first == 0xfe ? extended[il[p++]] : single[first];
                int count;
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: count = 0; break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: count = 1; break;
                    case OperandType.InlineVar: count = 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: count = 8; break;
                    case OperandType.InlineSwitch: count = 4 + BitConverter.ToInt32(il, p) * 4; break;
                    default: count = 4; break;
                }
                if ((code == OpCodes.Call || code == OpCodes.Callvirt) && p + 4 <= il.Length)
                {
                    var method = entry.Module.ResolveMethod(BitConverter.ToInt32(il, p)) as MethodInfo;
                    if (method != null && method.IsStatic && method.ReturnType.FullName == "Euterpe.Charts.Chart")
                    {
                        var args = method.GetParameters();
                        if (args.Length == 1 && args[0].ParameterType == typeof(string)) { lookup = method; return; }
                    }
                }
                p += count;
            }
        }
        internal static bool TryGetDetails(string uid, out string name, out string author)
        {
            name = author = null;
            try
            {
                if (!resolved) Resolve();
                if (lookup == null) return false;
                object chart = lookup.Invoke(null, new object[] { uid });
                if (chart == null) return false;
                var meta = chart.GetType().GetProperty("Meta").GetValue(chart);
                if (meta == null) return false;
                name = meta.GetType().GetProperty("Name").GetValue(meta) as string;
                var authorProperty = meta.GetType().GetProperty("Author");
                if (authorProperty != null) author = authorProperty.GetValue(meta) as string;
                return !String.IsNullOrWhiteSpace(name);
            }
            catch { return false; }
        }
    }
}
