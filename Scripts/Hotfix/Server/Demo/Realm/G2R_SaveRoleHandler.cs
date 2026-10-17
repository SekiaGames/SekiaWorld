namespace ET.Server
{
    /// <summary>
    /// 玩家**掉线**（心跳超时）时，Gate 把该角色停在哪儿回写给 RealM；RealM 只改 Mongo 里那一个角色的位置/朝向。
    ///
    /// <para>
    /// ⚠️ 只处理"掉线"，**不处理正常退出**（客户交代：点退出按钮/退程序那条路目前不做）。
    /// </para>
    /// </summary>
    [MessageHandler(SceneType.Realm)]
    public class G2R_SaveRoleHandler: MessageHandler<Scene, G2R_SaveRole, R2G_SaveRole>
    {
        protected override async ETTask Run(Scene scene, G2R_SaveRole request, R2G_SaveRole response)
        {
            string account = request.Account;
            bool ok = await AccountRepository.SaveRolePosition(scene, account, request.Sequence,
                    request.PosX, request.PosY, request.PosZ, request.RotY, GateMapConst.Normalize(request.MapId));

            if (!ok)
            {
                response.Error = ErrorCode.ERR_RoleNotFound;
                response.Message = "账号没找到";
            }

            Log.Info($"[角色存档] RealM 写库（掉线回写位置）：账号 {account} 槽 {request.Sequence + 1} "
                    + $"pos=({request.PosX:F2}, {request.PosY:F2}, {request.PosZ:F2}) rotY={request.RotY:F1} "
                    + $"mapId={request.MapId} ok={ok}");
        }
    }
}
