using System;

namespace ET.Server
{
    [Invoke((long)SceneType.Gate)]
    public class NetComponentOnReadInvoker_Gate: AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            HandleAsync(args).Coroutine();
        }

        private async ETTask HandleAsync(NetComponentOnRead args)
        {
            Session session = args.Session;
            object message = args.Message;
            Scene root = args.Session.Root();
            // 根据消息接口判断消息走向：
            //   ISessionMessage / IRequest —— 交给 session 分发（响应由 session 直接回给客户端）
            //   其余 C2M_* 业务消息 —— 投递给本 Gate 场景里该玩家自己的 Unit，由 Unit 的
            //   OrderedMessage 邮箱保证顺序（Gate 兼任 Map，Unit 就在本场景，不再跨进程转发）
            switch (message)
            {
                case ISessionMessage:
                case IRequest:
                {
                    MessageSessionDispatcher.Instance.Handle(session, message);
                    break;
                }
                case IMessage:
                {
                    long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                    Unit unit = root.GetComponent<UnitComponent>().Get(unitId);
                    if (unit == null)
                    {
                        Log.Error($"not found unit: {unitId} {message}");
                        break;
                    }

                    unit.GetComponent<MailBoxComponent>().Add(default, (MessageObject)message);
                    break;
                }
                default:
                {
                    throw new Exception($"not found handler: {message}");
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
