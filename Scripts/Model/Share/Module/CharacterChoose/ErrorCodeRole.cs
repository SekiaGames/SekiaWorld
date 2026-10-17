namespace ET
{
    /// <summary>
    /// 角色存档（创角 / 删角 / 掉线回写）的错误码（200131-200139）。
    /// 全部 &gt; 200000 ⇒ 属"不抛异常"语义：服务端把它们写进 response.Error/Message，
    /// 客户端直接读出来显示，不会变成 RpcException（≤200000 的码会被 Session.Call 抛出来）。
    /// <para>
    /// 放 Model/Share 而不是各端一份：本仓源码只有一份，客户端与服务端共用这一处定义。
    /// </para>
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>创角失败：这个存档位上已经有角色了</summary>
        public const int ERR_RoleSlotTaken = 200131;

        /// <summary>删角失败：这个存档位上没有角色</summary>
        public const int ERR_RoleNotFound = 200132;
    }
}
