using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TinifyAPI;

namespace ActShrinker
{
    class Program
    {
        static readonly string[] API_KEYs = new string[]
        {
            "lEXWM4Wfavvw0B6DKBJb3MNFi9xH6B1l",//105264061
            "VSWQQ500y8C8FrRnM7rxfY7rszBqM0xd",//455212036
        };

        const string ORIGIN_ROOT_NAME = "活动动态资源";
        const string OUTPUT_ROOT_NAME = "ActShrinker\\ActRes";

        static readonly object SyncObject = new object();

        #region Dir
        static string rootDir
        {
            get
            {
                return GetDirectoryPath(curDir);
            }
        }

        static string curDir
        {
            get
            {
                return Environment.CurrentDirectory;
            }
        }

        static string inputDir
        {
            get
            {
                return Path.Combine(rootDir, ORIGIN_ROOT_NAME);
            }
        }

        static string outputDir
        {
            get
            {
                return Path.Combine(rootDir, OUTPUT_ROOT_NAME);
            }
        }
        #endregion

        static void Main(string[] args)
        {
            if (args.Length == 0)
                return;

            #region 输入处理
            var para = args[0];

            List<string> targetFiles = null;
            var selectedDirectories = new List<string>();

            if (para.Equals("-a"))
            {
                targetFiles = new List<string>(GetAllFiles(inputDir, SearchOption.AllDirectories));
                selectedDirectories.Add(outputDir);
            }
            else
            {
                para = string.Format(@"lobby\login\{0}@newActivityRoot\{0}", para);

                Console.WriteLine(string.Format("编译参数：{0}", para));

                var targetPaths = para.Split('@');

                targetFiles = new List<string>();
                for (int i = 0; i < targetPaths.Length; i++)
                {
                    var targetPath = targetPaths[i];
                    targetFiles.AddRange(new List<string>(GetAllFiles(Path.Combine(inputDir, targetPath), SearchOption.AllDirectories)));
                    selectedDirectories.Add(Path.Combine(outputDir, targetPath));
                }
            }

            if (targetFiles != null)
            {
                //do nothing
            }
            else
            {
                return;//参数异常
            }

            targetFiles.RemoveAll(file => file.Contains("\\.svn\\"));

            if (args.Length > 1)
            {
                var keyIndex = int.Parse(args[1]);
                Tinify.Key = API_KEYs[keyIndex];
            }
            else
            {
                Tinify.Key = API_KEYs[0];
            }

            Console.WriteLine(string.Format("资源编译开始，Tinify.Key:{0}", Tinify.Key));
            #endregion

            #region Select

            var imgs = targetFiles.FindAll(file =>
            {
                var extension = new string[] { "png", "jpg" };

                for (int i = 0; i < extension.Length; i++)
                {
                    if (file.EndsWith(extension[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            });

            var targetImgs = imgs.FindAll(img => GetFileSize(img) >= 100);
            #endregion

            #region Copy
            {
                Console.WriteLine(string.Format("文件拷贝开始，共 {0} 份文件", targetFiles.Count));

                for (int i = 0; i < selectedDirectories.Count; i++)
                {
                    var selectedDirectory = selectedDirectories[i];
                    DeleteDirectory(selectedDirectory);
                }

                for (int i = 0; i < selectedDirectories.Count; i++)
                {
                    var selectedDirectory = selectedDirectories[i];
                    CreateDirectory(selectedDirectory);
                }

                for (int i = 0; i < targetFiles.Count; i++)
                {
                    var file = targetFiles[i];
                    var copy = file.Replace(ORIGIN_ROOT_NAME, OUTPUT_ROOT_NAME);

                    if (imgs.Contains(file))
                    {
                        var img = Image.FromFile(file);

                        var width = img.Width;
                        var height = img.Height;

                        var bitmap = new Bitmap(img, GetNearestPO2(width), GetNearestPO2(height));

                        CreateDirectory(GetDirectoryPath(copy));
                        bitmap.Save(copy);
                    }
                    else
                    {
                        CopyFile(file, copy);
                    }
                }
            }

            Console.WriteLine("文件拷贝完毕");
            #endregion

            #region RunTinyPng
            {
                Console.WriteLine(string.Format("图片压缩开始，共 {0} 张图片", targetImgs.Count));

                for (int i = 0; i < targetImgs.Count; i++)
                {
                    var index = i;

                    var targetImg = targetImgs[index];
                    var targetImgCopy = targetImg.Replace(ORIGIN_ROOT_NAME, OUTPUT_ROOT_NAME);

                    tinyImg(targetImgCopy);
                }

                while (true)
                {
                    Thread.Sleep(1000);
                    if (count == targetImgs.Count)
                    {
                        break;
                    }
                }

                Console.WriteLine("图片压缩完毕");
            }
            #endregion
        }

        #region Tinify
        static int count;

        async static void tinyImg(string path)
        {
            var source = Tinify.FromFile(path);
            await source.ToFile(path);

            lock (SyncObject)
            {
                count++;
            }

            Console.WriteLine("已完成第 {0} 张，Path:{1}", count, path);
        }
        #endregion

        #region IO
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
            return Directory.CreateDirectory(directoryPath);
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
        #endregion
    }
}
