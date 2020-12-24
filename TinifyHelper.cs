using System;
using System.Collections.Generic;
using System.Threading;
using TinifyAPI;

namespace ActShrinker
{
    class TinifyHelper
    {
        public bool Done { get; private set; } 

        int count;
        int sucCount;
        List<string> targetImgs;

        readonly object SyncObject = new object();

        static readonly string[] API_KEYs = new string[]
        {
            "lEXWM4Wfavvw0B6DKBJb3MNFi9xH6B1l",//105264061
            "VSWQQ500y8C8FrRnM7rxfY7rszBqM0xd",//455212036
        };

        public static void SetKey(int index)
        {
            Tinify.Key = API_KEYs[index];
        }

        public static string GetKey()
        {
            return Tinify.Key;
        }

        public TinifyHelper(List<string> targetImgs)
        {
            this.targetImgs = targetImgs;
        }

        public void Run()
        {
            Console.WriteLine(string.Format("图片压缩开始，共 {0} 张图片（小于 {1} kb 的图片不做压缩处理）", targetImgs.Count, Config.FILE_SIZE_LIMIT));

            for (int i = 0; i < targetImgs.Count; i++)
            {
                var index = i;

                var targetImg = targetImgs[index];
                var targetImgCopy = targetImg.Replace(DirHelper.ORIGIN_ROOT_NAME, DirHelper.OUTPUT_ROOT_NAME);

                tinyImg(targetImgCopy, () => HashCacher.Instance.Add(targetImg));
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

            Done = true;
        }

        async void tinyImg(string path, Action sucCallback = null)
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

                    sucCallback?.Invoke();
                }
            }

            Console.WriteLine("已完成第 {0} 张，Path:{1}", count, path);
        }
    }
}
