using System;

namespace RecastNavBaker
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Usage();
                    return 1;
                }

                switch (args[0])
                {
                    case "--bake":
                        return Bake.Run(args);
                    case "--verify":
                        return Verify.Run(args);
                    case "--roundtrip":
                        return Roundtrip.Run(args);
                    default:
                        Usage();
                        return 1;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[ERROR] " + e.Message);
                Console.Error.WriteLine(e.StackTrace);
                return 1;
            }
        }

        private static void Usage()
        {
            Console.WriteLine("RecastNavBaker —— 无头导航网格烘焙/校验工具（DotRecast）");
            Console.WriteLine();
            Console.WriteLine("约定式目录（推荐）：");
            Console.WriteLine("  <input-root>/<地图名>/            一张图的全部输入（<地图名>.obj + 可选 blockers.txt / areas.txt）");
            Console.WriteLine("                                    input-root 默认 = 系统临时目录/RecastInput（中间产物不落盘）");
            Console.WriteLine("  .FrameWork/Config/RecastOutput/<地图名>   烘焙产物（服务端按 ../Config/RecastOutput/<地图名> 读取）");
            Console.WriteLine();
            Console.WriteLine("用法：");
            Console.WriteLine("  烘焙：RecastNavBaker --bake --map <地图名> [--input-root <临时输入目录>] [--output-root .FrameWork/Config/RecastOutput]");
            Console.WriteLine("        [--tile-size 0]  # >0（世界单位）时按 tile 烘焙（Tiled 多 tile），默认 0 = Solo 单 tile");
            Console.WriteLine("        [--partition watershed|monotone|layers] [--cs 0.3] [--ch 0.2] [--slope 45] [--height 2.0]");
            Console.WriteLine("        [--radius 0.6] [--climb 0.9] [--region-min 8] [--region-merge 20] [--edge-len 12]");
            Console.WriteLine("        [--edge-error 1.3] [--verts-per-poly 6] [--detail-dist 6] [--detail-error 1]");
            Console.WriteLine("  显式输入：RecastNavBaker --bake --walkable <map.obj> --out <输出文件> [--blockers <txt>] [--areas <txt>]");
            Console.WriteLine("  校验：RecastNavBaker --verify --map <地图名> [--from x,y,z] [--to x,y,z]");
            Console.WriteLine("        RecastNavBaker --verify <.FrameWork/Config/RecastOutput/地图名>");
            Console.WriteLine("  自检：RecastNavBaker --roundtrip <.FrameWork/Config/RecastOutput/地图名>");
            Console.WriteLine();
            Console.WriteLine("<input-root>/<地图名>/blockers.txt（空气墙/障碍，栅格化成不可走实心体）：");
            Console.WriteLine("  box <cx cy cz> <sx sy sz>          中心 + 全尺寸");
            Console.WriteLine("  cyl <cx cy cz> <radius> <height>   底面中心 + 半径 + 高度");
            Console.WriteLine();
            Console.WriteLine("<input-root>/<地图名>/areas.txt（区域类型，行尾多一个 areaId）：");
            Console.WriteLine("  box <cx cy cz> <sx sy sz> <areaId>");
            Console.WriteLine("  cyl <cx cy cz> <radius> <height> <areaId>");
        }
    }
}
