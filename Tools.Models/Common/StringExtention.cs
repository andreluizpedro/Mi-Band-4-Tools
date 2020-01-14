using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tools.Models.Common
{
    public static class StringExtention
    {
        public static string AsSafe(this string _string)
        {
            return string.Concat(_string.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
