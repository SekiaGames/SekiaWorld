namespace ET.Server
{
    /// <summary>
    /// 地图常量与查询入口 —— **按地图 id 查，不再是编译期单一常量**。
    ///
    /// <para>
    /// 地图 id = <c>MapConfig.Id</c>（如 1 = 普隆德拉 / 2 = 普隆德拉南门 prt_1），与 RO 侧数据同口径
    /// （<c>SceneInfo.lua</c> 的 <c>ExitPoints.next_scene_ID</c>、<c>Table_Map.id</c>）。
    /// 角色的所在地图存在**角色存档**里（<c>RoleInfo.MapId</c>）：进图时按它决定出生图与下发的地图名，
    /// 传送时改写它（<c>C2G_ChangeMapHandler</c>）。
    /// </para>
    /// <para>
    /// 一张图对应两处资源：
    ///   服务端寻路数据 <c>.FrameWork/Config/RecastOutput/{SceneName}</c>（缺了 <c>PathfindingComponent</c> 会抛 "no nav data"）
    ///   客户端场景     <c>res://Scene/{MapConfig.NameEn}/{SceneName}.tscn</c>（由美术包 SekiaArt_Map.pck 提供）
    /// </para>
    /// </summary>
    public static class GateMapConst
    {
        /// <summary>
        /// 默认地图 = prt_1（普隆德拉南门）：新角色建出来就在这张图，
        /// 老存档缺地图字段（读出来 0）也兜底到它 —— 老号行为不变。
        /// </summary>
        public const int DefaultMapId = 2;

        /// <summary>存档里的地图 id 兜底：<=0（老文档 / 异常值）一律当默认图</summary>
        public static int Normalize(int mapId)
        {
            return mapId > 0? mapId : DefaultMapId;
        }

        /// <summary>
        /// 地图 id → 场景名（= <c>"Scene" + MapConfig.NameEn</c>）。
        /// 例：2 → <c>Sceneprt_1</c>、1 → <c>SceneProntera</c>。
        /// 客户端拿这个名字推资源路径（<c>MapSceneHelper.Resolve</c>：<c>res://Scene/Prontera/SceneProntera.tscn</c>），
        /// 所以**改表里的 NameEn 就等于改场景名**。
        /// </summary>
        public static string SceneNameOf(int mapId)
        {
            return "Scene" + MapConfigCategory.Instance.Get(mapId).NameEn;
        }

        /// <summary>
        /// 地图 id → **默认出生点**（= 该图布点数据里 id 最小的那个 <c>bp_*</c>；RO 约定出生点 ID=1 是默认出生点）。
        ///
        /// <para>
        /// 数据出处：各图 <c>.Art/SekiaArt_Map/Demos/MapPoints/&lt;地图名&gt;.tscn</c> → 跑一次布点导出
        /// （<c>.Art/NavExport-SekiaArt_Map.command</c> 或编辑器里 F6 单张）。
        /// ⚠️ 坐标由美术在场景里摆，**已经是 Godot 空间**（含镜像）—— 这里只把字节读出来，不做任何换算。
        /// </para>
        /// </summary>
        public static Unity.Mathematics.float3 BornPointOf(int mapId)
        {
            return BornPointOf(mapId, 0);
        }

        /// <summary>
        /// 地图 id + 出生点 id → 坐标；<paramref name="bornPointId"/> ≤ 0 或该 id 不存在时退回默认出生点。
        /// （传送阵会把出口的 <c>target_born_point</c> 带过来 —— 同一个阵每次落到同一个位置。）
        /// </summary>
        public static Unity.Mathematics.float3 BornPointOf(int mapId, int bornPointId)
        {
            string mapName = MapConfigCategory.Instance.Get(mapId).NameEn;
            MapPointsData data = MapPointsLoader.Load(mapName);
            BornPointInfo point = MapPointsLoader.FindBornPoint(data, bornPointId)
                    ?? MapPointsLoader.DefaultBornPoint(data);
            if (point == null)
            {
                throw new System.Exception($"[地图] {mapName}（id={mapId}）没有可用的出生点数据"
                        + " —— 先导一次布点（.Art/NavExport-SekiaArt_Map.command）");
            }

            return new Unity.Mathematics.float3(point.X, point.Y, point.Z);
        }

        /// <summary>
        /// 这张图的**服务端导航数据**烘好了没有（<c>.FrameWork/Config/RecastOutput/{SceneName}</c>）。
        /// <c>PathfindingComponent</c> 建 Unit 时读不到会直接抛 <c>no nav data</c>（会把整个协程炸掉），
        /// 所以传送/进图之前先在这儿拦一道，给客户端一个干净的错误码。
        /// 顺带把字节读进 <c>NavmeshComponent</c> 的缓存，真正建 Unit 时不再读盘。
        /// </summary>
        public static bool HasNav(int mapId)
        {
            try
            {
                NavmeshComponent.Instance.Get(SceneNameOf(mapId));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
