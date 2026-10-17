namespace ET.Server
{
    /// <summary>
    /// 创角：Gate 转发过来的创角请求，在 RealM 侧写库（存档位被占则失败）。
    /// 回带**创建后的完整列表**，Gate 直接转给客户端刷新选角界面。
    /// </summary>
    [MessageHandler(SceneType.Realm)]
    public class G2R_CreateRoleHandler: MessageHandler<Scene, G2R_CreateRole, R2G_CreateRole>
    {
        protected override async ETTask Run(Scene scene, G2R_CreateRole request, R2G_CreateRole response)
        {
            EntityRef<Scene> sceneRef = scene;
            string account = request.Account;
            AccountRole role = RoleListHelper.FromCreateRequest(request);

            if (!await AccountRepository.CreateRole(scene, account, role))
            {
                response.Error = ErrorCode.ERR_RoleSlotTaken;
                response.Message = $"存档位 {role.Sequence + 1} 上已经有角色了";
                Log.Warning($"[角色存档] RealM 创角失败（槽已占）：账号 {account} 槽 {role.Sequence + 1}");
                return;
            }

            Log.Info($"[角色存档] RealM 写库（创角）：账号 {account} 槽 {role.Sequence + 1} 名字「{role.Name}」");

            // 回带创建后的完整列表
            scene = sceneRef;
            if (scene == null)
            {
                return;
            }
            response.Roles.AddRange(RoleListHelper.ToInfoList(await AccountRepository.GetRoles(scene, account)));
        }
    }
}
