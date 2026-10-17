using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// Gate 回问 RealM 要某账号的角色列表（登录拿到 key 时，以及 Gate 上缓存缺了时）。
    /// **只有 RealM 读数据库** —— 见 <see cref="AccountRepository"/>。
    /// </summary>
    [MessageHandler(SceneType.Realm)]
    public class G2R_GetRolesHandler: MessageHandler<Scene, G2R_GetRoles, R2G_GetRoles>
    {
        protected override async ETTask Run(Scene scene, G2R_GetRoles request, R2G_GetRoles response)
        {
            string account = request.Account;
            List<AccountRole> roles = await AccountRepository.GetRoles(scene, account);

            response.Roles.AddRange(RoleListHelper.ToInfoList(roles));
            Log.Info($"[角色存档] RealM 读库：账号 {account} 有 {roles.Count} 个角色");
        }
    }
}
