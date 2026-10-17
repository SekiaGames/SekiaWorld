using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;

namespace RecastNavBaker
{
    public static class Bake
    {
        private sealed class Options
        {
            public string Map;
            public string InputRoot = Path.Combine(Path.GetTempPath(), "RecastInput");
            public string OutputRoot = ".FrameWork/Config/RecastOutput";
            public string Walkable;
            public string Out;
            public string Blockers;
            public string Areas;
            public string Partition = "watershed";
            public float TileSize;

            public float Cs = 0.3f;
            public float Ch = 0.2f;
            public float Slope = 45f;
            public float Height = 2.0f;
            public float Radius = 0.6f;
            public float Climb = 0.9f;
            public int RegionMin = 8;
            public int RegionMerge = 20;
            public float EdgeLen = 12f;
            public float EdgeError = 1.3f;
            public int VertsPerPoly = 6;
            public float DetailDist = 6f;
            public float DetailError = 1f;
        }

        public static int Run(string[] args)
        {
            Options o = ParseOptions(args);
            ResolveMapLayout(o);

            if (o.Walkable == null || o.Out == null)
            {
                Console.Error.WriteLine("用法：RecastNavBaker --bake --map <地图名> [--input-root <临时输入目录>] [--output-root .FrameWork/Config/RecastOutput]");
                Console.Error.WriteLine("  或：RecastNavBaker --bake --walkable <map.obj> --out <输出文件> [--blockers <txt>] [--areas <txt>] [参数...]");
                return 1;
            }

            if (!File.Exists(o.Walkable))
            {
                Console.Error.WriteLine($"[ERROR] walkable OBJ 不存在：{o.Walkable}");
                return 1;
            }

            if (o.VertsPerPoly != MeshFile.MaxVertsPerPoly)
            {
                Console.Error.WriteLine($"[ERROR] verts-per-poly 必须为 {MeshFile.MaxVertsPerPoly}（服务端按该值读取导航数据）");
                return 1;
            }

            BakeNow(o);
            return 0;
        }

        private static void ResolveMapLayout(Options o)
        {
            if (o.Map == null || o.Walkable != null || o.Out != null)
            {
                return;
            }

            string inputDir = Path.Combine(o.InputRoot, o.Map);
            if (!Directory.Exists(inputDir))
            {
                throw new DirectoryNotFoundException($"输入目录不存在：{inputDir}（约定：<input-root>/<地图名>/ 放该地图的 obj 与 blockers/areas；默认 input-root 是系统临时目录，不落盘）");
            }

            string preferred = Path.Combine(inputDir, o.Map + ".obj");
            string[] objs = Directory.GetFiles(inputDir, "*.obj");
            if (File.Exists(preferred))
            {
                o.Walkable = preferred;
            }
            else if (objs.Length == 1)
            {
                o.Walkable = objs[0];
            }
            else if (objs.Length == 0)
            {
                throw new FileNotFoundException($"输入目录里没有 .obj：{inputDir}");
            }
            else
            {
                throw new FileNotFoundException($"输入目录里有多个 .obj，请命名为 {o.Map}.obj：{inputDir}");
            }

            if (o.Blockers == null)
            {
                string blockers = Path.Combine(inputDir, "blockers.txt");
                if (File.Exists(blockers))
                {
                    o.Blockers = blockers;
                }
            }

            if (o.Areas == null)
            {
                string areas = Path.Combine(inputDir, "areas.txt");
                if (File.Exists(areas))
                {
                    o.Areas = areas;
                }
            }

            o.Out = Path.Combine(o.OutputRoot, o.Map);
        }

