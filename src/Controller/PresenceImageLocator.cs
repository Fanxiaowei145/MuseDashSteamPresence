using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MuseDashSteamPresence
{
    // Read addresses from the on-disk x64 image. This reader never loads or calls it.
    internal sealed class PresenceImageLocator
    {
        readonly byte[] image;
        readonly int table, count;
        readonly ulong imageBase;
        PresenceImageLocator(byte[] bytes)
        {
            image = bytes;
            int pe = I32(60);
            if (I32(pe) != 0x4550 || U16(pe + 4) != 0x8664 || U16(pe + 24) != 0x20b)
                throw new InvalidDataException("Unexpected Steam image format.");
            count = U16(pe + 6);
            table = checked(pe + 24 + U16(pe + 20));
            imageBase = BitConverter.ToUInt64(image, pe + 48);
        }
        int I32(int offset) { return BitConverter.ToInt32(image, offset); }
        int U16(int offset) { return BitConverter.ToUInt16(image, offset); }
        int Rva(int offset, int length)
        {
            for (int i = 0; i < count; i++)
            {
                int section = checked(table + i * 40);
                int delta = checked(offset - I32(section + 20));
                if (delta >= 0 && (long)delta + length <= I32(section + 16))
                    return checked(I32(section + 12) + delta);
            }
            throw new InvalidDataException("Address outside a file-backed section.");
        }
        IEnumerable<int> Matches(byte[] needle)
        {
            for (int i = 0; i <= image.Length - needle.Length; i++)
            {
                i = Array.IndexOf(image, needle[0], i);
                if (i < 0 || i > image.Length - needle.Length) yield break;
                int j = 1;
                while (j < needle.Length && needle[j] == image[i+j]) j++;
                if (j == needle.Length) yield return i;
            }
        }
        long[] ReadSlots(string type, params int[] slots)
        {
            long[] found = null;
            foreach (int name in Matches(Encoding.ASCII.GetBytes(type + "\0")))
            {
                int descriptor = Rva(name - 16, 16);
                foreach (int field in Matches(BitConverter.GetBytes(descriptor)))
                {
                    int col = field - 12;
                    if (col < 0 || col > image.Length - 24 || I32(col) != 1 || I32(col+4) != 0) continue;
                    int self = Rva(col, 24);
                    if (I32(col+20) != self) continue;
                    foreach (int reference in Matches(BitConverter.GetBytes(checked(imageBase + (ulong)self))))
                    {
                        if ((reference & 7) != 0) continue;
                        Rva(reference, checked((slots[slots.Length-1]+2)*8));
                        long[] values = new long[slots.Length];
                        for (int i = 0; i < slots.Length; i++)
                            values[i] = checked((long)(BitConverter.ToUInt64(image, reference + (slots[i]+1)*8) - imageBase));
                        if (found != null) throw new InvalidDataException("Ambiguous presence interface.");
                        found = values;
                    }
                }
            }
            if (found == null) throw new InvalidDataException("Missing presence interface.");
            return found;
        }
        public static SteamBuild.Profile Locate(byte[] image)
        {
            var reader = new PresenceImageLocator(image);
            return new SteamBuild.Profile(reader.ReadSlots(".?AVIClientSharedConnectionMap@@",0,1,2),
                reader.ReadSlots(".?AVCSteamClient@@",13,69),reader.ReadSlots(".?AVIClientFriendsMap@@",23,24));
        }
        public static SteamBuild.Profile ReadAudited(Stream stream)
        {
            if (stream.Length > 128 * 1024 * 1024) throw new InvalidDataException("Steam image exceeds limit.");
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                byte[] bytes = buffer.ToArray();
                var located = Locate(bytes);
                bool equivalent;
                var audited = SteamBuild.Identify(bytes, out equivalent);
                if (audited == null || !Equal(located.Shared,audited.Shared) || !Equal(located.Engine,audited.Engine) || !Equal(located.Friends,audited.Friends))
                    throw new InvalidDataException("Presence locator audit failed; sending disabled.");
                return located;
            }
        }
        static bool Equal(long[] a, long[] b) { return System.Linq.Enumerable.SequenceEqual(a,b); }
    }
}
