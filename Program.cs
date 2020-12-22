using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        const string OUTPUT_ROOT_NAME = @"ActShrinker\ActRes";
        const string TOOL_7ZA_ROOT_NAME = @"ActShrinker\tools\7z_win\7za.exe";
        const string DIR_LOBBY_LOGIN = @"\lobby\login";
        const int limit = 100;//筛选大于100kb的图片进行压缩。更优化一步，这里的筛选应该放在对图片进行PO2处理后，因为PO2处理会使图片变大。

        static readonly object SyncObject = new object();

        #region Dir
        static string rootDir
        {
            get
            {
                return IOHelper.GetDirectoryPath(curDir);
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

        static string tool7zaPath
        {
            get
            {
                return Path.Combine(rootDir, TOOL_7ZA_ROOT_NAME);
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
                Console.WriteLine(string.Format("编译参数：{0}", "全编译"));

                targetFiles = new List<string>(IOHelper.GetAllFiles(inputDir, SearchOption.AllDirectories));
                selectedDirectories.Add(outputDir);
            }
            else
            {
                para = string.Format(@"lobby\weaponstorage@lobby\login\{0}@newActivityRoot\{0}", para);

                Console.WriteLine(string.Format("编译参数：{0}", para));

                var targetPaths = para.Split('@');

                targetFiles = new List<string>();
                for (int i = 0; i < targetPaths.Length; i++)
                {
                    var targetPath = targetPaths[i];
                    targetFiles.AddRange(new List<string>(IOHelper.GetAllFiles(Path.Combine(inputDir, targetPath), SearchOption.AllDirectories)));
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

            var targetImgs = imgs.FindAll(img => IOHelper.GetFileSize(img) >= limit);
            #endregion

            #region Copy
            {
                Console.WriteLine(string.Format("文件拷贝开始，共 {0} 份文件", targetFiles.Count));

                for (int i = 0; i < selectedDirectories.Count; i++)
                {
                    var selectedDirectory = selectedDirectories[i];
                    IOHelper.DeleteDirectory(selectedDirectory);
                }

                for (int i = 0; i < selectedDirectories.Count; i++)
                {
                    var selectedDirectory = selectedDirectories[i];
                    IOHelper.CreateDirectory(selectedDirectory);
                }

                for (int i = 0; i < targetFiles.Count; i++)
                {
                    var file = targetFiles[i];
                    var copy = file.Replace(ORIGIN_ROOT_NAME, OUTPUT_ROOT_NAME);

                    var msg = "";

                    if (imgs.Contains(file) && !file.Contains(DIR_LOBBY_LOGIN))
                    {
                        var img = Image.FromFile(file);

                        var width = img.Width;
                        var height = img.Height;

                        var bitmap = new Bitmap(img, IOHelper.GetNearestPO2(width), IOHelper.GetNearestPO2(height));

                        IOHelper.CreateDirectory(IOHelper.GetDirectoryPath(copy));
                        bitmap.Save(copy);

                        msg = string.Format("已完成并调整第 {0} 份，Path:{1}", i + 1, file);
                    }
                    else
                    {
                        IOHelper.CopyFile(file, copy);

                        msg = string.Format("已完成第 {0} 份，Path:{1}", i + 1, file);
                    }

                    Console.WriteLine(msg);
                }
            }

            Console.WriteLine("文件拷贝完毕");
            #endregion

            #region RunTinyPng
            {
                Console.WriteLine(string.Format("图片压缩开始，共 {0} 张图片（小于 {1} kb 的图片不做压缩处理）", targetImgs.Count, limit));

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

                Console.WriteLine("图片压缩完毕，失败：{0} 张", count - sucCount);
            }
            #endregion

            #region Archive
            Archive();
            #endregion
        }

        #region Tinify
        static int count;
        static int sucCount;

        async static void tinyImg(string path)
        {
            var source = Tinify.FromFile(path);
            var suc = true;

            try
            {
                await source.ToFile(path);
            }
            catch (TinifyAPI.Exception exception)
            {
                Console.WriteLine("第 {0} 张 异常，Path:{1}, Exception:{2}", count, path, exception);
                suc = false;
            }

            lock (SyncObject)
            {
                count++;

                if (suc)
                {
                    sucCount++;
                }
            }

            Console.WriteLine("已完成第 {0} 张，Path:{1}", count, path);
        }
        #endregion

        #region 7za
        const string PREFIX = "ActRes_";

        static void Archive()
        {
            Clear();

            Zip();
        }

        static void Clear()
        {
            var oldArchives = new List<string>(IOHelper.GetAllFiles(curDir, SearchOption.TopDirectoryOnly, @".+\.zip"));

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
            process.StartInfo.Arguments = string.Format("a {0}.zip ActRes/*", archive);
            process.Start();
            process.WaitForExit();
        }
        #endregion
    }
}
