using System;
using System.IO;
using System.Security.Cryptography;

namespace MuseDashSteamPresence
{
    // Deliberately NOT a fuzzy ABI matcher. All bytes except COFF timestamp,
    // checksum and the unmapped, trailing certificate table remain identical.
    internal static class SteamImageFingerprint
    {
        static ushort U16(byte[] b, int p) { return BitConverter.ToUInt16(b,p); }
        static uint U32(byte[] b, int p) { return BitConverter.ToUInt32(b,p); }
        internal static string Compute(byte[] source)
        {
            try
            {
                if (source == null || source.Length < 512 || source.Length > 128 * 1024 * 1024) return null;
                if (U16(source,0) != 0x5a4d) return null;
                int pe = checked((int)U32(source,60));
                if (pe < 64 || pe > source.Length - 264 || U32(source,pe) != 0x4550 || U16(source,pe+4) != 0x8664) return null;
                int count = U16(source,pe+6), optional = pe+24, optionalSize = U16(source,pe+20);
                if (count < 1 || count > 96 || optionalSize != 240 || U16(source,optional) != 0x20b || U32(source,optional+108) != 16) return null;
                int table = optional+optionalSize;
                int headers = checked((int)U32(source,optional+60));
                if (headers < table+count*40 || headers > source.Length) return null;
                int security = optional+144;
                int certificate = checked((int)U32(source,security)), certificateSize = checked((int)U32(source,security+4));
                if ((certificate == 0) != (certificateSize == 0)) return null;
                int end = source.Length;
                if (certificate != 0)
                {
                    if ((certificate & 7) != 0 || certificate < headers || certificateSize < 8 || (long)certificate+certificateSize != source.Length) return null;
                    int cursor = certificate;
                    while (cursor < source.Length)
                    {
                        if (source.Length-cursor < 8) return null;
                        uint length = U32(source,cursor);
                        if (length < 8 || length > source.Length-cursor || U16(source,cursor+4) != 0x200 || U16(source,cursor+6) != 2) return null;
                        cursor = checked(cursor + (int)((length+7)&~7U));
                    }
                    if (cursor != source.Length) return null;
                    end = certificate;
                }
                long previousEnd = headers;
                for (int i=0;i<count;i++)
                {
                    int section=table+i*40;
                    uint size=U32(source,section+16), start=U32(source,section+20);
                    if (size == 0) continue;
                    if (start < previousEnd || (long)start+size > end) return null;
                    previousEnd=(long)start+size;
                }
                var normalized = new byte[end];
                Buffer.BlockCopy(source,0,normalized,0,end);
                Array.Clear(normalized,pe+8,4);
                Array.Clear(normalized,optional+64,4);
                Array.Clear(normalized,security,8);
                using (var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(normalized)).Replace("-","");
            }
            catch (ArgumentException) { return null; }
            catch (OverflowException) { return null; }
            catch (IndexOutOfRangeException) { return null; }
        }
    }
}
