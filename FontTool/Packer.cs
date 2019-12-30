using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FontTool
{
    class Packer
    {
        private const string font_24_folder = "bmp-24", font_16_folder = "bmp-latin";

        public static void Unpack(string font_path)
        {
            Console.WriteLine($"\nUnpacking {font_path}\n");

            // Get directory path
            string dir = Directory.GetParent(font_path).FullName;

            using (FileStream fs = new FileStream(font_path, FileMode.Open, FileAccess.Read))
            {
                int offset;
                byte fontVer;

                #region Header (32 bytes)
                {
                    byte[] buffer = readBytes(fs, 32);

                    // Font Version and Flags
                    fontVer = buffer[0x04];
                    Console.WriteLine($"Font Version:  {fontVer}");

                    switch (fontVer)
                    {
                        case 1:
                        case 2:
                            break;
                        default:
                            throw new InvalidDataException("Invalid version / version not supported");
                    }

                    // Offset to fixed size fonts
                    offset = (buffer[0x1F] << 24) + (buffer[0x1E] << 16) + (buffer[0x1D] << 8) + buffer[0x1C];
                    Console.WriteLine($"Offset:        0x{offset.ToString("X2")}");
                }
                #endregion

                #region Unpacking
                switch (fontVer)
                {
                    case 1:
                        if (offset > 0)
                            ExportRanges(fs, 0x20, 24, 24, @$"{dir}\{font_24_folder}", fontVer);
                        if ((uint)offset != 0xFFFFFFFF)
                            ExportRanges(fs, 0x20 + offset, 16, 20, @$"{dir}\{font_16_folder}", fontVer);
                        break;
                    case 2:
                        if (offset > 0)
                            ExportRanges(fs, 0x20, 24, 18, @$"{dir}\{font_24_folder}", fontVer);
                        if ((uint)offset != 0xFFFFFFFF)
                            ExportRanges(fs, 0x20 + offset, 16, 20, @$"{dir}\{font_16_folder}", fontVer);
                        break;
                    default:
                        throw new InvalidDataException("Invalid version / version not supported");
                }
                #endregion
            }
        }

        public static void Pack(string new_font_path, byte font_ver)
        {
            switch (font_ver)
            {
                case 1:
                case 2:
                    break;
                default:
                    throw new InvalidDataException("Invalid version / version not supported");
            }

            Console.WriteLine($"\nPacking {new_font_path}\n");

            // Get directory path
            string dir = Directory.GetParent(new_font_path).FullName;

            using (FileStream fs = new FileStream(new_font_path, FileMode.Create, FileAccess.Write))
            {
                uint offset = 0;
                byte[] header;

                switch (font_ver)
                {
                    case 1:
                        offset = WriteVariable(fs, 0x20, $@"{dir}\{font_24_folder}", 24, 24);
                        WriteFixed(fs, 0x20 + offset, $@"{dir}\{font_16_folder}", 16, 20);
                        break;
                    case 2:
                        offset = WriteFixed(fs, 0x20, $@"{dir}\{font_24_folder}", 24, 18);
                        WriteFixed(fs, 0x20 + offset, $@"{dir}\{font_16_folder}", 16, 20);
                        break;
                }

                header = GenerateHeader(font_ver, offset);
                fs.Position = 0;
                fs.Write(header, 0, 32);
            }
        }


        private static uint WriteFixed(FileStream fs, uint offset, string font_folder, int width, int height)
        {
            uint fsoldpos = (uint)fs.Position;
            fs.Position = offset;

            string[] files = Directory.EnumerateFiles(font_folder).Where(fn => Regex.IsMatch(fn, @"\\[0-9a-fA-F]{4}.bmp$")).ToArray();

            #region Ranges
            Console.WriteLine("Generating ranges...");
            byte[] ranges = GenerateRanges(files);
            int range_count = ranges.Length / 6;
            Console.WriteLine($"Generated {range_count} ranges");
            fs.WriteByte((byte)range_count);
            fs.WriteByte((byte)(range_count >> 8));
            fs.Write(ranges);
            #endregion

            #region Write BMP
            int width_bytes = width / 8;
            int char_size = width_bytes * height;

            byte[] char_data = new byte[char_size];

            for (int range_nr = 0, r_start, r_end; range_nr < range_count; ++range_nr)
            {
                r_start = (ranges[6 * range_nr + 1] << 8) + ranges[6 * range_nr];
                r_end = (ranges[6 * range_nr + 3] << 8) + ranges[6 * range_nr + 2];

                for (; r_start <= r_end; ++r_start)
                {
                    if (r_start % 10 == 0)
                        Console.Write($"Range: {range_nr}/{range_count}    Char: {r_start}/{r_end}\r");

                    #region Read BMP
                    Bitmap bmpi = new Bitmap($@"{font_folder}\{r_start.ToString("x4")}.bmp");

                    if (bmpi.Width != width || bmpi.Height != height || bmpi.PixelFormat != PixelFormat.Format1bppIndexed)
                        throw new InvalidDataException("Invalid file size/format");

                    unsafe
                    {
                        BitmapData bitmapData = bmpi.LockBits(new Rectangle(Point.Empty, bmpi.Size), ImageLockMode.ReadOnly, bmpi.PixelFormat);

                        byte* line = (byte*)bitmapData.Scan0;
                        for (int y = 0, x; y < height; ++y)
                        {
                            for (x = 0; x < width_bytes; ++x)
                                char_data[width_bytes * y + x] = line[x];

                            line += bitmapData.Stride;
                        }

                        bmpi.UnlockBits(bitmapData);
                    }
                    #endregion

                    fs.Write(char_data, 0, char_size);
                }
            }
            #endregion
            Console.Write(new string(' ', Console.WindowWidth));

            // Returns bytes written and restores filestream position
            offset = (uint)fs.Position - offset;
            fs.Position = fsoldpos;
            return offset;
        }

        private static uint WriteVariable(FileStream fs, uint offset, string font_folder, int width, int height)
        {
            uint fsoldpos = (uint)fs.Position;
            fs.Position = offset;

            string[] files = Directory.EnumerateFiles(font_folder).Where(fn => Regex.IsMatch(fn, @"\\[0-9a-fA-F]{6}.bmp$")).ToArray();

            #region Ranges
            Console.WriteLine("Generating ranges...");
            byte[] ranges = GenerateRanges(files);
            int range_count = ranges.Length / 6;
            Console.WriteLine($"Generated {range_count} ranges");
            fs.WriteByte((byte)range_count);
            fs.WriteByte((byte)(range_count >> 8));
            fs.Write(ranges);
            #endregion

            #region Write BMP
            int width_bytes = width / 8;
            int char_size = width_bytes * height;

            byte[] char_data = new byte[char_size];

            for (int range_nr = 0, i = 0, r_start, r_end; range_nr < range_count; ++range_nr)
            {
                r_start = (ranges[6 * range_nr + 1] << 8) + ranges[6 * range_nr];
                r_end = (ranges[6 * range_nr + 3] << 8) + ranges[6 * range_nr + 2];

                for (string filename; r_start <= r_end; ++r_start, ++i)
                {
                    if (r_start % 10 == 0)
                        Console.Write($"Range: {range_nr}/{range_count}    Char: {r_start}/{r_end}\r");

                    // filename = Directory.GetFiles(font_folder, @$"{r_start.ToString("x4")}??.bmp")[0];
                    filename = files[i];
                    if (r_start.ToString("x4") != filename.Substring(filename.LastIndexOf('\\') + 1, 4))
                        filename = files.Single(fn => Regex.IsMatch(fn, @$"\\{r_start.ToString("x4")}[0-9a-fA-F]{{2}}.bmp$"));
                    
                    #region Read BMP
                    Bitmap bmpi = new Bitmap(filename);

                    if (bmpi.Width != width || bmpi.Height != height || bmpi.PixelFormat != PixelFormat.Format1bppIndexed)
                        throw new InvalidDataException("Invalid file size/format");

                    unsafe
                    {
                        BitmapData bitmapData = bmpi.LockBits(new Rectangle(Point.Empty, bmpi.Size), ImageLockMode.ReadOnly, bmpi.PixelFormat);

                        byte* line = (byte*)bitmapData.Scan0;
                        for (int y = 0, x; y < height; ++y)
                        {
                            for (x = 0; x < width_bytes; ++x)
                                char_data[width_bytes * y + x] = line[x];

                            line += bitmapData.Stride;
                        }

                        bmpi.UnlockBits(bitmapData);
                    }
                    #endregion

                    fs.Write(char_data, 0, char_size);
                    fs.WriteByte((byte)HexToInt(filename.Split('\\').Last().Substring(4, 2)));
                }
            }
            #endregion
            Console.Write(new string(' ', Console.WindowWidth));

            // Returns bytes written and restores filestream position
            offset = (uint)fs.Position - offset;
            fs.Position = fsoldpos;
            return offset;
        }

        private static byte[] GenerateRanges(string[] files)
        {
            List<byte> ranges = new List<byte>();

            string file = files[0].Split('\\').Last();
            int n = files.Length,
                range_start = HexToInt(file.Substring(0, 4)),
                range_end = range_start,
                range_offset = 0;

            void appendRanges(int start, int end, int offset)
            {
                ranges.AddRange(new List<byte> { (byte)range_start, (byte)(range_start >> 8), (byte)range_end, (byte)(range_end >> 8), (byte)range_offset, (byte)(range_offset >> 8) });
            }

            for (int i = 1, tmp; i < n; ++i)
            {
                file = files[i].Split('\\').Last();
                tmp = HexToInt(file.Substring(0, 4));

                if (tmp > range_end + 1)
                {
                    appendRanges(range_start, range_end, range_offset);

                    range_offset += range_end - range_start + 1;
                    range_start = range_end = tmp;
                }
                else
                    ++range_end;
            }
            appendRanges(range_start, range_end, range_offset);

            return ranges.ToArray();
        }

        private static byte[] GenerateHeader(byte font_ver, uint offset_to_16px)
        {
            // 0x04 -> version; 0x0A -> flags; 
            byte[] header = { 0x4E, 0x45, 0x5A, 0x4B, 0x00, 0xFF, 0xFF, 0xFF,
                              0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF,
                              0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                              0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 };

            // 0x04 -> Version
            header[0x04] = font_ver;

            // 0x0A -> Flags
            switch (font_ver)
            {
                case 1:
                    header[0x0A] = 3;
                    break;
                case 2:
                    header[0x0A] = 6;
                    break;
                default:
                    throw new InvalidDataException("Invalid version / version not supported");
            }

            // 0x1C-0x1F -> Offset to 16px chars (Little endian)
            header[0x1C] = (byte)offset_to_16px;
            header[0x1D] = (byte)(offset_to_16px >>  8);
            header[0x1E] = (byte)(offset_to_16px >> 16);
            header[0x1F] = (byte)(offset_to_16px >> 24);

            return header;
        }

        private static void ExportRanges(FileStream fs, long offset, int width, int height, string export_dir, int font_ver)
        {
            byte[] buffer;
            fs.Position = offset;

            if (!Directory.Exists(export_dir))
                Directory.CreateDirectory(export_dir);

            #region Get Ranges Info
            buffer = readBytes(fs, 2);
            int ranges_count = (buffer[0x1] << 8) + buffer[0x0];

            byte[] ranges = readBytes(fs, ranges_count * 6);
            int start_range, end_range, num_characters;
            {
                int n = ranges_count * 6;
                start_range = (ranges[n - 5] << 8) + ranges[n - 6];
                end_range = (ranges[n - 3] << 8) + ranges[n - 4];
                num_characters = (ranges[n - 1] << 8) + ranges[n - 2] + end_range - start_range + 1;

                start_range = (ranges[1] << 8) + ranges[0];
                end_range = (ranges[3] << 8) + ranges[2];
            }
            #endregion

            #region Export Images
            Console.WriteLine($"Exporting {num_characters} characters...");
            int width_bytes = width / 8;
            int char_size = width_bytes * height;
            for (int range_nr = 0, i = 0; i < num_characters; ++i, ++start_range)
            {
                if (start_range > end_range)
                {
                    ++range_nr;
                    start_range = (ranges[range_nr * 6 + 1] << 8) + ranges[range_nr * 6];
                    end_range = (ranges[range_nr * 6 + 3] << 8) + ranges[range_nr * 6 + 2];
                }

                if (i % 10 == 0)
                    Console.Write($"{i}/{num_characters}\r");

                buffer = readBytes(fs, char_size);

                Bitmap bmpi = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
                unsafe
                {
                    BitmapData bitmapData = bmpi.LockBits(new Rectangle(Point.Empty, bmpi.Size), ImageLockMode.ReadWrite, bmpi.PixelFormat);
                    
                    byte* line = (byte*)bitmapData.Scan0;
                    for (int y = 0, x; y < height; ++y)
                    {
                        for (x = 0; x < width_bytes; ++x)
                            line[x] = buffer[width_bytes * y + x];

                        line += bitmapData.Stride;
                    }
                    
                    bmpi.UnlockBits(bitmapData);
                }

                switch (font_ver)
                {
                    case 1:
                        string margin_top = width == 24 ? ((int)readBytes(fs, 1)[0]).ToString("x2") : "";
                        bmpi.Save(@$"{export_dir}\{start_range.ToString("x4")}{margin_top}.bmp");
                        break;
                    case 2:
                        bmpi.Save(@$"{export_dir}\{start_range.ToString("x4")}.bmp");
                        break;
                    default:
                        throw new InvalidDataException("Invalid version / version not supported");
                }
            }
            Console.WriteLine($"{num_characters}/{num_characters}\nDone.");
            #endregion
        }

        private static byte[] readBytes(FileStream fs, int count)
        {
            byte[] buffer = new byte[count];
            if (fs.Read(buffer, 0, count) != count) throw new EndOfStreamException();
            return buffer;
        }

        private static int HexToInt(string hex)
        {
            hex = hex.ToUpper();
            if (!Regex.IsMatch(hex, @"^[0-9A-F]+$"))
                throw new InvalidDataException("Invalid Hex digit");

            int ret = 0, a, n = hex.Length;
            for (int i = 0; i < n; ++i)
            {
                a = hex[i];
                ret = (ret << 4) + (a > '9' ? a - 'A' + 10 : a - '0');
            }

            return ret;
        }
    }
}
