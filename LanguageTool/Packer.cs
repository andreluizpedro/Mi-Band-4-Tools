using LanguageTool.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tools.Models.Common;

namespace LanguageTool
{
    public class Packer
    {
        public static void Unpack(FileInfo langFile, DirectoryInfo output_folder)
        {
            LanguageFile languageFile;
            using (FileStream stream = langFile.OpenRead())
                languageFile = new LanguageFile(stream);

            Extract(languageFile, output_folder);
        }

        public static void Extract(LanguageFile languageFile, DirectoryInfo output_folder)
        {
            if (!output_folder.Exists)
                output_folder.Create();

            File.WriteAllText($@"{output_folder}\header.json", languageFile.toJson());

            foreach (Language language in languageFile.Languages)
                if (language != null)
                    language.exportAsTxt(new FileInfo(@$"{output_folder}\{language.Name.AsSafe()}.txt"));
        }

        public static void Pack(FileInfo headerFile, FileInfo output)
        {
            LanguageFile languageFile = Collect(headerFile);

            using (FileStream stream = output.Create())
                stream.Write(languageFile);
        }

        public static LanguageFile Collect(FileInfo headerFile)
        {
            JObject header = JObject.Parse(File.ReadAllText(headerFile.FullName));

            LanguageFile languageFile = new LanguageFile();

            #region Read header.json
            languageFile.Signature = Encoding.UTF8.GetBytes((string)header["Signature"]);
            languageFile.StringCount = (int)header["StringCount"];
            JArray langs = (JArray)header["Languages"];
            languageFile.Languages = new Language[langs.Count];
            #endregion

            for (int i = 0; i < langs.Count; ++i)
                if (langs[i].HasValues)
                    languageFile.Languages[i] = new Language(new FileInfo(@$"{headerFile.DirectoryName}\{(string)langs[i]["Name"]}.txt"), (string)langs[i]["Name"], languageFile.StringCount);

            return languageFile;
        }
    }
}
