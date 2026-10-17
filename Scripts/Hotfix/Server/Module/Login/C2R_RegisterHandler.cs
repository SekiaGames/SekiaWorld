namespace ET.Server
{
    /// <summary>
    /// 邮箱注册：注册流程第二步。
    /// 账号体系硬约定 —— 必须先通过邮箱验证码校验才允许注册，校验通过才写库。
    /// 验证码是一次性的，校验通过即消费；写库用 _id 主键判重，并发下也不会写出两条同邮箱。
    /// </summary>
    [MessageSessionHandler(SceneType.Realm)]
    public class C2R_RegisterHandler : MessageSessionHandler<C2R_Register, R2C_Register>
    {
        protected override async ETTask Run(Session session, C2R_Register request, R2C_Register response)
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

            // 先查重：已注册就直接拒，别白消费玩家手上那条验证码
            Account exist = await AccountRepository.FindByEmail(scene, email);
            scene = sceneRef;
            if (exist != null)
            {
                response.Error = ErrorCode.ERR_EmailAlreadyRegistered;
                response.Message = "该邮箱已注册，请直接登录";
                return;
            }

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
            Account account = new Account
            {
                Email = email,
                Salt = salt,
                PasswordHash = passwordHash,
                CreatedAt = now,
                LastLogin = 0,
            };

            bool created = await AccountRepository.TryCreate(scene, account);
            if (!created)
            {
                // 罕见路径：查重通过后、插入前被另一个并发注册抢先。主键冲突说明这个邮箱已经被占用了
                response.Error = ErrorCode.ERR_EmailAlreadyRegistered;
                response.Message = "该邮箱已注册，请直接登录";
                return;
            }

            Log.Info($"[账号] 注册成功: {email}");
        }
    }
}
