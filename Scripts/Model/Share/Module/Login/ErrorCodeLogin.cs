namespace ET
{
    /// <summary>
    /// 账号/邮箱错误码（200101-200111），与 <c>ErrorCode.cs</c> 同属一个 partial 类。
    /// 全部 &gt; 200000 ⇒ 属"不抛异常"语义：服务端把它们写进 response.Error/Message，
    /// 客户端直接读出来显示，不会变成 RpcException。
    /// <para>
    /// 放 Model/Share 而不是各端一份：本仓源码只有一份，客户端与服务端共用这一处定义。
    /// </para>
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>邮箱格式不正确</summary>
        public const int ERR_EmailInvalid = 200101;

        /// <summary>密码长度不合规</summary>
        public const int ERR_PasswordInvalid = 200102;

        /// <summary>发验证码太频繁，还在冷却中</summary>
        public const int ERR_EmailCodeCooldown = 200103;

        /// <summary>邮箱验证码错误、已过期或尝试次数用尽</summary>
        public const int ERR_EmailCodeIncorrect = 200104;

        /// <summary>邮箱已注册</summary>
        public const int ERR_EmailAlreadyRegistered = 200105;

        /// <summary>邮箱或密码错误（刻意不区分"没注册"和"密码错"）</summary>
        public const int ERR_EmailOrPasswordWrong = 200106;

        /// <summary>验证码发送失败</summary>
        public const int ERR_SendEmailFail = 200107;

        /// <summary>连不上登录服（Realm 未启动/网络不通），客户端本地产生</summary>
        public const int ERR_ConnectServerFail = 200108;

        /// <summary>邮箱未注册（重置密码时用）</summary>
        public const int ERR_EmailNotRegistered = 200109;

        /// <summary>免密登录凭据失效（被吊销 / 换过密码 / 与设备对不上）—— 客户端应清掉本地存档、让玩家重新输密码</summary>
        public const int ERR_TokenInvalid = 200110;

        /// <summary>
        /// 设备码缺失或非法（客户端自报的安装级标识）。
        /// 与 <see cref="ERR_TokenInvalid"/> 分开：这种情况**不该**让客户端清掉本地存档（存档本身可能是好的）。
        /// </summary>
        public const int ERR_DeviceInvalid = 200111;
    }
}
