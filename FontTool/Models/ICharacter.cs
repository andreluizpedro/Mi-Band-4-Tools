using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace FontTool.Models
{
    interface ICharacter
    {
        public Bitmap Image { get; set; }

        protected static Bitmap ParseImage(BinaryReader binaryReader, int width, int height)
        {
            int width_bytes = width / 8;

            byte[] char_data = binaryReader.ReadBytes(width_bytes * height);

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
            unsafe
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, bmp.PixelFormat);

                byte* line = (byte*)bmpData.Scan0;
                for (int y = 0, x; y < height; ++y)
                {
                    for (x = 0; x < width_bytes; ++x)
                        line[x] = char_data[width_bytes * y + x];

                    line += bmpData.Stride;
                }

                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        public long WriteToStream(Stream stream)
        {
            long origin = stream.Position;

            int height = Image.Height,
                width_bytes = Image.Width / 8;

            unsafe
            {
                BitmapData bitmapData = Image.LockBits(new Rectangle(Point.Empty, Image.Size), ImageLockMode.ReadOnly, Image.PixelFormat);

                byte* line = (byte*)bitmapData.Scan0;
                for (int y = 0, x; y < height; ++y)
                {
                    for (x = 0; x < width_bytes; ++x)
                        stream.WriteByte(line[x]);

                    line += bitmapData.Stride;
                }

                Image.UnlockBits(bitmapData);
            }

            if (this is VariableChar variableChar)
                stream.WriteByte(variableChar.Width);

            return stream.Position - origin;
        }
    }
}
