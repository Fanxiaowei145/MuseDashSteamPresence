using System;
using System.IO;
using System.Text;
using System.Xml;

namespace MuseDashSteamPresence
{
    public sealed class PresenceSettings
    {
        public bool ShowIdle = false;
        public string IdleText = "选歌中……";
        public bool ShowSong = true;
        public bool ShowAuthor = false;
        public string Separator = "_";
        public const string FileName = "settings.xml";

        public string Fingerprint
        {
            get { return ShowIdle + "|" + Encode(IdleText) + "|" + ShowSong + "|" + ShowAuthor + "|" + Encode(Separator); }
        }
        static string Encode(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? "")); }
        public static PresenceSettings Load(string path)
        {
            if (!File.Exists(path)) return new PresenceSettings();
            if (new FileInfo(path).Length > 32768) throw new InvalidDataException("配置文件过大。");
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            var doc = new XmlDocument { XmlResolver = null, PreserveWhitespace = true };
            using (var reader = XmlReader.Create(path, readerSettings)) doc.Load(reader);
            var root = doc.DocumentElement;
            if (root == null || root.Name != "MuseDashPresence" || root.GetAttribute("version") != "1")
                throw new InvalidDataException("无法识别的配置格式。");
            var s = new PresenceSettings();
            s.ShowIdle = ReadBool(root, "ShowIdle", s.ShowIdle);
            s.ShowSong = ReadBool(root, "ShowSong", s.ShowSong);
            s.ShowAuthor = ReadBool(root, "ShowAuthor", s.ShowAuthor);
            if (root["IdleText"] != null) s.IdleText = root["IdleText"].InnerText;
            if (root["Separator"] != null) s.Separator = root["Separator"].InnerText;
            s.Validate(); return s;
        }
        static bool ReadBool(XmlElement root, string name, bool fallback)
        {
            return root[name] == null ? fallback : XmlConvert.ToBoolean(root[name].InnerText);
        }
        public void Validate()
        {
            if ((IdleText ?? "").Length > 512) throw new InvalidDataException("自定义状态最多 512 个字符。");
            if ((Separator ?? "").Length > 32) throw new InvalidDataException("分隔符最多 32 个字符。");
            XmlConvert.VerifyXmlChars(IdleText ?? ""); XmlConvert.VerifyXmlChars(Separator ?? "");
        }
        public void Save(string path)
        {
            Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var w = XmlWriter.Create(temp, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
                {
                    w.WriteStartElement("MuseDashPresence"); w.WriteAttributeString("version", "1");
                    w.WriteElementString("ShowIdle", XmlConvert.ToString(ShowIdle));
                    w.WriteElementString("IdleText", IdleText ?? "");
                    w.WriteElementString("ShowSong", XmlConvert.ToString(ShowSong));
                    w.WriteElementString("ShowAuthor", XmlConvert.ToString(ShowAuthor));
                    w.WriteElementString("Separator", Separator ?? ""); w.WriteEndElement();
                }
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
