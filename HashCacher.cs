using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActShrinker
{
    class HashCacher
    {
        const string LOG_FILE_NAME = "md5cache.txt";
        const char SPLIT_SYMBOL = '|';

        static Lazy<HashCacher> lazy = new Lazy<HashCacher>();

        public static HashCacher Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        /// <summary>
        /// Key：文件名，Value：hash
        /// </summary>
        Dictionary<string, string> hashes = new Dictionary<string, string>();

        public HashCacher()
        {
            if (File.Exists(LOG_FILE_NAME))
            {
                foreach (var line in File.ReadAllLines(LOG_FILE_NAME))
                {
                    var kv = line.Split(SPLIT_SYMBOL);

                    var key = kv[1];
                    var value = kv[0];

                    hashes.Add(key, value);
                }
            }
        }

        public void Archive()
        {
            var stringBuilder = new StringBuilder();

            foreach (var entry in hashes)
            {
                var file = entry.Key;
                var hash = entry.Value;

                stringBuilder.AppendLine(string.Format("{0}{1}{2}", hash, SPLIT_SYMBOL, file));
            }

            File.WriteAllText(LOG_FILE_NAME, stringBuilder.ToString());
        }

        public void Add(string file)
        {
            var hash = GetHash(file);

            if (hashes.ContainsKey(file))
            {
                hashes[file] = hash;
            }
            else
            {
                hashes.Add(file, hash);
            }
        }

        public bool IsDone(string file)
        {
            if (hashes.ContainsKey(file))
            {
                return hashes[file] == GetHash(file);
            }
            else
            {
                return false;
            }
        }

        string GetHash(string file)
        {
            return HashHelper.ComputeMD5(file);
        }
    }
}
