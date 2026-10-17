namespace ET.Client
{
    [FriendOf(typeof(ClientSenderComponent))]
    public static class ClientAccountHelper
    {
        public static async ETTask<AccountOpResult> LoginAsync(Scene root, string email, string password, string token, string deviceCode)
        {
            EntityRef<Scene> rootRef = root;

            root.RemoveComponent<ClientSenderComponent>();

            ClientSenderComponent sender = await EnsureNetClientAsync(root);
            root = rootRef;
            if (root == null || sender == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            Main2NetClient_Login request = Main2NetClient_Login.Create();
            request.OwnerFiberId = root.Fiber().Id;
            request.Account = email;
            request.Password = password;
            request.Token = token;
            request.DeviceCode = deviceCode;

            NetClient2Main_Login response = await CallNetClientAsync<NetClient2Main_Login>(root, sender, request);
            if (response == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            using (response)
            {
                if (response.Error != ErrorCode.ERR_Success)
                {
                    return AccountOpResult.Fail(response.Error, response.Message);
                }

                return AccountOpResult.Success(response.PlayerId, response.Token,
                        new System.Collections.Generic.List<RoleInfo>(response.Roles));
            }
        }

        public static async ETTask<AccountOpResult> SendEmailCodeAsync(Scene root, string email, int type)
        {
            EntityRef<Scene> rootRef = root;
            ClientSenderComponent sender = await EnsureNetClientAsync(root);
            root = rootRef;
            if (root == null || sender == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            Main2NetClient_GetEmailCode request = Main2NetClient_GetEmailCode.Create();
            request.OwnerFiberId = root.Fiber().Id;
            request.Email = email;
            request.Type = type;

            NetClient2Main_GetEmailCode response = await CallNetClientAsync<NetClient2Main_GetEmailCode>(root, sender, request);
            if (response == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            using (response)
            {
                if (response.Error != ErrorCode.ERR_Success)
                {
                    return AccountOpResult.Fail(response.Error, response.Message, response.Cooldown);
                }

                return AccountOpResult.SuccessWithCooldown(response.Cooldown);
            }
        }

        public static async ETTask<AccountOpResult> RegisterAsync(Scene root, string email, string code, string password)
        {
            EntityRef<Scene> rootRef = root;
            ClientSenderComponent sender = await EnsureNetClientAsync(root);
            root = rootRef;
            if (root == null || sender == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            Main2NetClient_Register request = Main2NetClient_Register.Create();
            request.OwnerFiberId = root.Fiber().Id;
            request.Email = email;
            request.Code = code;
            request.Password = password;

            NetClient2Main_Register response = await CallNetClientAsync<NetClient2Main_Register>(root, sender, request);
            if (response == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            using (response)
            {
                if (response.Error != ErrorCode.ERR_Success)
                {
                    return AccountOpResult.Fail(response.Error, response.Message);
                }

                return AccountOpResult.Success(0);
            }
        }

        public static async ETTask<AccountOpResult> ResetPasswordAsync(Scene root, string email, string code, string password)
        {
            EntityRef<Scene> rootRef = root;
            ClientSenderComponent sender = await EnsureNetClientAsync(root);
            root = rootRef;
            if (root == null || sender == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            Main2NetClient_ResetPassword request = Main2NetClient_ResetPassword.Create();
            request.OwnerFiberId = root.Fiber().Id;
            request.Email = email;
            request.Code = code;
            request.Password = password;

            NetClient2Main_ResetPassword response = await CallNetClientAsync<NetClient2Main_ResetPassword>(root, sender, request);
            if (response == null)
            {
                return AccountOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "连接服务器失败，请稍后再试");
            }

            using (response)
            {
                if (response.Error != ErrorCode.ERR_Success)
                {
                    return AccountOpResult.Fail(response.Error, response.Message);
                }

                return AccountOpResult.Success(0);
            }
        }

        private static async ETTask<TResponse> CallNetClientAsync<TResponse>(Scene root, ClientSenderComponent sender, IRequest request)
                where TResponse : class, IResponse
        {
            return await root.GetComponent<ProcessInnerSender>().Call(sender.netClientActorId, request, false) as TResponse;
        }

        public static async ETTask<ClientSenderComponent> EnsureNetClientAsync(Scene root)
        {
            ClientSenderComponent sender = root.GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                sender = root.AddComponent<ClientSenderComponent>();
            }

            if (sender.fiberId != 0)
            {
                return sender;
            }

            EntityRef<ClientSenderComponent> senderRef = sender;
            EntityRef<Scene> rootRef = root;

            int fiberId = await FiberManager.Instance.Create(SchedulerType.ThreadPool, 0, SceneType.NetClient, "");

            sender = senderRef;
            root = rootRef;
            if (sender == null || root == null)
            {
                return null;
            }

            sender.fiberId = fiberId;
            sender.netClientActorId = new ActorId(root.Fiber().Process, sender.fiberId);
            return sender;
        }
    }
}
