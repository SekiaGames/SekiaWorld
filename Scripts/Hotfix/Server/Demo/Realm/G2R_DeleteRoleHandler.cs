namespace ET.Server
{
    /// <summary>
    /// 删角：Gate 转发过来的删除请求，在 RealM 侧删库（该槽本来没角色则失败）。
    /// ⚠️ Unity/RO 那边删角色有冷却，客户明确说**不做冷却，直接删**。
    /// 回带**删除后的完整列表**。
    /// </summary>
    [MessageHandler(SceneType.Realm)]
    public class G2R_DeleteRoleHandler: MessageHandler<Scene, G2R_DeleteRole, R2G_DeleteRole>
    {
        protected override async ETTask Run(Scene scene, G2R_DeleteRole request, R2G_DeleteRole response)
        {
            EntityRef<Scene> sceneRef = scene;
            string account = request.Account;
            int sequence = request.Sequence;

            if (!await AccountRepository.DeleteRole(scene, account, sequence))
            {
                response.Error = ErrorCode.ERR_RoleNotFound;
                response.Message = $"存档位 {sequence + 1} 上没有角色";
                Log.Warning($"[角色存档] RealM 删角失败（槽上没有角色）：账号 {account} 槽 {sequence + 1}");
                return;
            }

            Log.Info($"[角色存档] RealM 写库（删角）：账号 {account} 槽 {sequence + 1}");

            scene = sceneRef;
            if (scene == null)
            {
                return;
            }
            response.Roles.AddRange(RoleListHelper.ToInfoList(await AccountRepository.GetRoles(scene, account)));
        }
    }
}
