namespace ET.Server
{
    /// <summary>
    /// Realm 登录处理：本作登录 = 邮箱 + 密码，所以这个 Handler 归游戏仓。
    /// 职责：先过 <see cref="GameLoginVerify"/>，通过后再按框架的方式分配 Gate、取登录 key 并关闭 Realm 连接。
    /// </summary>
    [MessageSessionHandler(SceneType.Realm)]
    public class C2R_LoginHandler : MessageSessionHandler<C2R_Login, R2C_Login>
    {
        protected override async ETTask Run(Session session, C2R_Login request, R2C_Login response)
        {
            Scene realmScene = session.Scene();

            EntityRef<Session> verifySessionRef = session;
            LoginVerifyResult verifyResult = await GameLoginVerify.VerifyAsync(
                realmScene, request.Email, request.Password, request.Token, request.DeviceCode);
            session = verifySessionRef;
            if (session == null)
            {
                return;
            }

            if (verifyResult.Error != ErrorCode.ERR_Success)
            {
                // > 200000 的错误码是"不抛异常"语义，客户端直接读 response.Error/Message 显示
                response.Error = verifyResult.Error;
                response.Message = verifyResult.Message;
                return;
            }

            // 随机分配一个Gate
            StartSceneConfig config = RealmGateAddressHelper.GetGate(session.Zone(), request.Email);
            Log.Debug($"gate address: {config}");

            // 向gate请求一个key,客户端可以拿着这个key连接gate
            R2G_GetLoginKey r2GGetLoginKey = R2G_GetLoginKey.Create();
            EntityRef<Session> sessionRef = session;
            r2GGetLoginKey.Account = request.Email;
            G2R_GetLoginKey g2RGetLoginKey = (G2R_GetLoginKey) await session.Fiber().Root.GetComponent<MessageSender>().Call(
                config.ActorId, r2GGetLoginKey);

            // 剥离软路由后，客户端直连 Gate —— 回给客户端的是"外网地址"（内网地址客户端未必可达）
            response.Address = config.OuterIPPort.ToString();
            response.Key = g2RGetLoginKey.Key;
            response.GateId = g2RGetLoginKey.GateId;
            response.Token = verifyResult.Token; // 密码登录新签发的免密凭据；免密登录时为 null

            session = sessionRef;
            if (session == null)
            {
                return;
            }
            CloseSession(session).Coroutine();
        }

        private async ETTask CloseSession(Session session)
        {
            EntityRef<Session> sessionRef = session;
            await session.Root().TimerComponent.WaitAsync(1000);
            session = sessionRef;
            session?.Dispose();
        }
    }
}
