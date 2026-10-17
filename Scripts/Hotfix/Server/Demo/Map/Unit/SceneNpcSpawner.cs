using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 按布点数据把一张图的 NPC / 怪物摆出来。
    ///
    /// <para>
    /// 数据源：美术在 <c>.Art/SekiaArt_Map/Demos/MapPoints/{地图名}.tscn</c> 的
    /// <c>NpcPoint</c> / <c>MonsterPoint</c> 两个分组里摆的点位（导出成布点 bytes 的 <c>spawn</c> 段，
    /// 只有 <c>s</c> / <c>cs</c> 变体带这一段）。**服务端建 Unit**，客户端按 AOI 收到的 Unit 快照装配模型
    /// （见 <c>AfterUnitCreate_CreateUnitView</c>）—— 与 RO-Unity「NPC 由服务端下发、客户端只做表现」一致。
    /// </para>
    /// <para>
    /// 幂等：Unit id 由「图 + 点位下标 + 该点位的第几个」算出来 ⇒ 重复调用只跳过已存在的，不会重复摆。
    /// </para>
    /// </summary>
    public static class SceneNpcSpawner
    {
        /// <summary>Unit id 基址（与玩家的雪花 id、验证怪的 <c>1000000 + mapId</c> 都不冲突）</summary>
        private const long UnitIdBase = 2000000L;

        /// <summary>怪物巡逻用的 AIConfigId（AIConfig 表里 <c>Name=AI_XunLuo</c> 那一组）</summary>
        private const int PatrolAIConfigId = 1;

        /// <summary>
        /// 服务端的 <c>BEHAVIOUR_MOVE_ABLE</c>（<c>Table_Monster.Behaviors</c> 的 bit0）= 能不能动。
        /// 不能动的（绿草、虫蛹这类植物/蛹）不挂巡逻 AI。
        /// </summary>
        private const int BehaviorMoveAble = 1;

        /// <summary>
        /// 把 <paramref name="mapId"/> 这张图的 NPC / 怪物摆出来，返回**本次新建**的 Unit 数。
        /// 该图没有布点数据（或还没有 NPC 点位）时返回 0，不报错。
        /// </summary>
        public static int SpawnAll(Scene scene, int mapId)
        {
            string mapName = MapConfigCategory.Instance.Get(mapId).NameEn;
            MapPointsData data = MapPointsLoader.Load(mapName);
            if (data == null || data.SpawnPoints.Count == 0)
            {
                return 0;
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            int created = 0;
            for (int i = 0; i < data.SpawnPoints.Count; ++i)
            {
                SpawnPointInfo p = data.SpawnPoints[i];
                // 唯一 NPC（uniqueid != 0）只摆一个；普通点位 / 怪物按 num 摆（num 至少 1）
                int count = p.UniqueId != 0? 1 : math.max(1, p.Num);
                for (int n = 0; n < count; ++n)
                {
                    long unitId = UnitIdOf(mapId, i, n);
                    if (unitComponent.Get(unitId) != null)
                    {
                        continue;
                    }

                    Unit unit = UnitFactory.Create(scene, unitId, p.Id, new float3(p.X, p.Y, p.Z), mapId,
                            ForwardOf(p.Dir));

                    // 怪物点位挂巡逻 AI（NPC / 采集物不挂）；不能动的怪（Behaviors 没 bit0）也不挂。
                    // ⚠️ 必须在 Unit 摆到位之后挂 —— AIComponent.Awake 会把当前位置记成出生点（巡逻圆心）。
                    if (p.IsMonster && CanMove(p.Id))
                    {
                        unit.AddComponent<AIComponent, int>(PatrolAIConfigId);
                    }

                    created++;
                }
            }

            return created;
        }

        /// <summary>
        /// Unit id 规则：<c>2000000 + 图id × 1000000 + 点位下标 × 1000 + 该点位的第 n 个</c>。
        /// 点位下标 &lt; 1000（prt_1 有 149 个）、n &lt; 1000（单个点位最多刷 64 个）⇒ 不会串位。
        /// </summary>
        private static long UnitIdOf(int mapId, int pointIndex, int n)
        {
            return UnitIdBase + mapId * 1000000L + pointIndex * 1000L + n;
        }

        /// <summary>
        /// 这个怪物能不能动（<c>Table_Monster.Behaviors</c> 的 bit0）。
        /// 查不到配表的一律当不能动 —— 宁可不巡逻，也不要给没数据的怪乱挂 AI。
        /// </summary>
        private static bool CanMove(int monsterId)
        {
            if (!MonsterConfigCategory.Instance.Contain(monsterId))
            {
                return false;
            }

            return (MonsterConfigCategory.Instance.Get(monsterId).Behaviors & BehaviorMoveAble) != 0;
        }

        /// <summary>
        /// RO 的 <c>dir</c>（度）→ Unit 的朝向向量。与玩家走同一套约定
        /// （<c>Forward = (sin(yaw), 0, cos(yaw))</c>，客户端用 <c>atan2(Forward.x, Forward.z)</c> 反推 yaw）
        /// ⇒ 也就是 <c>yaw = dir</c>。
        ///
        /// <para>
        /// ⚠️ 这个换算**还没用截图核对过**：美术场景是 x 取反的镜像，朝向可能也要跟着镜像。
        /// 看着不对就改这里一处（客户端那边不用动）。
        /// </para>
        /// </summary>
        private static float3 ForwardOf(float dir)
        {
            float rad = math.radians(dir);
            return new float3(math.sin(rad), 0f, math.cos(rad));
        }
    }
}
