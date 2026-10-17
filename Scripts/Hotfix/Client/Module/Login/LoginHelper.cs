namespace ET.Client
{
    public static class LoginHelper
    {
        public static async ETTask<AccountOpResult> Login(Scene root, string email, string password, string token, string deviceCode)
        {
            EntityRef<Scene> rootRef = root;

            AccountOpResult result = await ClientAccountHelper.LoginAsync(root, email, password, token, deviceCode);
            if (!result.IsSuccess)
            {
                return result;
            }

            root = rootRef;
            if (root == null)
            {
                return result;
            }

            root.GetComponent<PlayerComponent>().MyId = result.PlayerId;

            ClientRoleListComponent roleList = root.GetComponent<ClientRoleListComponent>();
            if (roleList == null)
            {
                roleList = root.AddComponent<ClientRoleListComponent>();
            }
            roleList.Set(result.Roles);
            Log.Info($"[角色存档] 登录拿到账号角色列表：{result.Roles?.Count ?? 0} 个");

            await EventSystem.Instance.PublishAsync(root, new LoginFinish());
            return result;
        }

        public static async ETTask<AccountOpResult> SendEmailCode(Scene root, string email, int type)
        {
            return await ClientAccountHelper.SendEmailCodeAsync(root, email, type);
        }

        public static async ETTask<AccountOpResult> Register(Scene root, string email, string code, string password)
        {
            return await ClientAccountHelper.RegisterAsync(root, email, code, password);
        }

        public static async ETTask<AccountOpResult> ResetPassword(Scene root, string email, string code, string password)
        {
            return await ClientAccountHelper.ResetPasswordAsync(root, email, code, password);
        }
    }
}
