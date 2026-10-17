using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecastNavBaker
{
    public static class Roundtrip
    {
        public static int Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("用法：RecastNavBaker --roundtrip <.FrameWork/Config/RecastOutput/地图名>");
                return 1;
            }

            string src = args[1];
            if (!File.Exists(src))
            {
                Console.Error.WriteLine($"[ERROR] 文件不存在：{src}");
                return 1;
            }

            byte[] original = File.ReadAllBytes(src);
            MeshFile.FileData fd = MeshFile.Read(src);

            string tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                MeshFile.Write(tmp, fd);
                byte[] rewritten = File.ReadAllBytes(tmp);

                Console.WriteLine($"[INFO] 原始 {original.Length} 字节 / 回写 {rewritten.Length} 字节");
                Console.WriteLine($"[INFO] params: orig=({fd.Params.orig.x}, {fd.Params.orig.y}, {fd.Params.orig.z}) tile=({fd.Params.tileWidth}, {fd.Params.tileHeight}) maxTiles={fd.Params.maxTiles} maxPolys={fd.Params.maxPolys}");
                Console.WriteLine($"[INFO] mesh: tiles={fd.Tiles.Count} polys={fd.PolyCount} verts={fd.VertCount}");

                if (original.Length != rewritten.Length)
                {
                    Console.Error.WriteLine($"[FAIL] 长度不一致：{original.Length} vs {rewritten.Length}");
                    return 1;
                }

                List<int> diffs = new();
                for (int i = 0; i < original.Length; i++)
                {
                    if (original[i] != rewritten[i] && diffs.Count < 20)
                    {
                        diffs.Add(i);
                    }
                }

                if (diffs.Count == 0)
                {
                    Console.WriteLine("[PASS] 逐字节一致");
                    return 0;
                }

                Console.Error.WriteLine($"[FAIL] 有 {diffs.Count}+ 处字节不同，前几处偏移：" + string.Join(", ", diffs));
                return 1;
            }
            finally
            {
                if (File.Exists(tmp))
                {
                    File.Delete(tmp);
                }
            }
        }
    }
}
