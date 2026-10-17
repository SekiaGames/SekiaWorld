using System;


namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_LoginGateHandler : MessageSessionHandler<C2G_LoginGate, G2C_LoginGate>
    {
        protected override async ETTask Run(Session session, C2G_LoginGate request, G2C_LoginGate response)
        {
            Scene root = session.Root();
            string account = root.GetComponent<GateSessionKeyComponent>().Get(request.Key);
            if (account == null)
            {
                response.Error = ErrorCore.ERR_ConnectGateKeyError;
                response.Message = "Gate key验证失败!";
                return;
            }
            
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            Player player = playerComponent.GetByAccount(account);
            if (player == null)
            {
                player = playerComponent.AddChild<Player, string>(account);
                playerComponent.Add(player);
                PlayerSessionComponent playerSessionComponent = player.AddComponent<PlayerSessionComponent>();

                session.AddComponent<SessionPlayerComponent>().Player = player;
                playerSessionComponent.Session = session;
            }
            else
            {
                PlayerSessionComponent playerSessionComponent = player.GetComponent<PlayerSessionComponent>();
                playerSessionComponent.Session = session;
                // ⚠️ 重连分支也必须挂 SessionPlayerComponent：掉线（心跳超时）回写角色位置就是靠它
                //    的 Destroy 触发的（见 SessionPlayerComponentSystem）。原来只在"新建 Player"分支挂，
                //    老玩家重连后掉线就没有这个钩子 —— 表现是掉线日志/回写一条都不出。
                session.AddComponent<SessionPlayerComponent>().Player = player;
            }

            response.PlayerId = player.Id;

            // 登录 Gate 成功 = "拿到 key"了 ⇒ 顺手**回问 RealM 要这个账号的角色列表**，随登录响应一起给客户端
            // （客户端选角界面直接用它填槽）。⚠️ 只有 RealM 读写数据库：Gate 不碰 Mongo，只做中转。
            // 取列表失败**不挡登录**：选角界面会显示空槽，创角/删角时再报错。
            try
            {
                G2R_GetRoles g2RGetRoles = G2R_GetRoles.Create();
                g2RGetRoles.Account = account;
                R2G_GetRoles r2GGetRoles = (R2G_GetRoles) await root.GetComponent<MessageSender>().Call(
                        RealmHelper.GetRealm(root.Zone()).ActorId, g2RGetRoles);
                response.Roles.AddRange(r2GGetRoles.Roles);
                Log.Info($"[角色存档] Gate 拿到 key 后回问 RealM：账号 {account} 共 {r2GGetRoles.Roles.Count} 个角色");
            }
            catch (Exception e)
            {
                Log.Error($"[角色存档] 回问 RealM 取角色列表失败（登录继续，选角界面会显示空槽）：{e}");
            }

            await ETTask.CompletedTask;
        }
    }
}