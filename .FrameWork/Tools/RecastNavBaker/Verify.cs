using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DotRecast.Core;
using DotRecast.Detour;

namespace RecastNavBaker
{
    public static class Verify
    {
        private const int MaxPolys = 256;

        public static int Run(string[] args)
        {
            string path = null;
            string map = null;
            string outputRoot = ".FrameWork/Config/RecastOutput";
            RcVec3f from = RcVec3f.Of(-10, 0, -10);
            RcVec3f to = RcVec3f.Of(10, 0, 10);

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--map":
                        map = args[++i];
                        break;
                    case "--output-root":
                        outputRoot = args[++i];
                        break;
                    case "--from":
                        from = ParseVec(args[++i]);
                        break;
                    case "--to":
                        to = ParseVec(args[++i]);
                        break;
                    default:
                        path = args[i];
                        break;
                }
            }

            if (path == null && map != null)
            {
                path = Path.Combine(outputRoot, map);
            }

            if (path == null || !File.Exists(path))
            {
                Console.Error.WriteLine("用法：RecastNavBaker --verify <.FrameWork/Config/RecastOutput/地图名> [--from x,y,z] [--to x,y,z]");
                Console.Error.WriteLine("  或：RecastNavBaker --verify --map <地图名> [--output-root .FrameWork/Config/RecastOutput]");
                return 1;
            }

            MeshFile.FileData fd = MeshFile.Read(path);
            DtNavMesh navMesh = new(fd.Params, MeshFile.MaxVertsPerPoly);
            foreach (MeshFile.TileEntry tile in fd.Tiles)
            {
                navMesh.AddTile(tile.Data, 0, 0);
            }
            DtNavMeshQuery query = new(navMesh);
            DtQueryDefaultFilter filter = new();
            RcVec3f extents = RcVec3f.Of(15, 10, 15);

            RcVec3f startPos = RcVec3f.Of(-from.x, from.y, from.z);
            RcVec3f endPos = RcVec3f.Of(-to.x, to.y, to.z);

            query.FindNearestPoly(startPos, extents, filter, out long startRef, out RcVec3f startPt, out _);
            query.FindNearestPoly(endPos, extents, filter, out long endRef, out RcVec3f endPt, out _);
            if (startRef == 0 || endRef == 0)
            {
                Console.Error.WriteLine($"[FAIL] 起终点附近没有导航多边形（startRef={startRef} endRef={endRef}）");
                return 1;
            }

            List<long> polys = new(MaxPolys);
            query.FindPath(startRef, endRef, startPt, endPt, filter, ref polys, new DtFindPathOption(0, float.MaxValue));
            if (polys.Count == 0)
            {
                Console.Error.WriteLine("[FAIL] 找不到路径（两点之间不连通）");
                return 1;
            }

            RcVec3f lastPos = endPt;
            if (polys[^1] != endRef)
            {
                DtStatus status = query.ClosestPointOnPoly(polys[^1], endPt, out RcVec3f closest, out bool _);
                if (status.Succeeded())
                {
                    lastPos = closest;
                }
            }

            List<StraightPathItem> straight = new();
            query.FindStraightPath(startPt, lastPos, polys, ref straight, MaxPolys, DtNavMeshQuery.DT_STRAIGHTPATH_ALL_CROSSINGS);

            Console.WriteLine($"[INFO] 文件：{path}（{fd.Tiles.Count} 个 tile）");
            Console.WriteLine($"[INFO] 多边形 {fd.PolyCount} 个 / 顶点 {fd.VertCount} 个");
            Console.WriteLine($"[PASS] 路径点 {straight.Count} 个（polys={polys.Count}）：");
            for (int i = 0; i < straight.Count; i++)
            {
                RcVec3f pos = straight[i].pos;
                Console.WriteLine($"  [{i}] 游戏坐标 ({-pos.x:0.###}, {pos.y:0.###}, {pos.z:0.###})");
            }

            RcVec3f last = straight[^1].pos;
            float dx = last.x - endPt.x;
            float dy = last.y - endPt.y;
            float dz = last.z - endPt.z;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

            float tx = -endPt.x - to.x;
            float tz = endPt.z - to.z;
            float targetOffset = (float)Math.Sqrt(tx * tx + tz * tz);
            if (targetOffset > 1.0f)
            {
                Console.WriteLine($"[INFO] 目标点离导航网格 {targetOffset:0.###}（目标附近没有可行走多边形，已吸附到最近的网格点）");
            }

            if (distance > 1.0f)
            {
                Console.WriteLine($"[WARN] 只找到部分路径，终点离网格目标 {distance:0.###}（可能不连通/被障碍挡住）");
                return 2;
            }

            Console.WriteLine($"[PASS] 已到达目标（误差 {distance:0.###}）");
            return 0;
        }

        private static RcVec3f ParseVec(string text)
        {
            string[] parts = text.Split(',');
            if (parts.Length != 3)
            {
                throw new ArgumentException($"坐标格式应为 x,y,z：{text}");
            }

            return RcVec3f.Of(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }
}
