using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tools.Models.Common;

namespace LanguageTool
{
    public class LanguageFile
    {
        private const string ref_signature = "LANG";

        public byte[] Signature { get; set; }
        public int StringCount { get; set; }
        public Language[] Languages { get; set; }

        public LanguageFile()
        {
        }

        public LanguageFile(Stream stream)
        {
            long origin = stream.Position;

            BinaryReader br = new BinaryReader(stream);

            #region Header (8 bytes)
            Signature = br.ReadBytes(4);
            if (Encoding.UTF8.GetString(Signature) != ref_signature)
                throw new InvalidDataException("Wrong Signature");

            int buffer = br.ReadInt32();
            StringCount = (buffer & 0xFFFFFF) - 1;
            int lang_count = buffer >> 24;
            #endregion

            Languages = new Language[lang_count];

            BinaryReader offsets = new BinaryReader(new MemoryStream(br.ReadBytes(lang_count * 4)));

            for (int i = 0, offset; i < lang_count; ++i)
            {
                offset = offsets.ReadInt32();
                if (offset != 0)
                {
                    stream.Position = offset + origin;
                    Languages[i] = new Language(stream, StringCount);
                }
            }
        }

        public string toJson(int offset = 0)
        {
            string ret = new string(' ', offset * 2) + "{\n";
            string ofs = new string(' ', (offset + 1) * 2);
            ret += $"{ofs}\"Signature\": \"{Encoding.UTF8.GetString(Signature)}\",\n";
            ret += $"{ofs}\"StringCount\": {StringCount},\n";
            ret += $"{ofs}\"Languages\": [";
            if (Languages.Length == 0)
                ret += "]\n";
            else
            {
                ret += '\n' + Languages[0].toJson(offset + 2);
                for (int i = 1; i < Languages.Length; ++i)
                    if (Languages[i] != null)
                        ret += ",\n" + Languages[i].toJson(offset + 2);
                    else
                        ret += $",\n{ofs}  null";
                ret += $"\n{ofs}]\n";
            }
            ret += new string(' ', offset * 2) + '}';

            return ret;
        }

        public BrokenRules Validate()
        {
            BrokenRules brokenRules = new BrokenRules();

            if (Encoding.UTF8.GetString(Signature) != ref_signature)
                brokenRules.Add(new BrokenRule("Signature invalid!"));

            foreach (Language lang in Languages)
                if (lang != null)
                    brokenRules.AddRange(lang.Validate(StringCount));

            return brokenRules;
        }
    }
}