        private static void BakeNow(Options o)
        {
            SimpleInputGeomProvider simpleGeom = ObjGeometry.Load(o.Walkable);
            IInputGeomProvider geom = simpleGeom;
            Console.WriteLine($"[INFO] 输入几何：{o.Walkable}（顶点 {simpleGeom.vertices.Length / 3}，三角面 {simpleGeom.faces.Length / 3}）");

            RcConfig cfg = CreateConfig(o);
            RcTelemetry telemetry = new();
            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();
            Console.WriteLine($"[INFO] 范围：bmin=({bmin.x}, {bmin.y}, {bmin.z}) bmax=({bmax.x}, {bmax.y}, {bmax.z})（cs={cfg.Cs} ch={cfg.Ch}）");

            if (o.Areas != null)
            {
                ApplyAreas(simpleGeom, o.Areas);
            }

            DtNavMesh navMesh = o.TileSize > 0
                ? BakeTiled(o, geom, cfg, bmin, bmax, telemetry)
                : BakeSolo(o, geom, cfg, bmin, bmax, telemetry);

            MeshFile.FileData fd = new()
            {
                Params = navMesh.GetParams(),
            };

            for (int i = 0; i < navMesh.GetMaxTiles(); i++)
            {
                DtMeshTile tile = navMesh.GetTile(i);
                if (tile?.data == null)
                {
                    continue;
                }

                fd.Tiles.Add(new MeshFile.TileEntry
                {
                    Data = tile.data,
                    TileRef = navMesh.GetTileRef(tile),
                });
            }

            string dir = Path.GetDirectoryName(Path.GetFullPath(o.Out));
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            MeshFile.Write(o.Out, fd);

            Console.WriteLine($"[INFO] navmesh：tiles={fd.Tiles.Count} polys={fd.PolyCount} verts={fd.VertCount} params(maxTiles={fd.Params.maxTiles} maxPolys={fd.Params.maxPolys} tile={fd.Params.tileWidth}x{fd.Params.tileHeight} orig=({fd.Params.orig.x}, {fd.Params.orig.y}, {fd.Params.orig.z}))");
            Console.WriteLine($"[PASS] 已写出 {o.Out}（{new FileInfo(o.Out).Length} 字节）");
        }

        private static RcConfig CreateConfig(Options o)
        {
            RcPartition partition = PartitionOf(o.Partition);
            RcAreaModification walkableArea = new(1);
            if (o.TileSize <= 0)
            {
                return new RcConfig(
                    partition,
                    o.Cs, o.Ch,
                    o.Slope, o.Height, o.Radius, o.Climb,
                    o.RegionMin, o.RegionMerge,
                    o.EdgeLen, o.EdgeError,
                    o.VertsPerPoly,
                    o.DetailDist, o.DetailError,
                    true, true, true,
                    walkableArea, true);
            }

            int tileVoxels = TileVoxels(o);
            int borderSize = (int)Math.Ceiling(o.Radius / o.Cs) + 3;
            float minRegionAreaWorld = o.RegionMin * o.RegionMin * o.Cs * o.Cs;
            float mergeRegionAreaWorld = o.RegionMerge * o.RegionMerge * o.Cs * o.Cs;
            return new RcConfig(
                true, tileVoxels, tileVoxels,
                borderSize,
                partition,
                o.Cs, o.Ch,
                o.Slope, o.Height, o.Radius, o.Climb,
                minRegionAreaWorld, mergeRegionAreaWorld,
                o.EdgeLen, o.EdgeError,
                o.VertsPerPoly,
                o.DetailDist, o.DetailError,
                true, true, true,
                walkableArea, true);
        }

        private static int TileVoxels(Options o)
        {
            return Math.Max(1, (int)MathF.Round(o.TileSize / o.Cs));
        }

        private static DtNavMesh BakeSolo(Options o, IInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax, RcTelemetry telemetry)
        {
            RecastBuilderConfig builderCfg = new(cfg, bmin, bmax);
            Console.WriteLine($"[INFO] 网格：{builderCfg.width}x{builderCfg.height}（Solo 单 tile）");

            RcHeightfield solid = RecastVoxelization.BuildSolidHeightfield(geom, builderCfg, telemetry);
            if (o.Blockers != null)
            {
                ApplyBlockers(solid, o.Blockers, telemetry, cfg.WalkableClimb);
            }

            RecastBuilder builder = new();
            RecastBuilderResult result = builder.Build(0, 0, geom, cfg, solid, telemetry);
            RcPolyMesh pmesh = result.GetMesh();
            if (pmesh == null || pmesh.npolys == 0)
            {
                throw new InvalidOperationException("烘焙结果没有可行走多边形（检查输入几何是否封闭/是否所有面都不可走）");
            }

            Console.WriteLine($"[INFO] 多边形网格：顶点 {pmesh.nverts} / 多边形 {pmesh.npolys} / nvp {pmesh.nvp}");
            DtMeshData data = CreateDetourData(o, pmesh, result.GetMeshDetail(), cfg, 0, 0);
            return new DtNavMesh(data, MeshFile.MaxVertsPerPoly, 0);
        }

