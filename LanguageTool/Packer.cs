using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LanguageTool
{
    public class Packer
    {
        public static void Unpack(Stream stm, string output_folder)
        {
            if (!Directory.Exists(output_folder))
                Directory.CreateDirectory(output_folder);

            LanguageFile languageFile = LanguageFile.Parse(stm);

            File.WriteAllText($@"{output_folder}\header.json", languageFile.toJson());
        }

        public static void Pack()
        {
            // using (FileStream fs = new FileStream($@"{output_folder}\out.dat", FileMode.Create, FileAccess.ReadWrite))
                // languageFile.WriteToStream(fs);
        }
    }
}
