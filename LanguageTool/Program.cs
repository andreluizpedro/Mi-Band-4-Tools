using System;
using System.IO;

namespace LanguageTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Help();

                #if DEBUG
                FileInfo tfile = new FileInfo(@"C:\Users\Jakob\Desktop\cinco_v86\2250.dat");
                DirectoryInfo output = new DirectoryInfo($@"{tfile.DirectoryName}\lang");
                Packer.Unpack(tfile, output);

                FileInfo outFile = new FileInfo($@"{output.FullName}\out.dat");
                Packer.Pack(new FileInfo($@"{output.FullName}\header.json"), outFile);

                LanguageFile languageFile1 = new LanguageFile(tfile.OpenRead());
                LanguageFile languageFile2 = new LanguageFile(outFile.OpenRead());
                #endif

                return;
            }

            FileInfo file = new FileInfo(args[0]);

            try
            {
                DirectoryInfo output = new DirectoryInfo($@"{file.DirectoryName}\lang");
                Packer.Unpack(file, output);
                return;
            }
            catch { }

            try
            {
                FileInfo outFile = new FileInfo($@"{file.DirectoryName}\out.dat");
                Packer.Pack(file, outFile);
                return;
            }
            catch { }

            Help();
        }

        static void Help()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  LanguageTool.exe <path_to_file>");
        }
    }
}
