using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ActShrinker
{
    static class IOHelper
    {
        public static string[] GetAllFiles(string path, SearchOption searchOption = SearchOption.AllDirectories, string fileNameRegex = null)
        {
            if (!ExistsDirectory(path))
                return new string[0];

            var allFiles = new List<string>(Directory.GetFiles(path, "*.*", searchOption));

            if (fileNameRegex == null)
            {
                //do nothing
            }
            else
            {
                allFiles = allFiles.FindAll(filePath =>
                {
                    var fileName = Path.GetFileName(filePath);
                    return Regex.IsMatch(fileName, fileNameRegex);
                });
            }

            return allFiles.ToArray();
        }

        public static DirectoryInfo CreateDirectory(string directoryPath)
        {
            if (!ExistsDirectory(directoryPath))
            {
                return Directory.CreateDirectory(directoryPath);
            }
            else
            {
                return new DirectoryInfo(directoryPath);
            }
        }

        public static void DeleteDirectory(string directoryPath, bool recursive = true)
        {
            if (ExistsDirectory(directoryPath))
            {
                Directory.Delete(directoryPath, recursive);
            }
            else
            {
                //do nothing
            }
        }

        public static bool ExistsFile(string filePath)
        {
            return File.Exists(filePath);
        }

        public static void DeleteFile(string filePath)
        {
            if (ExistsFile(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                //do nothing
            }
        }

        public static int GetSmallestPO2(int size)
        {
            var i = 1;
            for (; ; )
            {
                var next = i << 1;

                if (size < next)
                {
                    return i;
                }

                i = next;
            }
        }

        public static int GetBiggestPO2(int size)
        {
            var i = 2;
            for (; ; )
            {
                if (size < i)
                {
                    return i;
                }

                i = i << 1;
            }
        }

        public static int GetNearestPO2(int size)
        {
            var p1 = GetSmallestPO2(size);
            var p2 = GetBiggestPO2(size);

            return p2 - size > size - p1 ? p1 : p2;
        }

        public static bool ExistsDirectory(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public static string GetDirectoryPath(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        public static void CopyFile(string souceFilePath, string destFilePath, bool overwrite = true)
        {
            var destDirectoryPath = GetDirectoryPath(destFilePath);

            if (!ExistsDirectory(destDirectoryPath))
                CreateDirectory(destDirectoryPath);

            File.Copy(souceFilePath, destFilePath, overwrite);
        }

        public static int GetFileSize(string path)
        {
            var fileInfo = new FileInfo(path);
            return (int)Math.Ceiling(fileInfo.Length / 1024.0); //kb
        }
    }
}
