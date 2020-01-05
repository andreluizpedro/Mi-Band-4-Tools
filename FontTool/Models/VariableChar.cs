using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace FontTool.Models
{
    class VariableChar : ICharacter
    {
        public Bitmap Image { get; set; }

        public byte Width { get; set; }

        public static VariableChar Parse(BinaryReader binaryReader, int width, int height)
        {
            VariableChar vChar = new VariableChar() { Image = ICharacter.ParseImage(binaryReader, width, height) };
            vChar.Width = binaryReader.ReadByte();
            return vChar;
        }
    }
}
