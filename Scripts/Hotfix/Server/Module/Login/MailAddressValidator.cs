namespace ET.Server
{
    /// <summary>
    /// 邮箱格式与规范化。
    /// 只做基本形状校验（本地部分@域名、域名带点），不追求 RFC 5322 全量正则 —— 那是无底洞，
    /// 而且真正的"这个邮箱能不能用"由玩家能不能收到验证码决定。这里主要是挡住明显的手误与垃圾输入。
    /// </summary>
    public static class MailAddressValidator
    {
        /// <summary>统一小写去空格。账号主键、验证码索引、查库都必须用规范化后的值，否则 A@x.com 与 a@x.com 会变成两个账号。</summary>
        public static string Normalize(string email)
        {
            return email == null ? null : email.Trim().ToLowerInvariant();
        }

        public static bool IsValid(string email)
        {
            if (string.IsNullOrEmpty(email) || email.Length > MailConfig.EmailMaxLength)
            {
                return false;
            }

            if (email.Contains(' '))
            {
                return false;
            }

            // 必须恰好一个 @，且两边的部分都不为空
            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
            {
                return false;
            }

            string local = email[..at];
            string domain = email[(at + 1)..];
            if (local.Length == 0 || domain.Length == 0)
            {
                return false;
            }

            // 域名至少要有一个点，且点不能在开头或结尾（挡掉 a@.com / a@com. 这类）
            int dot = domain.IndexOf('.');
            return dot > 0 && dot != domain.Length - 1;
        }
    }
}
