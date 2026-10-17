using System;

namespace ET.Client
{
    /// <summary>
    /// 传送（换地图）：请服务端把角色挪到另一张图。
    ///
    /// <para>
    /// 「在哪个图」由服务端从角色存档里维护（<c>RoleInfo.MapId</c> → <c>MapConfig.NameEn</c> → 场景名），
    /// 客户端只报目标地图 id，随后走**和进图完全一样**的切场景链路：
    /// <c>M2C_StartSceneChange</c> → <c>SceneChangeHelper.SceneChangeTo</c> →
    /// <c>MapSceneHelper.Resolve</c> → <c>res://Scene/{NameEn}/Scene{NameEn}.tscn</c>。
    /// </para>
    /// </summary>
    public static class ChangeMapHelper
    {
        /// <param name="mapId">目标地图 = <c>MapConfig.Id</c>（1=普隆德拉 / 2=普隆德拉南门 prt_1）</param>
        /// <param name="targetBornPoint">
        /// 目标出生点 id（布点数据里 <c>bp_&lt;id&gt;</c>；0 = 目标图的默认出生点）。
        /// 传送阵把出口的 <c>target_born_point</c> 带过来 —— 同一个阵每次落到同一个位置。
        /// </param>
        public static async ETTask ChangeMapAsync(Scene root, int mapId, int targetBornPoint = 0)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                C2G_ChangeMap c2GChangeMap = C2G_ChangeMap.Create();
                c2GChangeMap.MapId = mapId;
                c2GChangeMap.TargetBornPoint = targetBornPoint;
                G2C_ChangeMap g2CChangeMap =
                        await root.GetComponent<ClientSenderComponent>().Call(c2GChangeMap) as G2C_ChangeMap;
                root = rootRef;

                // 服务端校验失败（地图不存在 / 导航没烘）时会填 Error，这里只看不抛
                if (g2CChangeMap.Error != ErrorCode.ERR_Success)
                {
                    Log.Error($"[传送] 服务端拒绝：MapId={mapId} Error={g2CChangeMap.Error} "
                            + $"Message={g2CChangeMap.Message}");
                    return;
                }

                Log.Info($"[传送] 服务端已接受：MapId={mapId} → {g2CChangeMap.SceneName}（等场景切完）");

                // 等切场景完成：与进图同一个等待信号（SceneChangeHelper 收尾时 Notify）
                await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
