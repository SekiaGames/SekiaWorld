namespace ET.Server
{
    /// <summary>
    /// 重置密码：邮箱 + 邮箱验证码 + 新密码，校验通过才改库。
    /// 账号体系约定 —— 和注册同源：必须先过邮箱验证码这一关，且验证码必须按"重置密码"用途发
    /// （见 <see cref="C2R_GetEmailCodeHandler"/>：重置用途要求邮箱已注册）。
    /// 验证码是一次性的，校验通过即消费；改库是单文档原子更新（整组换 salt + hash）。
    /// **改密码 = 吊销该账号的全部免密凭据**：旧凭据一律作废，那几台设备下次得重新输密码。
    /// </summary>
    [MessageSessionHandler(SceneType.Realm)]
    public class C2R_ResetPasswordHandler : MessageSessionHandler<C2R_ResetPassword, R2C_ResetPassword>
    {
        protected override async ETTask Run(Session session, C2R_ResetPassword request, R2C_ResetPassword response)
        {
            string email = MailAddressValidator.Normalize(request.Email);
            if (!MailAddressValidator.IsValid(email))
            {
                response.Error = ErrorCode.ERR_EmailInvalid;
                response.Message = "邮箱格式不正确";
                return;
            }

            string passwordError = PasswordValidator.Check(request.Password);
            if (passwordError != null)
            {
                response.Error = ErrorCode.ERR_PasswordInvalid;
                response.Message = passwordError;
                return;
            }

            Scene scene = session.Scene();
            EntityRef<Scene> sceneRef = scene;
            long now = TimeInfo.Instance.ClientNow();

            // 先确认账号存在：没注册过的邮箱无从重置（提示与注册发码那侧同口径，见 ErrorCode.ERR_EmailNotRegistered）
            Account account = await AccountRepository.FindByEmail(scene, email);
            scene = sceneRef;
            if (account == null)
            {
                response.Error = ErrorCode.ERR_EmailNotRegistered;
                response.Message = "该邮箱未注册，请先注册";
                return;
            }

            int revokedTokens = account.Tokens?.Count ?? 0;

            // 校验验证码（通过才继续）。组件在 await 之后取：跨 await 不直接引用 Entity 变量
            EmailCodeComponent emailCodeComponent = EmailCodeHelper.GetOrAdd(scene);
            int codeError = emailCodeComponent.VerifyCode(email, request.Code, now);
            if (codeError != ErrorCode.ERR_Success)
            {
                response.Error = codeError;
                response.Message = "验证码错误或已过期，请重新获取";
                return;
            }

            PasswordHasher.Hash(request.Password, out string salt, out string passwordHash);
            bool updated = await AccountRepository.ResetPassword(scene, email, salt, passwordHash);
            if (!updated)
            {
                response.Error = ErrorCode.ERR_EmailNotRegistered;
                response.Message = "该邮箱未注册，请先注册";
                return;
            }

            Log.Info($"[账号] 重置密码成功: {email}，已吊销 {revokedTokens} 条免密凭据");
        }
    }
}
