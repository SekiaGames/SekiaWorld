using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ET
{
    /// <summary>
    /// 地图布点数据的运行时读取口 —— 数据由美术在
    /// <c>.Art/SekiaArt_Map/Demos/MapPoints/&lt;地图名&gt;.tscn</c> 里摆、跑那张场景导出
    /// （见 <c>Demos/MapPoints/MapPointsExporter.cs</c>）。
    ///
    /// <para>
    /// **不走 <c>ConfigLoader</c>**（那套是 Excel 表形状的配置）；这里读的是各图的点位数据，形状不同。
    /// 产物目录与变体选择**照抄配置那套规矩**（见 <c>Scripts/Loader/ConfigLoaderInvoker.cs</c> 的 <c>ConfigPath</c>）：
    /// <list type="bullet">
    /// <item>编辑器里按 <c>Init.codeMode</c>：Client → <c>Bundles/MapPoints</c>（= <c>c</c> 变体）、
    ///   Server → <c>.FrameWork/Config/MapPoints_BytesExport/s</c>、两端都跑 → <c>.../cs</c>。</item>
    /// <item>打包后一律读 <c>res://Bundles/MapPoints/&lt;地图名&gt;.bytes</c>（包内那份 = c 变体）。</item>
    /// </list>
    /// </para>
    /// <para>
    /// ⚠️ **读文件那步在 <c>Loader</c> 层**（<c>MapPointsBytesReader</c>，走 <see cref="EventSystem"/> 的 Invoke
    /// —— 与导航数据 <c>RecastFileReader</c> 同一套）：本文件在 <c>Model/Share</c>，客户端与服务端都要用，
    /// 而 <c>Model</c> 看不到 <c>Loader</c> 程序集。
    /// 请求类型 <see cref="MapPointsReadBytes"/> 因此**住 Core**（Loader 层与 Model 层唯一的共同引用），
    /// 别挪到本文件里来 —— 那样 Loader 层的处理器就看不到它了。
    /// </para>
    /// <para>
    /// 地图名 = <c>MapConfig.NameEn</c>（<c>prt_1</c> / <c>Prontera</c>）—— 与服务端下发的场景名差一个 <c>Scene</c> 前缀，
    /// 用 <see cref="MapNameOfScene"/> 换。
    /// </para>
    /// </summary>
    public static class MapPointsLoader
    {
        /// <summary>场景名（服务端下发的 <c>Sceneprt_1</c>）→ 地图名（<c>prt_1</c>）</summary>
        public static string MapNameOfScene(string sceneName)
        {
            return sceneName != null && sceneName.StartsWith("Scene")? sceneName["Scene".Length..] : sceneName;
        }

        /// <summary>读一张图的布点；读不到（没导过 / 该图没数据）返回 null，不抛（读文件那层已经打过日志）</summary>
        public static MapPointsData Load(string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
            {
                return null;
            }

            byte[] bytes = EventSystem.Instance.Invoke<MapPointsReadBytes, byte[]>(
                    new MapPointsReadBytes { MapName = mapName });
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                return Parse(bytes);
            }
            catch (Exception e)
            {
                Log.Error($"[布点] {mapName}.bytes 解析失败：{e}");
                return null;
            }
        }

        /// <summary>出生点：取 id 最小的那个（RO 的约定 —— 出生点 ID=1 是默认出生点）</summary>
        public static BornPointInfo DefaultBornPoint(MapPointsData data)
        {
            if (data == null || data.BornPoints.Count == 0)
            {
                return null;
            }

            BornPointInfo best = data.BornPoints[0];
            foreach (BornPointInfo p in data.BornPoints)
            {
                if (p.Id < best.Id)
                {
                    best = p;
                }
            }

            return best;
        }

        /// <summary>按 id 找出生点（出口的 <c>target_born_point</c> 指的就是它）；找不到返回 null</summary>
        public static BornPointInfo FindBornPoint(MapPointsData data, int id)
        {
            if (data == null)
            {
                return null;
            }

            foreach (BornPointInfo p in data.BornPoints)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }

            return null;
        }

        /// <summary>本图的主相机（相机只给客户端用；一张图一台，多台时取 id 最小的）</summary>
        public static CameraInfo MainCamera(MapPointsData data)
        {
            if (data == null || data.Cameras.Count == 0)
            {
                return null;
            }

            CameraInfo best = data.Cameras[0];
            foreach (CameraInfo c in data.Cameras)
            {
                if (c.Id < best.Id)
                {
                    best = c;
                }
            }

            return best;
        }

        /// <summary>
        /// 解析 bytes。布局与导出器 <c>MapPointsExporter.ToBytes</c> **一一对应**（小端）：
        /// <code>
        /// char[4] "MPTS" | int32 version=2 | int32 mapNameLen + utf8
        /// int32 bornCount | int32 exitCount | int32 camCount | int32 spawnCount
        /// born  : int32 id | float x,y,z
        /// exit  : int32 id, int32 targetMap, int32 targetBornPoint, float radius | float x,y,z
        /// cam   : int32 id, float fov, float qx,qy,qz,qw | float focusX,focusY,focusZ
        /// spawn : int32 id, int32 uniqueId, int32 num, int32 isMonster, float dir,
        ///         int32 waitActionLen + utf8, int32 mapIconLen + utf8,
        ///         float scaleMin, scaleMax | float x,y,z
        /// </code>
        /// ⚠️ v2 相对 v1 只追加了 spawn 段，但**版本号对不上会直接抛** ⇒ 改了导出器必须重导所有图的布点。
        /// </summary>
        public static MapPointsData Parse(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var r = new BinaryReader(ms, Encoding.UTF8);
            string magic = Encoding.ASCII.GetString(r.ReadBytes(4));
            if (magic != "MPTS")
            {
                throw new Exception("魔数不是 MPTS（是不是读成 json 了？）");
            }

            int version = r.ReadInt32();
            if (version != 2)
            {
                throw new Exception($"不认识的版本 {version}（导出器与运行时版本不匹配；" +
                        "改了 MapPointsExporter/Loader 之后要重导所有图的布点）");
            }

            var data = new MapPointsData { Map = Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32())) };
            int bornCount = r.ReadInt32();
            int exitCount = r.ReadInt32();
            int camCount = r.ReadInt32();
            int spawnCount = r.ReadInt32();

            for (int i = 0; i < bornCount; ++i)
            {
                data.BornPoints.Add(new BornPointInfo
                {
                    Id = r.ReadInt32(), X = r.ReadSingle(), Y = r.ReadSingle(), Z = r.ReadSingle(),
                });
            }

            for (int i = 0; i < exitCount; ++i)
            {
                data.ExitPoints.Add(new ExitPointInfo
                {
                    Id = r.ReadInt32(),
                    TargetMap = r.ReadInt32(),
                    TargetBornPoint = r.ReadInt32(),
                    Radius = r.ReadSingle(),
                    X = r.ReadSingle(),
                    Y = r.ReadSingle(),
                    Z = r.ReadSingle(),
                });
            }

            for (int i = 0; i < camCount; ++i)
            {
                data.Cameras.Add(new CameraInfo
                {
                    Id = r.ReadInt32(),
                    Fov = r.ReadSingle(),
                    Qx = r.ReadSingle(),
                    Qy = r.ReadSingle(),
                    Qz = r.ReadSingle(),
                    Qw = r.ReadSingle(),
                    FocusX = r.ReadSingle(),
                    FocusY = r.ReadSingle(),
                    FocusZ = r.ReadSingle(),
                });
            }

            for (int i = 0; i < spawnCount; ++i)
            {
                data.SpawnPoints.Add(new SpawnPointInfo
                {
                    Id = r.ReadInt32(),
                    UniqueId = r.ReadInt32(),
                    Num = r.ReadInt32(),
                    IsMonster = r.ReadInt32() != 0,
                    Dir = r.ReadSingle(),
                    WaitAction = ReadText(r),
                    MapIcon = ReadText(r),
                    ScaleMin = r.ReadSingle(),
                    ScaleMax = r.ReadSingle(),
                    X = r.ReadSingle(),
                    Y = r.ReadSingle(),
                    Z = r.ReadSingle(),
                });
            }

            return data;
        }

        /// <summary>读「长度 + utf8」字符串（与导出器的 <c>WriteText</c> 对应）</summary>
        private static string ReadText(BinaryReader r)
        {
            int len = r.ReadInt32();
            return len <= 0? string.Empty : Encoding.UTF8.GetString(r.ReadBytes(len));
        }
    }

    /// <summary>一张图上的布点数据（三类坐标都已是 Godot 空间 —— 美术在场景里摆的就是最终值）</summary>
    [EnableClass]
    public sealed class MapPointsData
    {
        /// <summary>地图名 = <c>MapConfig.NameEn</c>（<c>prt_1</c> / <c>Prontera</c>）</summary>
        public string Map;

        public List<BornPointInfo> BornPoints = new();
        public List<ExitPointInfo> ExitPoints = new();
        public List<CameraInfo> Cameras = new();

        /// <summary>
        /// NPC / 怪物点位（<c>NpcPoint</c> + <c>MonsterPoint</c> 两个分组合并，用 <see cref="SpawnPointInfo.IsMonster"/> 区分）。
        /// **只有 <c>s</c> / <c>cs</c> 变体带这一段**（客户端变体 <c>c</c> 没有 —— 客户端按服务端下发的 Unit 快照建实体）。
        /// </summary>
        public List<SpawnPointInfo> SpawnPoints = new();
    }

    /// <summary>
    /// 一个 NPC / 怪物点位。字段语义与 RO-Server <c>SceneInfo.lua</c> 的 <c>NPCPoints</c> 一致
    /// （<c>Dir</c> 是 RO 原始角度、**没有**换算成 Godot 朝向）。
    /// </summary>
    [EnableClass]
    public sealed class SpawnPointInfo
    {
        /// <summary>配表 id：<see cref="IsMonster"/> 为 false 时查 <c>NpcConfig</c>，为 true 时查 <c>MonsterConfig</c></summary>
        public int Id;

        /// <summary>服务端唯一 NPC 编号（0 = 非唯一）</summary>
        public int UniqueId;

        /// <summary>刷新数量</summary>
        public int Num;

        /// <summary>是不是怪物点位</summary>
        public bool IsMonster;

        /// <summary>朝向（RO 角度，度）</summary>
        public float Dir;

        /// <summary>待机动作名（空 = 默认 <c>wait</c>）</summary>
        public string WaitAction;

        /// <summary>小地图图标（空 = 不显示）</summary>
        public string MapIcon;

        public float ScaleMin;
        public float ScaleMax;

        public float X;
        public float Y;
        public float Z;
    }

    /// <summary>出生点（<c>bp_&lt;id&gt;</c>）：只有坐标，id 就是 RO 的出生点 ID（出口的 <c>target_born_point</c> 引用它）</summary>
    [EnableClass]
    public sealed class BornPointInfo
    {
        public int Id;
        public float X;
        public float Y;
        public float Z;
    }

    /// <summary>出口 = 传送阵（<c>ex_&lt;id&gt;</c>）：走进去切到 <see cref="TargetMap"/> 的 <see cref="TargetBornPoint"/> 出生点</summary>
    [EnableClass]
    public sealed class ExitPointInfo
    {
        public int Id;
        public int TargetMap;
        public int TargetBornPoint;

        /// <summary>触发半径（米）</summary>
        public float Radius;

        public float X;
        public float Y;
        public float Z;
    }

    /// <summary>相机（<c>cam_&lt;id&gt;</c>，Camera3D 节点）：只给客户端用</summary>
    [EnableClass]
    public sealed class CameraInfo
    {
        public int Id;

        /// <summary>视野角（度）</summary>
        public float Fov;

        /// <summary>相机朝向（四元数，已是 Godot 空间 —— 运行时直接套，不做 Unity 换算）</summary>
        public float Qx;
        public float Qy;
        public float Qz;
        public float Qw;

        /// <summary>焦点视口（x/y 是 0..1 视口坐标，z 是**离相机的距离**）</summary>
        public float FocusX;
        public float FocusY;
        public float FocusZ;
    }
}
