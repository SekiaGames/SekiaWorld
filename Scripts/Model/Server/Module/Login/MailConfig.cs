namespace ET.Server
{
    /// <summary>
    /// 邮件与验证码策略。
    /// <para>
    /// **SMTP 连接参数不在这个文件**：带口令的东西不能硬编码进代码、更不能进仓库，
    /// 已挪到 <see cref="SmtpSettings"/>（读 Config/Mail/Smtp.json，由使用者自己维护）。
    /// 这里只留"策略"——冷却、有效期、长度这些与部署环境无关的常量。
    /// </para>
    /// <para>
    /// 注意：这些值都只该出现在服务端，别挪进 @c 表或客户端。
    /// </para>
    /// </summary>
    public static class MailConfig
    {
        /// <summary>同一邮箱两次发码的最小间隔（秒）</summary>
        public const int CodeCooldownSeconds = 60;

        /// <summary>验证码有效期（秒）</summary>
        public const int CodeExpireSeconds = 5 * 60;

        /// <summary>同一个验证码最多允许校验失败几次，超过即作废（必须重新发码，避免被暴力猜）</summary>
        public const int CodeMaxTryCount = 5;

        /// <summary>密码长度下限</summary>
        public const int PasswordMinLength = 6;

        /// <summary>密码长度上限（先挡住超长输入，免得白算 PBKDF2）</summary>
        public const int PasswordMaxLength = 64;

        /// <summary>邮箱长度上限</summary>
        public const int EmailMaxLength = 128;
    }
}