        private static DtNavMesh BakeTiled(Options o, IInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax, RcTelemetry telemetry)
        {
            int tileVoxels = TileVoxels(o);
            RcUtils.CalcTileCount(bmin, bmax, cfg.Cs, tileVoxels, tileVoxels, out int tw, out int th);
            Console.WriteLine($"[INFO] 分块：{tw}x{th} = {tw * th} 块，tile={o.TileSize}（{tileVoxels} 体素 = {tileVoxels * cfg.Cs:0.###} 世界单位）");

            RecastBuilder builder = new();
            List<DtMeshData> datas = new();
            int maxPolys = 0;
            int emptyTiles = 0;
            int totalPolys = 0;
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    RecastBuilderConfig builderCfg = new(cfg, bmin, bmax, x, y);
                    RcHeightfield solid = RecastVoxelization.BuildSolidHeightfield(geom, builderCfg, telemetry);
                    if (o.Blockers != null)
                    {
                        ApplyBlockers(solid, o.Blockers, telemetry, cfg.WalkableClimb);
                    }

                    RecastBuilderResult result = builder.Build(x, y, geom, cfg, solid, telemetry);
                    RcPolyMesh pmesh = result.GetMesh();
                    if (pmesh == null || pmesh.npolys == 0)
                    {
                        emptyTiles++;
                        continue;
                    }

                    datas.Add(CreateDetourData(o, pmesh, result.GetMeshDetail(), cfg, x, y));
                    maxPolys = Math.Max(maxPolys, pmesh.npolys);
                    totalPolys += pmesh.npolys;
                }
            }

            if (datas.Count == 0)
            {
                throw new InvalidOperationException("烘焙结果没有可行走多边形（检查输入几何是否封闭/是否所有面都不可走）");
            }

            DtNavMeshParams p = new()
            {
                orig = RcVec3f.Of(bmin.x, bmin.y, bmin.z),
                tileWidth = tileVoxels * cfg.Cs,
                tileHeight = tileVoxels * cfg.Cs,
                maxTiles = Math.Max(1, tw * th),
                maxPolys = Math.Max(1, maxPolys),
            };

            DtNavMesh navMesh = new(p, MeshFile.MaxVertsPerPoly);
            foreach (DtMeshData data in datas)
            {
                navMesh.AddTile(data, 0, 0);
            }

