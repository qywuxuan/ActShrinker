using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActShrinker
{
    static class Config
    {
        public const int FILE_SIZE_LIMIT = 100;//筛选大于100kb的图片进行压缩。更优化一步，这里的筛选应该放在对图片进行PO2处理后，因为PO2处理会使图片变大。
    }
}
