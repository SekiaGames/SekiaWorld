namespace ET.Server
{
    // [FriendOf]：读/写 Player 的字段（Account / RoleSequence）与 Unit 的 Position/Forward 都是跨实体访问，
    // 分析器 ET0002 要求友元。
    [EntitySystemOf(typeof(SessionPlayerComponent))]
    [FriendOf(typeof(Player))]
    [FriendOf(typeof(Unit))]
    public static partial class SessionPlayerComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this SessionPlayerComponent self)
        {
            Scene root = self.Root();
            if (root.IsDisposed)
            {
                return;
            }

            // 发送断线消息给本 Gate 场景里的 Unit。
            // Gate 兼任 Map，Unit 就在本场景，直接投递到 Unit 的邮箱即可，不再需要 Location 转发。
            Unit unit = root.GetComponent<UnitComponent>().Get(self.Player.Id);
            if (unit == null)
            {
                Log.Info($"[角色存档] 掉线：玩家 {self.Player.Id} 在本场景没有 Unit（没进图 / 已清）⇒ 不回写位置");
                return;
            }

            // 玩家**掉线**（收/发都静默超过 SessionTimeoutTime，由 SessionIdleCheckerComponent 判定）时，
            // 把这个角色停在哪回写进库：Gate → RealM → MongoDB（只有 RealM 读写数据库）。
            // ⚠️ 客户交代：**只做掉线**，「点退出按钮 / 退程序」那种正常退出目前不做 ⇒ 按 Session.Error 区分。
            int sessionError = self.GetParent<Session>().Error;
            if (sessionError == ErrorCore.ERR_SessionSendOrRecvTimeout)
            {
                Player player = root.GetComponent<PlayerComponent>().GetById(unit.Id);
                if (player == null)
                {
                    Log.Warning($"[角色存档] 掉线回写跳过：Gate 上找不到 Unit {unit.Id} 对应的 Player");
                }
                else
                {
                    Log.Info($"[角色存档] 检测到掉线（心跳超时 {sessionError}）⇒ 通知 RealM 回写位置（玩家 {self.Player.Id}）");
                    SaveUnitPosition(root, player, unit, player.RoleSequence, "掉线（心跳超时）回写").Coroutine();
                }
            }

            unit.GetComponent<MailBoxComponent>().Add(default, G2M_SessionDisconnect.Create());
        }

        [EntitySystem]
        private static void Awake(this SessionPlayerComponent self)
        {

        }

        /// <summary>
        /// 把某个角色**停在哪儿**写回库（Gate → RealM → MongoDB，只有 RealM 读写数据库）。
        /// 两条路共用：
        ///   ① 掉线（心跳超时）—— <see cref="Destroy"/>
        ///   ② 重进图 / 换角色 —— <see cref="C2G_EnterMapHandler"/> 换掉旧 Unit 之前
        ///      （客户端「切换角色」是纯客户端的，服务端只收得到下一条 EnterMap，
        ///        不在这儿落笔的话，这段位移就随着旧 Unit 被丢掉）
        /// </summary>
        public static async ETTask SaveUnitPosition(Scene root, Player player, Unit unit, int sequence, string reason)
        {
            // await 之后不能再碰实体 ⇒ 先把要用的都抄成局部变量
            string account = player.Account;
            float posX = unit.Position.x;
            float posY = unit.Position.y;
            float posZ = unit.Position.z;
            // 朝向：模型的正面在 +Z（与客户端装配角色时的算法一致），重建 Unit 时再还原成 Forward
            float rotY = (float)(System.Math.Atan2(unit.Forward.x, unit.Forward.z) * 180.0 / System.Math.PI);
            // 在哪张图：位置与地图是一对（存档里也是同一份角色记录），必须一起回写。
            // Normalize：Unit 的图理论上不会缺失，但写库这条路上不该因为一个异常的 0 值再抛异常。
            int mapId = GateMapConst.Normalize(unit.MapId);

            if (sequence < 0)
            {
                Log.Warning($"[角色存档] {reason}跳过：账号 {account} 没记录本次用的是哪个存档位（没进过图？）");
                return;
            }

            G2R_SaveRole request = G2R_SaveRole.Create();
            request.Account = account;
            request.Sequence = sequence;
            request.PosX = posX;
            request.PosY = posY;
            request.PosZ = posZ;
            request.RotY = rotY;
            request.MapId = mapId;

            try
            {
                await root.GetComponent<MessageSender>().Call(RealmHelper.GetRealm(root.Zone()).ActorId, request);
                Log.Info($"[角色存档] {reason}：账号 {account} 槽 {sequence + 1} "
                        + $"pos=({posX:F2}, {posY:F2}, {posZ:F2}) rotY={rotY:F1} mapId={mapId}（{GateMapConst.SceneNameOf(mapId)}）");
            }
            catch (System.Exception e)
            {
                Log.Error($"[角色存档] {reason}失败：账号 {account} 槽 {sequence + 1} → {e}");
            }
        }
    }
}
