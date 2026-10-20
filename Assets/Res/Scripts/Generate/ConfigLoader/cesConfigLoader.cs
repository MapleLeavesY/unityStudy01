using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Core.Excel
{
    // 自动生成信息，请勿手动修改
    // 日期: 2026/10/20 10:11:39

    public static class cesConfigLoader
    {
        private static readonly string bytesDir = Application.streamingAssetsPath + "/Configs/ces.bytes";
        private static Dictionary<int, cesConfig> cache;

        public static Dictionary<int, cesConfig> LoadAll()
        {
            if (cache != null) return cache;

            cache = new Dictionary<int, cesConfig>();
            var bytes = File.ReadAllBytes(bytesDir);

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                var count = reader.ReadInt32();

                for (int cur = 0; cur < count; cur++)
                {
                    var serialize = new cesConfigSerializable();
                    var config = serialize.Reader(reader);
                    if (config == null) continue;

                    var key = config.GetKey();
                    cache[key] = config;
                }

                return cache;
            }
        }

        public static List<cesConfig> GetAll()
        {
            if (cache == null) LoadAll();
            return cache.Values.ToList();
        }

        public static cesConfig Get(int key)
        {
            if (cache == null) LoadAll();
            bool canFind = cache.TryGetValue(key, out var value);
            return canFind ? value : null;
        }
    }
}
