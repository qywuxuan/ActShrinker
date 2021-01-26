using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ActShrinker
{
    class _7zaHelper
    {
        const string PREFIX = "ActRes_";
        const string TOOL_7ZA_ROOT_NAME = @"ActShrinker\tools\7z_win\7za.exe";

        static string tool7zaPath
        {
            get
            {
                return Path.Combine(DirHelper.rootDir, TOOL_7ZA_ROOT_NAME);
            }
        }

        public static void Archive()
        {
            Clear();

            Zip();
        }

        static void Clear()
        {
            var oldArchives = new List<string>(IOHelper.GetAllFiles(DirHelper.curDir, SearchOption.TopDirectoryOnly, @".+\.zip"));

            foreach (var oldArchive in oldArchives)
            {
                IOHelper.DeleteFile(oldArchive);
            }
        }

        static void Zip()
        {
            var archive = PREFIX + DateTime.Now.ToString("yyyyMMdd_HHmm");

            var process = new Process();
            process.StartInfo.FileName = tool7zaPath;
            process.StartInfo.Arguments = string.Format("a {0}.zip ./ActRes/*", archive);
            process.Start();
            process.WaitForExit();
        }
    }
}
