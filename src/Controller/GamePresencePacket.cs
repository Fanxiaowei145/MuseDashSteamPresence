using System;
using System.IO;
using System.Text;

namespace MuseDashSteamPresence
{
    internal static class GamePresencePacket
    {
        public const ulong AppId = 774171;
        static void Varint(Stream stream, uint value)
        {
            while (value > 127) { stream.WriteByte((byte)((value & 127) | 128)); value >>= 7; }
            stream.WriteByte((byte)value);
        }
        public static byte[] Create(string title, uint gameProcessId)
        {
            if (String.IsNullOrWhiteSpace(title) || gameProcessId == 0) throw new ArgumentException("Actual game and song required.");
            byte[] text = Encoding.UTF8.GetBytes(title);
            if (text.Length > 240) throw new ArgumentException("Title exceeds UTF-8 limit.");
            using (var game = new MemoryStream()) using (var packet = new MemoryStream())
            {
                game.WriteByte(0x11); // GamePlayed.game_id, fixed64; type 0 = real app.
                byte[] id = BitConverter.GetBytes(AppId);
                game.Write(id, 0, id.Length);
                game.WriteByte(0x3a); Varint(game, (uint)text.Length);
                game.Write(text, 0, text.Length);
                game.WriteByte(0x48); Varint(game, gameProcessId);
                // Steam SharedConnection expects the same full protobuf envelope
                // as Steam's own UI: EMsg|0x80000000, header size, header, body.
                byte[] envelope = BitConverter.GetBytes(5410U | 0x80000000U);
                packet.Write(envelope, 0, 4);
                packet.Write(new byte[4], 0, 4); // Empty header; current session is supplied by Steam.
                packet.WriteByte(0x0a); Varint(packet, (uint)game.Length);
                byte[] nested = game.ToArray(); packet.Write(nested, 0, nested.Length);
                return packet.ToArray();
            }
        }
    }
}
