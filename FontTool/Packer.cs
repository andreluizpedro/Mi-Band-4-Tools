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
    public class Packer
    {
        private const string font_24_folder = "bmp-24", font_16_folder = "bmp-16";

        // TODO: bool IsFontValid(string font_path)

        public static void Unpack(string font_path)
        {
            Console.WriteLine($"\nUnpacking {font_path}\n");

            string dir = Directory.GetParent(font_path).FullName;

            using (FileStream fs = new FileStream(font_path, FileMode.Open, FileAccess.Read))
                Unpack(fs, dir);
        }

        public static long Unpack(Stream stream, string output_dir)
        {
            long origin = stream.Position;

            BinaryReader br = new BinaryReader(stream);

            long offset;
            byte fontVer;

            #region Header (32 bytes)
            {
                stream.Position = origin + 0x04;
                fontVer = br.ReadByte();
                Console.WriteLine($"Font Version:  {fontVer}");

                switch (fontVer)
                {
                    case 1:
                    case 2:
                        break;
                    default:
                        throw new InvalidDataException("Invalid version / version not supported");
                }

                // Offset to 16px fonts
                stream.Position = origin + 0x1C;
                offset = br.ReadUInt32();
                Console.WriteLine($"Offset:        0x{offset.ToString("X4")}");
            }
            #endregion

            #region Unpacking
            switch (fontVer)
            {
                case 1:
                    stream.Position = origin + 0x20;
                    UnpackVariable(stream, 24, 24, @$"{output_dir}\{font_24_folder}");
                    if (offset != 0xFFFFFFFF)
                    {
                        stream.Position = origin + 0x20 + offset;
                        UnpackFixed(stream, 16, 20, @$"{output_dir}\{font_16_folder}");
                    }
                    break;
                case 2:
                    stream.Position = origin + 0x20;
                    UnpackFixed(stream, 24, 18, @$"{output_dir}\{font_24_folder}");
                    if (offset != 0xFFFFFFFF)
                    {
                        stream.Position = origin + 0x20 + offset;
                        UnpackFixed(stream, 16, 20, @$"{output_dir}\{font_16_folder}");
                    }
                    break;
                default:
                    throw new InvalidDataException("Invalid version / version not supported");
            }
            #endregion

            return stream.Position - origin;
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
                long offset = 0;
                byte[] header;

                switch (font_ver)
                {
                    case 1:
                        offset = PackVariable(fs, 0x20, $@"{dir}\{font_24_folder}", 24, 24);
                        PackFixed(fs, 0x20 + offset, $@"{dir}\{font_16_folder}", 16, 20);
                        break;
                    case 2:
                        offset = PackFixed(fs, 0x20, $@"{dir}\{font_24_folder}", 24, 18);
                        PackFixed(fs, 0x20 + offset, $@"{dir}\{font_16_folder}", 16, 20);
                        break;
                }

                header = GenerateHeader(font_ver, offset);
                fs.Position = 0;
                fs.Write(header, 0, 32);
            }
        }


        private static long PackFixed(Stream stream, string font_folder, int width, int height)
        {
            long origin = stream.Position;

            BinaryWriter bw = new BinaryWriter(stream);

            string[] files = Directory.EnumerateFiles(font_folder).Where(fn => Regex.IsMatch(fn, @"\\[0-9a-fA-F]{4}.bmp$")).ToArray();

            #region Ranges
            Console.WriteLine("Generating ranges...");
            byte[] ranges = GenerateRanges(files);
            int range_count = ranges.Length / 6;
            Console.WriteLine($"Generated {range_count} ranges");
            bw.Write((ushort)range_count);
            bw.Write(ranges);
            #endregion

            #region Write BMP
            int width_bytes = width / 8;
            int char_size = width_bytes * height;

            using BinaryReader rangesBR = new BinaryReader(new MemoryStream(ranges));

            byte[] char_data = new byte[char_size];
            for (int range_nr = 0, r_start, r_end; range_nr < range_count; ++range_nr)
            {
                r_start = rangesBR.ReadUInt16();
                r_end = rangesBR.ReadUInt16();
                rangesBR.BaseStream.Position += 2;

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

                    bw.Write(char_data);
                }
            }
            Console.Write(new string(' ', Console.WindowWidth));
            #endregion

            return stream.Position - origin;
        }

        private static long PackVariable(Stream stream, string font_folder, int width, int height)
        {
            long origin = stream.Position;

            BinaryWriter bw = new BinaryWriter(stream);

            string[] files = Directory.EnumerateFiles(font_folder).Where(fn => Regex.IsMatch(fn, @"\\[0-9a-fA-F]{6}.bmp$")).ToArray();

            #region Ranges
            Console.WriteLine("Generating ranges...");
            byte[] ranges = GenerateRanges(files);
            int range_count = ranges.Length / 6;
            Console.WriteLine($"Generated {range_count} ranges");
            bw.Write((ushort)range_count);
            bw.Write(ranges);
            #endregion

            #region Write BMP
            int width_bytes = width / 8;
            int char_size = width_bytes * height;

            using BinaryReader rangesBR = new BinaryReader(new MemoryStream(ranges));

            byte[] char_data = new byte[char_size];
            for (int range_nr = 0, i = 0, r_start, r_end; range_nr < range_count; ++range_nr)
            {
                r_start = rangesBR.ReadUInt16();
                r_end = rangesBR.ReadUInt16();
                rangesBR.BaseStream.Position += 2;

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

                    bw.Write(char_data);
                    bw.Write((byte)HexToInt(filename.Split('\\').Last().Substring(4, 2)));
                }
            }
            Console.Write(new string(' ', Console.WindowWidth));
            #endregion

            return stream.Position - origin;
        }

        private static byte[] GenerateRanges(string[] files)
        {
            List<byte> ranges = new List<byte>();

            string file = files[0].Split('\\').Last();
            int n = files.Length,
                range_start = HexToInt(file.Substring(0, 4)),
                range_end = range_start,
                range_offset = 0;

            void appendRanges(int start, int end, int offset) => ranges.AddRange(new List<byte> { (byte)start, (byte)(start >> 8), (byte)end, (byte)(end >> 8), (byte)offset, (byte)(offset >> 8) });

            for (int i = 1, tmp; i < n; ++i)
            {
                file = files[i].Split('\\').Last();
                tmp = HexToInt(file.Substring(0, 4));

                if (tmp == range_end + 1)
                    ++range_end;
                else if (tmp > range_end + 1)
                {
                    appendRanges(range_start, range_end, range_offset);

                    range_offset += range_end - range_start + 1;
                    range_start = range_end = tmp;
                }
                else
                    throw new InvalidDataException();

            }
            appendRanges(range_start, range_end, range_offset);

            return ranges.ToArray();
        }

        private static long UnpackFixed(Stream stream, int width, int height, string export_dir)
        {
            long origin = stream.Position;

            BinaryReader br = new BinaryReader(stream);

            if (!Directory.Exists(export_dir))
                Directory.CreateDirectory(export_dir);

            int r_start, r_end,
                range_count = br.ReadUInt16();
            using BinaryReader ranges = new BinaryReader(new MemoryStream(br.ReadBytes(range_count)));

            int width_bytes = width / 8,
                char_size = width_bytes * height;

            byte[] char_data;
            Bitmap bmp; BitmapData bmpData;
            for (int range_nr = 0; range_nr < range_count; ++range_nr)
            {
                r_start = ranges.ReadUInt16();
                r_end = ranges.ReadUInt16();
                ranges.BaseStream.Position += 2;

                for (; r_start <= r_end; ++r_start)
                {
                    if (r_start % 10 == 0)
                        Console.Write($"Range: {range_nr}/{range_count}    Char: {r_start}/{r_end}\r");

                    char_data = br.ReadBytes(char_size);

                    bmp = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
                    unsafe
                    {
                        bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, bmp.PixelFormat);

                        byte* line = (byte*)bmpData.Scan0;
                        for (int y = 0, x; y < height; ++y)
                        {
                            for (x = 0; x < width_bytes; ++x)
                                line[x] = char_data[width_bytes * y + x];

                            line += bmpData.Stride;
                        }

                        bmp.UnlockBits(bmpData);
                    }
                    bmp.Save(@$"{export_dir}\{r_start.ToString("x4")}.bmp");
                }
            }

            return stream.Position - origin;
        }

        private static long UnpackVariable(Stream stream, int width, int height, string export_dir)
        {
            long origin = stream.Position;

            BinaryReader br = new BinaryReader(stream);

            if (!Directory.Exists(export_dir))
                Directory.CreateDirectory(export_dir);

            int r_start, r_end,
                range_count = br.ReadUInt16();

            using BinaryReader ranges = new BinaryReader(new MemoryStream(br.ReadBytes(range_count * 6)));

            int width_bytes = width / 8,
                char_size = width_bytes * height,
                char_width;

            byte[] char_data;
            Bitmap bmp; BitmapData bmpData;
            for (int range_nr = 0; range_nr < range_count; ++range_nr)
            {
                r_start = ranges.ReadUInt16();
                r_end = ranges.ReadUInt16();
                ranges.BaseStream.Position += 2;

                for (; r_start <= r_end; ++r_start)
                {
                    if (r_start % 10 == 0)
                        Console.Write($"Range: {range_nr}/{range_count}    Char: {r_start}/{r_end}\r");

                    char_data = br.ReadBytes(char_size);
                    char_width = br.ReadByte();

                    bmp = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
                    unsafe
                    {
                        bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, bmp.PixelFormat);

                        byte* line = (byte*)bmpData.Scan0;
                        for (int y = 0, x; y < height; ++y)
                        {
                            for (x = 0; x < width_bytes; ++x)
                                line[x] = char_data[width_bytes * y + x];

                            line += bmpData.Stride;
                        }

                        bmp.UnlockBits(bmpData);
                    }
                    bmp.Save(@$"{export_dir}\{r_start.ToString("x4")}{char_width.ToString("x2")}.bmp");
                }
            }

            return stream.Position - origin;
        }

        private static byte[] GenerateHeader(byte font_ver, uint offset_to_16px)
        {
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