            Console.WriteLine($"[INFO] 有效 tile {datas.Count} 个（空 {emptyTiles} 个），多边形合计 {totalPolys}，单块最多 {maxPolys}");
            return navMesh;
        }

        private static DtMeshData CreateDetourData(Options o, RcPolyMesh pmesh, RcPolyMeshDetail dmesh, RcConfig cfg, int tileX, int tileZ)
        {
            int[] polyAreas = new int[pmesh.npolys];
            int[] polyFlags = new int[pmesh.npolys];
            for (int i = 0; i < pmesh.npolys; i++)
            {
                polyAreas[i] = 0;
                polyFlags[i] = 1;
            }

            DtNavMeshCreateParams cp = new()
            {
                verts = pmesh.verts,
                vertCount = pmesh.nverts,
                polys = pmesh.polys,
                polyAreas = polyAreas,
                polyFlags = polyFlags,
                polyCount = pmesh.npolys,
                nvp = pmesh.nvp,
                detailMeshes = dmesh?.meshes,
                detailVerts = dmesh?.verts,
                detailVertsCount = dmesh?.nverts ?? 0,
                detailTris = dmesh?.tris,
                detailTriCount = dmesh?.ntris ?? 0,
                walkableHeight = o.Height,
                walkableRadius = o.Radius,
                walkableClimb = o.Climb,
                bmin = pmesh.bmin,
                bmax = pmesh.bmax,
                cs = cfg.Cs,
                ch = cfg.Ch,
                buildBvTree = true,
                tileX = tileX,
                tileZ = tileZ,
                tileLayer = 0,
            };

            DtMeshData data = DtNavMeshBuilder.CreateNavMeshData(cp);
            if (data == null)
            {
                throw new InvalidOperationException("CreateNavMeshData 失败");
            }

            return data;
        }

        private static RcPartition PartitionOf(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "watershed" => RcPartition.WATERSHED,
                "monotone" => RcPartition.MONOTONE,
                "layers" => RcPartition.LAYERS,
                _ => throw new ArgumentException($"未知分区方式：{name}（watershed/monotone/layers）"),
            };
        }

        private static void ApplyBlockers(RcHeightfield solid, string file, RcTelemetry ctx, int flagMergeThr)
        {
            int count = 0;
            foreach ((string kind, float[] v) in ReadShapes(file))
            {
                if (kind == "box")
                {
                    RcVec3f center = RcVec3f.Of(v[0], v[1], v[2]);
                    RcVec3f[] halfEdges =
                    {
                        RcVec3f.Of(v[3] * 0.5f, 0, 0),
                        RcVec3f.Of(0, v[4] * 0.5f, 0),
                        RcVec3f.Of(0, 0, v[5] * 0.5f),
                    };
                    RecastFilledVolumeRasterization.RasterizeBox(solid, center, halfEdges, RcConstants.RC_NULL_AREA, flagMergeThr, ctx);
                }
                else
                {
                    RcVec3f start = RcVec3f.Of(v[0], v[1], v[2]);
                    RcVec3f end = RcVec3f.Of(v[0], v[1] + v[4], v[2]);
                    RecastFilledVolumeRasterization.RasterizeCylinder(solid, start, end, v[3], RcConstants.RC_NULL_AREA, flagMergeThr, ctx);
                }

                count++;
            }

            Console.WriteLine($"[INFO] 障碍体：{count} 个（{file}）");
        }

        private static void ApplyAreas(SimpleInputGeomProvider geom, string file)
        {
            int count = 0;
            foreach ((string kind, float[] v) in ReadShapes(file))
            {
                float[] footprint;
                float hmin;
                float hmax;
                int areaId;

                if (kind == "box")
                {
                    float halfX = v[3] * 0.5f;
                    float halfZ = v[5] * 0.5f;
                    footprint = new[]
                    {
                        v[0] - halfX, 0, v[2] - halfZ,
                        v[0] + halfX, 0, v[2] - halfZ,
                        v[0] + halfX, 0, v[2] + halfZ,
                        v[0] - halfX, 0, v[2] + halfZ,
                    };
                    hmin = v[1] - v[4] * 0.5f;
                    hmax = v[1] + v[4] * 0.5f;
                    areaId = (int)v[6];
                }
                else
                {
                    const int sides = 16;
                    footprint = new float[sides * 3];
                    for (int i = 0; i < sides; i++)
                    {
                        float angle = (float)(i * 2.0 * Math.PI / sides);
                        footprint[i * 3 + 0] = v[0] + (float)Math.Cos(angle) * v[3];
                        footprint[i * 3 + 1] = 0;
                        footprint[i * 3 + 2] = v[2] + (float)Math.Sin(angle) * v[3];
                    }

                    hmin = v[1];
                    hmax = v[1] + v[4];
                    areaId = (int)v[5];
                }

                geom.AddConvexVolume(new RcConvexVolume
                {
                    verts = footprint,
                    hmin = hmin,
                    hmax = hmax,
                    areaMod = new RcAreaModification(areaId),
                });
                count++;
            }

            Console.WriteLine($"[INFO] 区域标记：{count} 个（{file}）");
        }

        private static IEnumerable<(string kind, float[] values)> ReadShapes(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"形状文件不存在：{file}");
            }

            foreach (string raw in File.ReadLines(file))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts[0] != "box" && parts[0] != "cyl")
                {
                    throw new FormatException($"无法识别的形状行：{line}");
                }

                float[] values = new float[parts.Length - 1];
                for (int i = 1; i < parts.Length; i++)
                {
                    values[i - 1] = float.Parse(parts[i], CultureInfo.InvariantCulture);
                }

                yield return (parts[0], values);
            }
        }

        private static Options ParseOptions(string[] args)
        {
            Options o = new();
            for (int i = 1; i < args.Length; i++)
            {
                string a = args[i];
                switch (a)
                {
                    case "--map": o.Map = args[++i]; break;
                    case "--input-root": o.InputRoot = args[++i]; break;
                    case "--output-root": o.OutputRoot = args[++i]; break;
                    case "--walkable": o.Walkable = args[++i]; break;
                    case "--out": o.Out = args[++i]; break;
                    case "--blockers": o.Blockers = args[++i]; break;
                    case "--areas": o.Areas = args[++i]; break;
                    case "--partition": o.Partition = args[++i]; break;
                    case "--tile-size": o.TileSize = Float(args[++i]); break;
                    case "--cs": o.Cs = Float(args[++i]); break;
                    case "--ch": o.Ch = Float(args[++i]); break;
                    case "--slope": o.Slope = Float(args[++i]); break;
                    case "--height": o.Height = Float(args[++i]); break;
                    case "--radius": o.Radius = Float(args[++i]); break;
                    case "--climb": o.Climb = Float(args[++i]); break;
                    case "--region-min": o.RegionMin = (int)Float(args[++i]); break;
                    case "--region-merge": o.RegionMerge = (int)Float(args[++i]); break;
                    case "--edge-len": o.EdgeLen = Float(args[++i]); break;
                    case "--edge-error": o.EdgeError = Float(args[++i]); break;
                    case "--verts-per-poly": o.VertsPerPoly = (int)Float(args[++i]); break;
                    case "--detail-dist": o.DetailDist = Float(args[++i]); break;
                    case "--detail-error": o.DetailError = Float(args[++i]); break;
                    default: throw new ArgumentException($"未知参数：{a}");
                }
            }

            return o;
        }

        private static float Float(string text)
        {
            return float.Parse(text, CultureInfo.InvariantCulture);
        }
    }
}
