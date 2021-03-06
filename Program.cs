﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TinifyAPI;

namespace ActShrinker
{
    class Program
    {
        const string API_KEY = "lEXWM4Wfavvw0B6DKBJb3MNFi9xH6B1l";
        const string ORIGIN_ROOT_NAME = "活动动态资源";
        const string OUTPUT_ROOT_NAME = "ActShrinker/ActRes";

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
            #region 输入处理
            Console.WriteLine("使用说明：\n输入-a后回车，遍历所有活动文件。\n输入-t + 指定文件夹后回车，遍历指定活动文件夹，例如：-t version/v3|version/v4\n");
            var rawPara = Console.ReadLine();

            List<string> targetFiles = null;
            var selectedDirectories = new List<string>();

            var paras = rawPara.Split(' ');
            if (paras[0].Equals("-a"))
            {
                targetFiles = new List<string>(GetAllFiles(inputDir, SearchOption.AllDirectories));
                selectedDirectories.Add(outputDir);
            }
            else if (paras[0].Equals("-t"))
            {
                if (paras.Length < 2)
                {
                    //input wrong
                }
                else
                {
                    var targetPaths = paras[1].Split('|');
                    targetFiles = new List<string>();
                    for (int i = 0; i < targetPaths.Length; i++)
                    {
                        var targetPath = targetPaths[i];
                        targetFiles.AddRange(new List<string>(GetAllFiles(Path.Combine(inputDir, targetPath), SearchOption.AllDirectories)));
                        selectedDirectories.Add(Path.Combine(outputDir, targetPath));
                    }
                }
            }

            if (targetFiles != null)
            {
                //do nothing
            }
            else
            {
                Console.WriteLine("参数异常，按任意键退出");
                Console.ReadKey();
                return;
            }
            #endregion

            var startTime = DateTime.Now;

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
                    Console.Clear();
                    Console.WriteLine("文件拷贝中，当前第 {0} 份，共 {1} 份", i + 1, targetFiles.Count);

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
            #endregion

            #region RunTinyPng
            {
                Tinify.Key = API_KEY;

                var count = 0;

                for (int i = 0; i < targetImgs.Count; i++)
                {
                    var index = i;

                    Task.Factory.StartNew(async () =>
                    {
                        var targetImg = targetImgs[index];
                        var targetImgCopy = targetImg.Replace(ORIGIN_ROOT_NAME, OUTPUT_ROOT_NAME);

                        var source = Tinify.FromFile(targetImgCopy);
                        await source.ToFile(targetImgCopy).ContinueWith(_ =>
                        {
                            lock (SyncObject)
                            {
                                count++;
                            }
                        });
                    }, TaskCreationOptions.PreferFairness);
                }

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("压缩中，当前第 {0} 张，共 {1} 张", count + 1, targetImgs.Count);
                    Thread.Sleep(100);
                    if (count == targetImgs.Count)
                    {
                        break;
                    }
                }

                Console.Clear();
                var ts = (DateTime.Now - startTime);
                Console.WriteLine(string.Format("压缩完毕，等待打包。压缩耗时：{0}分 {1}秒。", ts.Minutes, ts.Seconds));
            }
            #endregion

            RunZipBatch();
        }

        static void RunZipBatch()
        {
            Process proc = null;
            try
            {
                proc = new Process();
                proc.StartInfo.WorkingDirectory = curDir;
                proc.StartInfo.FileName = "win_build.bat";
                //proc.StartInfo.Arguments = string.Format("10");
                proc.StartInfo.CreateNoWindow = false;
                proc.Start();
                proc.WaitForExit();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }
        }

        #region IO
        public static string[] GetAllFiles(string path, SearchOption searchOption = SearchOption.AllDirectories, string fileNameRegex = null)
        {
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
