using System;
using System.Collections.Generic;
using System.Text;

namespace FontTool.Models
{
    public class FontFile
    {
        private const string ref_signature = "NEZK";

        public byte[] Signature { get; private set; }
        public byte Version { get; private set; }
        public uint Flags { get; set; }


    }
}
