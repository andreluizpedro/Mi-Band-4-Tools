using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace FontTool.Models
{
    class FixedChar : ICharacter
    {
        public Bitmap Image { get; set; }

        public static FixedChar Parse(BinaryReader binaryReader, int width, int height)
        {
            return new FixedChar() { Image = ICharacter.ParseImage(binaryReader, width, height) };
        }
    }
}
