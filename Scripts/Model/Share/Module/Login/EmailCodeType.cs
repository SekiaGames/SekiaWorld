namespace ET
{
    /// <summary>
    /// 邮箱验证码的用途。客户端发码请求时带上，服务端据此决定"这个邮箱该不该收到码"：
    /// <see cref="Register"/> 要求邮箱**未**注册，<see cref="ResetPassword"/> 要求邮箱**已**注册。
    /// <para>
    /// 放 Model/Share：两端都要用（客户端界面挑用途、服务端据此校验邮箱状态），
    /// 本仓源码只有一份，所以不像参考工程那样要在客户端/服务端各放一份副本。
    /// </para>
    /// </summary>
    public static class EmailCodeType
    {
        /// <summary>注册：邮箱必须未注册</summary>
        public const int Register = 1;

        /// <summary>重置密码：邮箱必须已注册</summary>
        public const int ResetPassword = 2;

        /// <summary>把线上的值归一化：只有明确等于 <see cref="ResetPassword"/> 才算重置用途</summary>
        public static bool IsResetPassword(int type)
        {
            return type == ResetPassword;
        }
    }
}
