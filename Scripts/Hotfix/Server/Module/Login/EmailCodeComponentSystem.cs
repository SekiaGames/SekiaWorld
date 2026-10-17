namespace ET.Server
{
    [EntitySystemOf(typeof(EmailCodeComponent))]
    [FriendOf(typeof(EmailCodeComponent))]
    public static partial class EmailCodeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EmailCodeComponent self)
        {
        }

        /// <summary>还要等多少秒才能再发码；0 = 现在就能发</summary>
        public static int GetCooldownRemaining(this EmailCodeComponent self, string email, long now)
        {
            if (!self.records.TryGetValue(email, out EmailCodeRecord record))
            {
                return 0;
            }

            long remainMs = record.NextSendTime - now;
            if (remainMs <= 0)
            {
                return 0;
            }

            // 向上取整：不然"还剩 0.4 秒"会显示成 0 秒、却依然被拒，玩家看着莫名其妙
            return (int)((remainMs + 999) / 1000);
        }

        /// <summary>写入（覆盖）一条验证码，同时重置冷却与失败次数</summary>
        public static void SetCode(this EmailCodeComponent self, string email, string code, long now)
        {
            self.records[email] = new EmailCodeRecord
            {
                Code = code,
                ExpireTime = now + MailConfig.CodeExpireSeconds * 1000L,
                NextSendTime = now + MailConfig.CodeCooldownSeconds * 1000L,
                TryCount = 0,
            };
        }

        /// <summary>
        /// 撤掉一条验证码记录（连同冷却）。
        /// 用途只有一个：发信失败时把刚写进去的码抹掉 —— 否则玩家既收不到码，60 秒冷却里还不让重发，
        /// 等于被一封发不出去的邮件锁住。
        /// </summary>
        public static void RemoveCode(this EmailCodeComponent self, string email)
        {
            self.records.Remove(email);
        }

        /// <summary>
        /// 校验验证码：通过则消费掉（一次性，用完即删），不通过返回错误码。
        /// 失败累计到上限就作废整条记录，逼客户端重新发码 —— 否则 6 位码可以被慢慢暴力猜。
        /// </summary>
        public static int VerifyCode(this EmailCodeComponent self, string email, string code, long now)
        {
            if (!self.records.TryGetValue(email, out EmailCodeRecord record))
            {
                return ErrorCode.ERR_EmailCodeIncorrect;
            }

            if (now > record.ExpireTime)
            {
                self.records.Remove(email);
                return ErrorCode.ERR_EmailCodeIncorrect;
            }

            if (record.Code != code)
            {
                record.TryCount++;
                if (record.TryCount >= MailConfig.CodeMaxTryCount)
                {
                    self.records.Remove(email);
                }
                else
                {
                    self.records[email] = record;
                }

                return ErrorCode.ERR_EmailCodeIncorrect;
            }

            self.records.Remove(email);
            return ErrorCode.ERR_Success;
        }
    }
}
