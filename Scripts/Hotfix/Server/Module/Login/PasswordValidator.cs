namespace ET.Server
{
    /// <summary>
    /// 注册与重置密码共用的密码校验，避免两处各写一遍、日后改规则漏掉一处。
    /// 口径与 <see cref="MailConfig"/> 里的长度上下限绑定（加长/放宽只改配置）。
    /// </summary>
    public static class PasswordValidator
    {
        /// <summary>校验密码；通过返回 null，不通过返回给玩家看的提示</summary>
        public static string Check(string password)
        {
            if (string.IsNullOrEmpty(password) ||
                    password.Length < MailConfig.PasswordMinLength ||
                    password.Length > MailConfig.PasswordMaxLength)
            {
                return $"密码长度需在 {MailConfig.PasswordMinLength}-{MailConfig.PasswordMaxLength} 位之间";
            }

            return null;
        }
    }
}
