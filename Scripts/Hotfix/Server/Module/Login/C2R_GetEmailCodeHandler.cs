namespace ET.Server
{
    /// <summary>
    /// 发邮箱验证码：注册与重置密码共用的第一步，靠 <see cref="C2R_GetEmailCode.Type"/> 区分用途 ——
    /// 注册要求邮箱未注册，重置密码要求邮箱已注册；用途不符直接拒，省得白发一条码。
    /// 配好 <see cref="SmtpSettings"/> 就走 SMTP 真发邮件，没配则验证码只写服务端日志
    /// （Logs/*.Info.log，搜 [MailCode]），见 <see cref="MailSender"/>；
    /// 冷却/有效期/失败次数见 <see cref="MailConfig"/>。
    /// </summary>
    [MessageSessionHandler(SceneType.Realm)]
    public class C2R_GetEmailCodeHandler : MessageSessionHandler<C2R_GetEmailCode, R2C_GetEmailCode>
    {
        protected override async ETTask Run(Session session, C2R_GetEmailCode request, R2C_GetEmailCode response)
        {
            string email = MailAddressValidator.Normalize(request.Email);
            if (!MailAddressValidator.IsValid(email))
            {
                response.Error = ErrorCode.ERR_EmailInvalid;
                response.Message = "邮箱格式不正确";
                return;
            }

            // 0/未知一律当"注册"（加 Type 字段之前的行为），见 EmailCodeType
            bool isResetPassword = EmailCodeType.IsResetPassword(request.Type);

            Scene scene = session.Scene();
            EntityRef<Scene> sceneRef = scene;
            long now = TimeInfo.Instance.ClientNow();

            // 用途与邮箱状态必须对得上：注册码只发给没注册过的邮箱，重置码只发给已注册的邮箱
            Account account = await AccountRepository.FindByEmail(scene, email);
            scene = sceneRef;
            if (isResetPassword)
            {
                if (account == null)
                {
                    response.Error = ErrorCode.ERR_EmailNotRegistered;
                    response.Message = "该邮箱未注册，请先注册";
                    return;
                }
            }
            else if (account != null)
            {
                response.Error = ErrorCode.ERR_EmailAlreadyRegistered;
                response.Message = "该邮箱已注册，请直接登录";
                return;
            }

            // 组件在 await 之后才取：跨 await 不直接引用 Entity 变量（分析器要求，见 AwaitEntityAnalyzer）
            EmailCodeComponent emailCodeComponent = EmailCodeHelper.GetOrAdd(scene);
            int cooldown = emailCodeComponent.GetCooldownRemaining(email, now);
            if (cooldown > 0)
            {
                response.Error = ErrorCode.ERR_EmailCodeCooldown;
                response.Message = $"发送太频繁，请 {cooldown} 秒后再试";
                response.Cooldown = cooldown;
                return;
            }

            string code = RandomGenerator.RandomNumber(0, 999999).ToString("D6");
            emailCodeComponent.SetCode(email, code, now);

            // 发信要走"后台线程 + 投回纤程"（MailKit 是阻塞 API），因此这里会真 await。
            // 跨 await 不直接引用 Entity（分析器要求，见 AwaitEntityAnalyzer）：Fiber 不是 Entity 可以持有，
            // 组件则用 EntityRef 在 await 之后再取回来
            Fiber fiber = scene.Fiber();
            EntityRef<EmailCodeComponent> emailCodeRef = emailCodeComponent;

            bool sent = await MailSender.SendEmailCode(fiber, email, code, isResetPassword);
            if (!sent)
            {
                // 发信失败就把刚写进去的码撤掉：留着等于把玩家锁在冷却里 —— 收不到码，60 秒内还不让重发
                EmailCodeComponent codeComponent = emailCodeRef;
                if (codeComponent != null)
                {
                    codeComponent.RemoveCode(email);
                }

                response.Error = ErrorCode.ERR_SendEmailFail;
                response.Message = "验证码发送失败，请稍后重试";
                return;
            }

            response.Cooldown = MailConfig.CodeCooldownSeconds;
        }
    }
}
