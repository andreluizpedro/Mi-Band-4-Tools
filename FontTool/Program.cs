using System;
using System.IO;

namespace FontTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Help();
                return;
            }

            switch (args[0])
            {
                case "pack":
                    if (args.Length == 3)
                        Packer.Pack(args[2], byte.Parse(args[1]));
                    else
                        Help();
                    break;
                case "unpack":
                    if (args.Length == 2)
                        if (File.Exists(args[1]))
                            Packer.Unpack(args[1]);
                        else
                            Console.WriteLine("File does not exist!");
                    else
                        Help();
                    break;
                default:
                    Help();
                    break;
            }
        }

        static void Help()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  FontTool.exe unpack <path_to_file>");
            Console.WriteLine("  FontTool.exe pack <version> <path_to_new_file>");
        }
    }
}
