using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LanguageTool.Common
{
    public static class StreamExtentions
    {
        /// <summary>
        /// Writes instance of Language to stream
        /// </summary>
        public static long Write(this Stream stream, Language language)
        {
            long origin = stream.Position;

            BinaryWriter bw = new BinaryWriter(stream);

            int header_length = (language.Strings.Length + 1) * 4;
            byte[] name_b = Encoding.UTF8.GetBytes(language.Name);

            uint buffer = (uint)(header_length << 12) + (uint)name_b.Length;
            bw.Write(buffer);

            int n = language.Strings.Length;
            for (int i = 0, offset = header_length + name_b.Length, s_len; i < n; ++i)
            {
                s_len = Encoding.UTF8.GetByteCount(language.Strings[i]);
                buffer = (uint)(offset << 12) + (uint)s_len;
                bw.Write(buffer);
                offset += s_len;
            }
            bw.Write(name_b);
            for (int i = 0; i < n; ++i)
                bw.Write(Encoding.UTF8.GetBytes(language.Strings[i]));

            return stream.Position - origin;
        }

        /// <summary>
        /// Writes instance of LanguageFile to stream
        /// </summary>
        public static long Write(this Stream stream, LanguageFile languageFile)
        {
            long origin = stream.Position;

            BinaryWriter bw = new BinaryWriter(stream);

            bw.Write(languageFile.Signature);

            int string_count = languageFile.StringCount + 1,
                lang_count = languageFile.Languages.Length;

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
                if (languageFile.Languages[i] == null)
                    bw.Write(0);
                else
                {
                    bw.Write((int)offset);

                    old_pos = stream.Position;

                    stream.Position = offset + origin;
                    offset += stream.Write(languageFile.Languages[i]);

                    stream.Position = old_pos;
                }
            }

            return stream.Position - origin;
        }
    }
}
