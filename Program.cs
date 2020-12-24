using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ActShrinker
{
    class Program
    {
        const string DIR_LOBBY_LOGIN = @"\lobby\login";
        static readonly string[] EXTENSION = new string[] { "png", "jpg" };

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

                targetFiles = new List<string>(IOHelper.GetAllFiles(DirHelper.inputDir, SearchOption.AllDirectories));
                selectedDirectories.Add(DirHelper.outputDir);
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
                    targetFiles.AddRange(new List<string>(IOHelper.GetAllFiles(Path.Combine(DirHelper.inputDir, targetPath), SearchOption.AllDirectories)));
                    selectedDirectories.Add(Path.Combine(DirHelper.outputDir, targetPath));
                }
            }

            if (targetFiles != null && targetFiles.Count > 0)
            {
                //do nothing
            }
            else
            {
                Console.WriteLine("指定目录下没有需要编译的资源，编译结束");
                return;
            }

            targetFiles.RemoveAll(file => file.Contains("\\.svn\\"));

            if (args.Length > 1)
            {
                var keyIndex = int.Parse(args[1]);
                TinifyHelper.SetKey(keyIndex);
            }
            else
            {
                TinifyHelper.SetKey(0);
            }

            Console.WriteLine(string.Format("资源编译开始，Tinify.Key:{0}", TinifyHelper.GetKey()));
            #endregion

            #region Select

            var imgs = targetFiles.FindAll(file =>
            {
                for (int i = 0; i < EXTENSION.Length; i++)
                {
                    if (file.EndsWith(EXTENSION[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            });

            var targetImgs = imgs.FindAll(img => IOHelper.GetFileSize(img) >= Config.FILE_SIZE_LIMIT);
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
                    var copy = file.Replace(DirHelper.ORIGIN_ROOT_NAME, DirHelper.OUTPUT_ROOT_NAME);

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
                var TinifyHelper = new TinifyHelper(targetImgs);
                TinifyHelper.Run();
            }
            #endregion

            #region Archive
            _7zaHelper.Archive();
            #endregion
        }
    }
}
