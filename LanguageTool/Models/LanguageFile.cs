using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LanguageTool
{
    class LanguageFile
    {
        private const string ref_signature = "LANG";

        public byte[] Signature { get; private set; }
        public int StringCount { get; private set; }
        public Language[] Languages { get; set; }


        private LanguageFile(int string_count, int language_count)
        {
            Signature = Encoding.UTF8.GetBytes(ref_signature);
            StringCount = string_count;
            Languages = new Language[language_count];
        }

        public static LanguageFile Parse(Stream stream)
        {
            long origin = stream.Position;

            BinaryReader br = new BinaryReader(stream);

            #region Header (8 bytes)
            byte[] buffer = br.ReadBytes(4);
            if (Encoding.UTF8.GetString(buffer) != ref_signature)
                throw new InvalidDataException("Wrong Signature");

            buffer = br.ReadBytes(4);
            int string_count = (buffer[2] << 16) + (buffer[1] << 8) + buffer[0] - 1,
                lang_count = buffer[3];
            #endregion

            LanguageFile languageFile = new LanguageFile(string_count, lang_count);

            byte[] offsets = br.ReadBytes(lang_count * 4);

            for (int i = 0, offset; i < lang_count; ++i)
            {
                offset = (offsets[4 * i + 3] << 24) + (offsets[4 * i + 2] << 16) + (offsets[4 * i + 1] << 8) + offsets[4 * i];
                if (offset != 0)
                {
                    stream.Position = offset + origin;
                    languageFile.Languages[i] = Language.Parse(stream, string_count);
                }
            }

            return languageFile;
        }

        public long WriteToStream(Stream stream)
        {
            long origin = stream.Position;

            BinaryWriter bw = new BinaryWriter(stream);

            bw.Write(Signature);

            int string_count = StringCount + 1,
                lang_count = Languages.Length;
            
            byte[] buffer = new byte[4];
            buffer[0] = (byte)string_count;
            buffer[1] = (byte)(string_count >> 8);
            buffer[2] = (byte)(string_count >> 16);
            buffer[3] = (byte)lang_count;
            bw.Write(buffer);

            long offset = 8 + lang_count * 4,
                 old_pos;

            for (int i = 0; i < lang_count; ++i)
            {
                if (Languages[i] == null)
                    bw.Write(0);
                else
                {
                    bw.Write((int)offset);

                    old_pos = stream.Position;

                    stream.Position = offset + origin;
                    offset += Languages[i].WriteToStream(stream);

                    stream.Position = old_pos;
                }
            }

            return stream.Position - origin;
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
    }
}
