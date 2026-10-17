namespace ET.Server
{
	/// <summary>
	/// 传送：把角色挪到**另一张图**。
	///
	/// <para>
	/// 「这个角色在哪张图」是**角色信息**的一部分（存在角色存档的 <c>RoleInfo.MapId</c> 里），
	/// 所以传送 = 改存档里的地图 + 按目标图重建 Unit + 让客户端切场景。
	/// </para>
	/// <para>
	/// 与 <see cref="C2G_EnterMapHandler"/> 的关系：进图 = 「按存档里的图把 Unit 建出来」；
	/// 传送 = 「改存档里的图，再走一遍进图那套动作」。因此流程与它同构：
	/// 先回响应（否则 <c>M2C_StartSceneChange</c> 会比 <c>G2C_ChangeMap</c> 先到客户端）→
	/// 回写旧 Unit 停在哪儿（含旧图）→ 清旧 Unit → 按目标图出生点重建 → 下发
	/// <c>M2C_StartSceneChange</c> / <c>M2C_CreateMyUnit</c> → 目标图的常驻怪。
	/// **客户端不用改**：切场景复用进图那条链（<c>M2C_StartSceneChange</c> → <c>SceneChangeHelper</c>
	/// → <c>MapSceneHelper.Resolve</c> → <c>res://Scene/{NameEn}/Scene{NameEn}.tscn</c>）。
	/// </para>
	/// <para>
	/// ⚠️ 换图必须**立刻落库**（不等掉线回写）：否则玩家换图后掉线/重登会拿旧图的位置回到旧图。
	/// </para>
	/// </summary>
	// [FriendOf]：读 Player.Account / Player.RoleSequence 是跨实体访问，分析器 ET0002 要求友元。
	[FriendOf(typeof(Player))]
	[MessageSessionHandler(SceneType.Gate)]
	public class C2G_ChangeMapHandler : MessageSessionHandler<C2G_ChangeMap, G2C_ChangeMap>
	{
		protected override async ETTask Run(Session session, C2G_ChangeMap request, G2C_ChangeMap response)
		{
			Player player = session.GetComponent<SessionPlayerComponent>().Player;
			if (player == null)
			{
				response.Error = ErrorCore.ERR_UnverifiedSessionSendMessage;
				response.Message = "还没登录 Gate";
				return;
			}

			// 校验放在回响应之前：不合法就直接随 G2C_ChangeMap 回去，不用起协程
			int mapId = request.MapId;
			if (!MapConfigCategory.Instance.Contain(mapId))
			{
				response.Error = ErrorCode.ERR_MapNotFound;
				response.Message = $"地图 id={mapId} 在 MapConfig 里不存在";
				return;
			}

			if (!GateMapConst.HasNav(mapId))
			{
				response.Error = ErrorCode.ERR_MapNavMissing;
				response.Message = $"地图 id={mapId}（{GateMapConst.SceneNameOf(mapId)}）的服务端导航数据没烘"
						+ "（.FrameWork/Config/RecastOutput 下缺这份数据）";
				return;
			}

			response.SceneName = GateMapConst.SceneNameOf(mapId);

			// 先让 G2C_ChangeMap 返回，再切图（与进图同理：不然 M2C_StartSceneChange 可能先到客户端）
			ChangeMap(session.Root(), player, mapId, request.TargetBornPoint).Coroutine();
			await ETTask.CompletedTask;
		}

		/// <summary>
		/// 真正的换图动作。与 <c>C2G_EnterMapHandler.EnterMap</c> 同构，差别只在：
		/// 目标图与出生点不是从存档里读的，而是本次请求指定的；且换完要**立刻**把新图写回库。
		/// </summary>
		private static async ETTask ChangeMap(Scene scene, Player player, int mapId, int targetBornPoint)
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
			int sequence = player.RoleSequence;

			UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
			Unit unit = unitComponent.Get(player.Id);
			if (unit == null)
			{
				Log.Warning($"[传送] 玩家 {player.Id} 在本场景没有 Unit（还没进图？）⇒ 不换图");
				return;
			}

			int oldMapId = unit.MapId;

			// ① 旧 Unit 停在哪儿（含**旧图**）先写回它自己那一档：
			//    位置与地图是一对，分开写会出现"坐标是 A 图的、图却是 B 图"的中间态。
			if (sequence >= 0)
			{
				await SessionPlayerComponentSystem.SaveUnitPosition(scene, player, unit, sequence, "传送离开旧图回写");
				scene = sceneRef;
				player = playerRef;
				if (scene == null || player == null)
				{
					return;
				}

				unitComponent = scene.GetComponent<UnitComponent>();
			}

			// ② 清旧 Unit（寻路 / AOI 都挂在它身上），按目标图出生点重建：
			//    新建的 PathfindingComponent 会自动换成目标图的导航数据。
			Log.Info($"[传送] 账号 {account} 槽 {sequence + 1}：图 {oldMapId}（{GateMapConst.SceneNameOf(oldMapId)}）"
					+ $" → {mapId}（{GateMapConst.SceneNameOf(mapId)}）");
			unitComponent.Remove(player.Id);

			Unity.Mathematics.float3 spawnPos = GateMapConst.BornPointOf(mapId, targetBornPoint);
			Unit myUnit = UnitFactory.Create(scene, player.Id, UnitFactory.PlayerConfigId, spawnPos, mapId);

			// ③ 立刻落库：新图 + 该图出生点（不等掉线回写）
			if (sequence >= 0)
			{
				G2R_SaveRole save = G2R_SaveRole.Create();
				save.Account = account;
				save.Sequence = sequence;
				save.PosX = spawnPos.x;
				save.PosY = spawnPos.y;
				save.PosZ = spawnPos.z;
				save.RotY = 0f;
				save.MapId = mapId;
				try
				{
					await scene.GetComponent<MessageSender>().Call(RealmHelper.GetRealm(scene.Zone()).ActorId, save);
					scene = sceneRef;
					player = playerRef;
					if (scene == null || player == null)
					{
						return;
					}
				}
				catch (System.Exception e)
				{
					// 落库失败不回滚换图：人已经在目标图了，最坏是下次重登回到旧图（比把玩家卡住好）
					Log.Error($"[传送] 落库失败（重登会回到旧图）：账号 {account} 槽 {sequence + 1} → {e}");
					scene = sceneRef;
					player = playerRef;
					if (scene == null || player == null)
					{
						return;
					}
				}

				unitComponent = scene.GetComponent<UnitComponent>();
			}

			// ④ 通知客户端切场景（与进图同一条消息），再补自己的 Unit 快照
			// ⚠️ 上面几个 await 之后不能再引用 await 之前的 Entity 变量（分析器 ET0033）⇒ 重新取一次自己的 Unit
			myUnit = scene.GetComponent<UnitComponent>().Get(player.Id);
			if (myUnit == null)
			{
				Log.Warning($"[传送] 换图后在本场景找不到玩家 {player.Id} 的 Unit ⇒ 不下发切场景");
				return;
			}

			M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
			m2CStartSceneChange.SceneInstanceId = scene.InstanceId;
			m2CStartSceneChange.SceneName = GateMapConst.SceneNameOf(mapId);
			MapMessageHelper.SendToClient(myUnit, m2CStartSceneChange);

			M2C_CreateMyUnit m2CCreateMyUnit = M2C_CreateMyUnit.Create();
			m2CCreateMyUnit.Unit = UnitHelper.CreateUnitInfo(myUnit);
			MapMessageHelper.SendToClient(myUnit, m2CCreateMyUnit);

			// ⑤ 目标图的 NPC / 怪物（按布点数据摆；幂等，已存在的跳过）+ 常驻验证怪
			int spawned = SceneNpcSpawner.SpawnAll(scene, mapId);
			Log.Info($"[NPC] {GateMapConst.SceneNameOf(mapId)}：本次新建 {spawned} 个 NPC/怪物 Unit");

			Log.Info($"[传送] 完成：账号 {account} 槽 {sequence + 1} 已在 {GateMapConst.SceneNameOf(mapId)} "
					+ $"pos=({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})");

			// 客户端的 Current 场景在 G2C_ChangeMap 响应之后才建，视野进出通知会早到被丢掉 ⇒ 等一拍再补推
			await C2G_EnterMapHandler.NotifyUnitsLater(scene, player.Id);
		}
	}
}
