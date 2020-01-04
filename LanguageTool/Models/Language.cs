using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LanguageTool
{
    public class Language
    {
        public string Name { get; private set; } // Max length = 0xFFF
        public string[] Strings { get; private set; } // Max length = 0x3FFFE

        private Language(string name, int string_count)
        {
            Name = name;
            Strings = new string[string_count];
        }

        public static Language Parse(Stream stream, int string_count)
        {
            long origin = stream.Position;

            BinaryReader br = new BinaryReader(stream);

            uint buffer = br.ReadUInt32();
            int header_length = (int)(buffer >> 12),
                name_length = (int)(buffer & 0xFFF);

            BinaryReader header = new BinaryReader(new MemoryStream(br.ReadBytes(header_length - 4)));

            Language language = new Language(Encoding.UTF8.GetString(br.ReadBytes(name_length)), string_count);

            for (int i = 0, offset, length; i < string_count; ++i)
            {
                buffer = header.ReadUInt32();
                offset = (int)(buffer >> 12);
                length = (int)(buffer & 0xFFF);

                stream.Position = origin + offset;
                language.Strings[i] = Encoding.UTF8.GetString(br.ReadBytes(length));
            }

            return language;
        }

        public long WriteToStream(Stream stream)
        {
            long origin = stream.Position;

            BinaryWriter bw = new BinaryWriter(stream);

            int header_length = (Strings.Length + 1) * 4;
            byte[] name_b = Encoding.UTF8.GetBytes(Name);

            uint buffer = (uint)(header_length << 12) + (uint)name_b.Length;
            bw.Write(buffer);

            int n = Strings.Length;
            for (int i = 0, offset = header_length + name_b.Length; i < n; ++i)
            {
                buffer = (uint)(offset << 12) + (uint)Strings[i].Length;
                bw.Write(buffer);
                offset += Strings[i].Length;
            }
            bw.Write(name_b);
            for (int i = 0; i < n; ++i)
                bw.Write(Encoding.UTF8.GetBytes(Strings[i]));

            return stream.Position - origin;
        }

        public string toJson(int offset = 0)
        {
            string ret = new string(' ', offset * 2) + "{\n";
            string ofs = new string(' ', (offset + 1) * 2);
            ret += $"{ofs}\"Name\": \"{Name.Replace("\n", @"\n").Replace("\t", @"\t").Replace("\r", @"\r")}\"\n";
            ret += new string(' ', offset * 2) + '}';

            return ret;
        }
    }
}
