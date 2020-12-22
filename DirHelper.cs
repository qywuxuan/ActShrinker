using System;
using System.IO;

namespace ActShrinker
{
    class DirHelper
    {
        public const string ORIGIN_ROOT_NAME = "活动动态资源";
        public const string OUTPUT_ROOT_NAME = @"ActShrinker\ActRes";

        public static string rootDir
        {
            get
            {
                return IOHelper.GetDirectoryPath(curDir);
            }
        }

        public static string curDir
        {
            get
            {
                return Environment.CurrentDirectory;
            }
        }

        public static string inputDir
        {
            get
            {
                return Path.Combine(rootDir, ORIGIN_ROOT_NAME);
            }
        }

        public static string outputDir
        {
            get
            {
                return Path.Combine(rootDir, OUTPUT_ROOT_NAME);
            }
        }
    }
}
