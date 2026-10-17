using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
	// [FriendOf]：写 Player.RoleSequence 是跨实体访问，分析器 ET0002 要求友元。
	[FriendOf(typeof(Player))]
	[MessageSessionHandler(SceneType.Gate)]
	public class C2G_EnterMapHandler : MessageSessionHandler<C2G_EnterMap, G2C_EnterMap>
	{
		protected override async ETTask Run(Session session, C2G_EnterMap request, G2C_EnterMap response)
		{
			Player player = session.GetComponent<SessionPlayerComponent>().Player;

			// 上一次用的是哪个存档位（可能是别的角色）：换角色/重进图时要把**旧 Unit 停在哪**先存回它自己那一档
			int previousSequence = player.RoleSequence;

			// 记下这次用的是哪个存档位上的角色：写库（掉线回写 / 换角色存档）时要靠它定位库里的那一条
			player.RoleSequence = request.RoleSequence;

            response.MyId = player.Id;

            // 等一帧再建 Unit 并下发 M2C_*，先让 G2C_EnterMap 返回，
            // 否则 M2C_StartSceneChange 可能比 G2C_EnterMap 还早到客户端
            EnterMap(session.Root(), player, request.RoleSequence, previousSequence).Coroutine();
            await ETTask.CompletedTask;
        }

        private static async ETTask EnterMap(Scene scene, Player player, int roleSequence, int previousSequence)
        {
            EntityRef<Scene> sceneRef = scene;
            EntityRef<Player> playerRef = player;
            await scene.Fiber().WaitFrameFinish();
            scene = sceneRef;
            player = playerRef;
            if (scene == null || player == null)
            {
                return;
            }

            string account = player.Account;

            // ① 先处理旧 Unit：把「它停在哪」写回**它自己的存档位**，再清掉。
            //    客户端「切换角色 / 重进图」在服务端只表现为这一条 EnterMap，不在这儿落笔，
            //    这段位移就随着旧 Unit 一起丢了（表现出来就是：切完角色再进图回到上一次存档的点）。
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            Unit unit = unitComponent.Get(player.Id);
            if (unit != null)
            {
                // await 之后不能再碰实体 ⇒ 先把 InstanceId 抄下来（只为日志）
                long oldInstanceId = unit.InstanceId;

                if (previousSequence >= 0)
                {
                    await SessionPlayerComponentSystem.SaveUnitPosition(scene, player, unit, previousSequence, "换角色/重进图回写");
                    scene = sceneRef;
                    player = playerRef;
                    if (scene == null || player == null)
                    {
                        return;
                    }

                    unitComponent = scene.GetComponent<UnitComponent>();
                }

                Log.Info($"[进图] 清掉旧 Unit（instance={oldInstanceId}，存档位 {previousSequence + 1}）");
                unitComponent.Remove(player.Id);
            }

            // ② 出生在**哪张图** = 这个存档位存在角色存档里的地图（`RoleInfo.MapId`，见 C2G_CreateRoleHandler /
            //    C2G_ChangeMapHandler）；老存档没有这个字段 ⇒ 读出来 0 ⇒ 兜底默认图，老号行为不变。
            //    出生点 = 这个存档位**上次存下来的位置/朝向**（"作为下次上线时的位置"，它属于上面那张图）——
            //    数据本体在 RealM 的 Mongo 里，只有 RealM 读写数据库 ⇒ 这里问它要一次角色列表。
            //    拿不到（没存过 / 查询失败 / 那一档被删了）就退回该图的出生点 GateMapConst.BornPointOf(mapId)。
            int mapId = GateMapConst.DefaultMapId;
            Unity.Mathematics.float3 spawnPos = GateMapConst.BornPointOf(mapId);
            float spawnRotY = 0f;
            try
            {
                G2R_GetRoles g2RGetRoles = G2R_GetRoles.Create();
                g2RGetRoles.Account = account;
                R2G_GetRoles r2GGetRoles = (R2G_GetRoles) await scene.GetComponent<MessageSender>().Call(
                        RealmHelper.GetRealm(scene.Zone()).ActorId, g2RGetRoles);

                scene = sceneRef;
                player = playerRef;
                if (scene == null || player == null)
                {
                    return;
                }

                foreach (RoleInfo role in r2GGetRoles.Roles)
                {
                    if (role.Sequence != roleSequence)
                    {
                        continue;
                    }

                    mapId = GateMapConst.Normalize(role.MapId);
                    spawnPos = new Unity.Mathematics.float3(role.PosX, role.PosY, role.PosZ);
                    spawnRotY = role.RotY;
                    Log.Info($"[进图] 用存档位置出生：账号 {account} 槽 {roleSequence + 1} "
                            + $"pos=({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2}) rotY={spawnRotY:F1} "
                            + $"mapId={mapId}（{GateMapConst.SceneNameOf(mapId)}）");
                    break;
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[进图] 读存档位置失败（改用默认出生点）：账号 {account} → {e}");
                scene = sceneRef;
                player = playerRef;
                if (scene == null || player == null)
                {
                    return;
                }
            }

            // 存档里的图可能已经不在表里 / 那份导航没烘（换过表、或美术还没导导航）⇒ 退回默认图，
            // 否则 UnitFactory 建 PathfindingComponent 时会抛 "no nav data" 把整个协程炸掉。
            if (!MapConfigCategory.Instance.Contain(mapId) || !GateMapConst.HasNav(mapId))
            {
                Log.Warning($"[进图] 存档里的地图 id={mapId} 表里没有或导航没烘 ⇒ 退回默认图 {GateMapConst.DefaultMapId}"
                        + $"（{GateMapConst.SceneNameOf(GateMapConst.DefaultMapId)}）");
                mapId = GateMapConst.DefaultMapId;
                spawnPos = GateMapConst.BornPointOf(mapId);
            }

            unitComponent = scene.GetComponent<UnitComponent>();

            // Gate 兼任 Map：Unit 直接建在本 Gate 场景里，不再传送到独立的 Map 服务器
            unit = UnitFactory.Create(scene, player.Id, UnitFactory.PlayerConfigId, spawnPos, mapId);

            // 朝向也按存档还原（客户端用 Forward 求 yaw；写库那边用的就是 atan2(Forward.x, Forward.z)）
            double radY = spawnRotY * System.Math.PI / 180.0;
            unit.Forward = new Unity.Mathematics.float3((float)System.Math.Sin(radY), 0f, (float)System.Math.Cos(radY));

            // 通知客户端开始切场景：**图来自这个角色的存档**（客户端按 SceneName 加载
            // res://Scene/{MapConfig.NameEn}/{SceneName}.tscn，见 MapSceneHelper.Resolve）
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneInstanceId = scene.InstanceId;
            m2CStartSceneChange.SceneName = GateMapConst.SceneNameOf(mapId);
            MapMessageHelper.SendToClient(unit, m2CStartSceneChange);

            // 通知客户端创建 My Unit
            M2C_CreateMyUnit m2CCreateMyUnit = M2C_CreateMyUnit.Create();
            m2CCreateMyUnit.Unit = UnitHelper.CreateUnitInfo(unit);
            MapMessageHelper.SendToClient(unit, m2CCreateMyUnit);

            // 按布点数据把这张图的 NPC / 怪物摆出来（数据来自美术的 Demos/MapPoints/{地图名}.tscn）
            int spawned = SceneNpcSpawner.SpawnAll(scene, mapId);
            Log.Info($"[NPC] {GateMapConst.SceneNameOf(mapId)}：本次新建 {spawned} 个 NPC/怪物 Unit");

            await NotifyUnitsLater(scene, player.Id);
        }

		/// <summary>
		/// 客户端的 Current 场景在 G2C_EnterMap 响应之后才创建，视野进出通知会早到被丢掉；
		/// 等一拍再把玩家**视野内**的单位补推一遍（客户端按 id 去重，幂等）。换图（C2G_ChangeMapHandler）复用同一段。
		///
		/// <para>
		/// 为什么不是只补推一个：一张图上现在有上百个 NPC/怪物（来自布点数据），逐个
		/// <c>NoticeUnitAdd</c> 会发上百个包 ⇒ 这里**合并成一条** <c>M2C_CreateUnits</c>。
		/// </para>
		/// </summary>
		internal static async ETTask NotifyUnitsLater(Scene scene, long playerId)
		{
			EntityRef<Scene> sceneRef = scene;
			await scene.TimerComponent.WaitAsync(1000);
			scene = sceneRef;
			if (scene == null)
			{
				return;
			}

			UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
			Unit player = unitComponent?.Get(playerId);
			if (player == null)
			{
				return;
			}

			// ⚠️ 用 GetAsLong 而不是 GetAsFloat：GetAsFloat 会**再除以 10000**（那是给「倍率型」数值用的），
			//    而 AOI 这里存的就是「米 × 1000」的绝对值（15000 = 15 米）—— 用 GetAsFloat 会算出 0 米。
			float viewDist = player.NumericComponent.GetAsLong(NumericType.AOI) / 1000f;
			float sqr = viewDist * viewDist;

			List<Unit> seen = new();
			foreach (Entity e in unitComponent.Children.Values)
			{
				if (e is not Unit u || u.Id == player.Id || u.MapId != player.MapId)
				{
					continue;
				}

				if (math.distancesq(u.Position, player.Position) <= sqr)
				{
					seen.Add(u);
				}
			}

			MapMessageHelper.NoticeUnitAddBatch(player, seen);
			Log.Info($"[进图] 补推视野内单位 {seen.Count} 个（半径 {viewDist:F0} 米；客户端场景建好前的 AOI 通知会丢）");
		}
	}
}
