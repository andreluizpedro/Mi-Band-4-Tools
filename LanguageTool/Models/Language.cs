using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tools.Models.Common;

namespace LanguageTool
{
    public class Language
    {
        public string Name { get; set; }
        public string[] Strings { get; set; }

        #region Constructors
        public Language(Stream stream, int string_count)
        {
            long origin = stream.Position;

            BinaryReader br = new BinaryReader(stream);

            uint buffer = br.ReadUInt32();
            int header_length = (int)(buffer >> 12),
                name_length = (int)(buffer & 0xFFF);

            BinaryReader header = new BinaryReader(new MemoryStream(br.ReadBytes(header_length - 4)));

            Name = Encoding.UTF8.GetString(br.ReadBytes(name_length));
            Strings = new string[string_count];

            for (int i = 0, offset, length; i < string_count; ++i)
            {
                buffer = header.ReadUInt32();
                offset = (int)(buffer >> 12);
                length = (int)(buffer & 0xFFF);

                stream.Position = origin + offset;
                Strings[i] = Encoding.UTF8.GetString(br.ReadBytes(length));
            }
        }

        public Language(FileInfo fileInfo, string name, int string_count)
        {
            Name = name;
            Strings = new string[string_count];

            using StreamReader sr = new StreamReader(fileInfo.OpenRead());

            for (int i = 0; i < string_count; ++i)
                Strings[i] = sr.ReadLine().Replace(@"\n", "\n").Replace(@"\t", "\t").Replace(@"\r", "\r"); ;
        }
        #endregion

        public string toJson(int offset = 0)
        {
            string ret = new string(' ', offset * 2) + "{\n";
            string ofs = new string(' ', (offset + 1) * 2);
            ret += $"{ofs}\"Name\": \"{Name.AsSafe()}\"\n";
            ret += new string(' ', offset * 2) + '}';

            return ret;
        }

        public void exportAsTxt(FileInfo output_file)
        {
            using StreamWriter sw = new StreamWriter(output_file.Create());

            foreach (string s in Strings)
                sw.WriteLine(s.Replace("\n", @"\n").Replace("\t", @"\t").Replace("\r", @"\r"));
        }

        public BrokenRules Validate(int expected_string_count)
        {
            BrokenRules brokenRules = new BrokenRules();

            if (string.IsNullOrEmpty(Name))
                brokenRules.Add(new BrokenRule("Language name is null or empty!", this));
            
            if (Strings == null)
                brokenRules.Add(new BrokenRule("Strings undefined!", this));

            if (brokenRules.Any())
                return brokenRules;

            if (Encoding.UTF8.GetByteCount(Name) > 0xFFF)
                brokenRules.Add(new BrokenRule("Language name too long!", this));

            if (Strings.Length != expected_string_count)
                brokenRules.Add(new BrokenRule($"String count ({Strings.Length}) != expected ({expected_string_count})", this));

            for (int i = 0; i < Strings.Length; ++i)
            {
                if (string.IsNullOrEmpty(Strings[i]))
                    brokenRules.Add(new BrokenRule($"String {i} is null or empty!", this));
                else if (Encoding.UTF8.GetByteCount(Strings[i]) > 0xFFF)
                    brokenRules.Add(new BrokenRule($"String {i} too long!", this));
            }

            return brokenRules;
        }
    }
}
