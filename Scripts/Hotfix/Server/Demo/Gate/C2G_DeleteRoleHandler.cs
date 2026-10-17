namespace ET.Server
{
    /// <summary>
    /// 客户端删角 → **转发给 RealM 删库**（Gate 不碰数据库）。
    /// ⚠️ 不做 Unity/RO 那套删除冷却，直接删（客户明确要求）。
    /// </summary>
    [FriendOf(typeof(Player))]
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_DeleteRoleHandler: MessageSessionHandler<C2G_DeleteRole, G2C_DeleteRole>
    {
        protected override async ETTask Run(Session session, C2G_DeleteRole request, G2C_DeleteRole response)
        {
            Scene root = session.Root();
            Player player = session.GetComponent<SessionPlayerComponent>().Player;
            if (player == null)
            {
                response.Error = ErrorCore.ERR_UnverifiedSessionSendMessage;
                response.Message = "还没登录 Gate";
                return;
            }

            string account = player.Account;
            int sequence = request.Sequence;

            G2R_DeleteRole g2RDeleteRole = G2R_DeleteRole.Create();
            g2RDeleteRole.Account = account;
            g2RDeleteRole.Sequence = sequence;

            R2G_DeleteRole r2GDeleteRole = (R2G_DeleteRole) await root.GetComponent<MessageSender>().Call(
                    RealmHelper.GetRealm(root.Zone()).ActorId, g2RDeleteRole);

            response.Error = r2GDeleteRole.Error;
            response.Message = r2GDeleteRole.Message;
            response.Roles.AddRange(r2GDeleteRole.Roles);
            Log.Info($"[角色存档] Gate 转发删角给 RealM：账号 {account} 槽 {sequence + 1}"
                    + $"→ Error={response.Error}（回带列表 {response.Roles.Count} 个）");
        }
    }
}
