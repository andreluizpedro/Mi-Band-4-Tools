using System;
using System.IO;

namespace LanguageTool
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            string file = @"C:\Users\Jakob\Desktop\Mili_cinco_l\2250.dat";
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                Packer.Unpack(fs, $@"{Directory.GetParent(file)}\lang");
#endif
        }
    }
}
