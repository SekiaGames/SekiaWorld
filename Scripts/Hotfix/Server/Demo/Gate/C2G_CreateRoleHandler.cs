namespace ET.Server
{
    /// <summary>
    /// 客户端创角 → **转发给 RealM 写库**（Gate 不碰数据库，只中转；数据本体在 RealM 的 Mongo 里）。
    /// 对应 Unity/RO 那条「Gate 通知 RealM 更新角色列表、同步角色属性」。
    /// </summary>
    // [FriendOf]：读 Player.Account 是跨实体访问，分析器 ET0002 要求友元。
    [FriendOf(typeof(Player))]
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_CreateRoleHandler: MessageSessionHandler<C2G_CreateRole, G2C_CreateRole>
    {
        protected override async ETTask Run(Session session, C2G_CreateRole request, G2C_CreateRole response)
        {
            Scene root = session.Root();
            Player player = session.GetComponent<SessionPlayerComponent>().Player;
            if (player == null)
            {
                response.Error = ErrorCore.ERR_UnverifiedSessionSendMessage;
                response.Message = "还没登录 Gate";
                return;
            }

            // 先把要用的都抄成局部变量：await 之后不再碰 session/player/request（ET 的实体使用规范）
            string account = player.Account;
            int sequence = request.Sequence;
            string name = request.Name;
            int gender = request.Gender;
            int destClassId = request.DestClassId;

            G2R_CreateRole g2RCreateRole = G2R_CreateRole.Create();
            g2RCreateRole.Account = account;
            g2RCreateRole.Sequence = sequence;
            g2RCreateRole.Name = name;
            g2RCreateRole.Gender = gender;
            g2RCreateRole.DestClassId = destClassId;
            g2RCreateRole.Parts.AddRange(request.Parts);

            // 出生点 + 出生图：新角色一建出来就站在**默认图**的出生点上 ⇒ 直接写进库，不用等第一次掉线回写
            int mapId = GateMapConst.DefaultMapId;
            var spawn = GateMapConst.BornPointOf(mapId);
            g2RCreateRole.MapId = mapId;
            g2RCreateRole.PosX = spawn.x;
            g2RCreateRole.PosY = spawn.y;
            g2RCreateRole.PosZ = spawn.z;

            R2G_CreateRole r2GCreateRole = (R2G_CreateRole) await root.GetComponent<MessageSender>().Call(
                    RealmHelper.GetRealm(root.Zone()).ActorId, g2RCreateRole);

            response.Error = r2GCreateRole.Error;
            response.Message = r2GCreateRole.Message;
            response.Roles.AddRange(r2GCreateRole.Roles);
            Log.Info($"[角色存档] Gate 转发创角给 RealM：账号 {account} 槽 {sequence + 1} 名字「{name}」"
                    + $"→ Error={response.Error}（回带列表 {response.Roles.Count} 个）");
        }
    }
}
