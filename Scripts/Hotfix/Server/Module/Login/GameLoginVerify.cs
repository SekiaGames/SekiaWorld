namespace ET.Server
{
    /// <summary>登录校验结果：Error 为 <see cref="ErrorCode.ERR_Success"/> 表示校验通过。</summary>
    public struct LoginVerifyResult
    {
        public int Error;

        public string Message;

        /// <summary>密码登录通过时新签发的免密凭据；免密登录通过时为 null（不刷新、不续期）</summary>
        public string Token;

        public static LoginVerifyResult Success(string token)
        {
            return new LoginVerifyResult { Error = ErrorCode.ERR_Success, Message = string.Empty, Token = token };
        }

        public static LoginVerifyResult Fail(int error, string message)
        {
            return new LoginVerifyResult { Error = error, Message = message };
        }
    }

    /// <summary>
    /// 登录校验：**邮箱 + 密码**，或 **邮箱 + 免密凭据**（二选一；凭据非空时忽略密码）。
    /// 两种方式都要带**设备码**：凭据与设备一对一，签发与校验都拿它配对。
    /// 由 Realm 登录处理调用（Scripts/Hotfix/Server/Module/Login/C2R_LoginHandler.cs）。
    /// <para>
    /// 密码校验通过时**顺手签发**新凭据并随结果返回：签发要跨 await 读写库，放在这里一次做完，
    /// 调用方就不必再自己持着 Scene 跨 await 了。
    /// </para>
    /// </summary>
    public static class GameLoginVerify
    {
        public static async ETTask<LoginVerifyResult> VerifyAsync(Scene scene, string email, string password, string token, string deviceCode)
        {
            string normalizedEmail = MailAddressValidator.Normalize(email);
            if (!MailAddressValidator.IsValid(normalizedEmail))
            {
                return LoginVerifyResult.Fail(ErrorCode.ERR_EmailInvalid, "邮箱格式不正确");
            }

            // 设备码是签发与校验的必需项：没有它就没法做"凭据与设备一对一"，宁可拒掉
            if (string.IsNullOrEmpty(deviceCode))
            {
                return LoginVerifyResult.Fail(ErrorCode.ERR_DeviceInvalid, "设备标识无效，请重启客户端后再试");
            }

            // 跨 await 不许直接用 Entity 变量，按框架惯例用 EntityRef 取回
            EntityRef<Scene> sceneRef = scene;
            Account account = await AccountRepository.FindByEmail(scene, normalizedEmail);
            scene = sceneRef;
            if (scene == null)
            {
                return LoginVerifyResult.Fail(ErrorCode.ERR_ConnectServerFail, "服务器繁忙，请稍后再试");
            }

            // 带凭据就是免密登录：只认凭据，不再看密码（密码为空也不算错）
            if (!string.IsNullOrEmpty(token))
            {
                if (!HasValidToken(account, token, deviceCode))
                {
                    return LoginVerifyResult.Fail(ErrorCode.ERR_TokenInvalid, "登录状态已失效，请重新输入密码");
                }

                await AccountRepository.UpdateLastLogin(scene, normalizedEmail, TimeInfo.Instance.ClientNow());
                Log.Info($"[账号] 免密登录成功: {normalizedEmail}");
                return LoginVerifyResult.Success(null);
            }

            if (string.IsNullOrEmpty(password))
            {
                return LoginVerifyResult.Fail(ErrorCode.ERR_EmailOrPasswordWrong, "邮箱或密码错误");
            }

            // "没这个账号"和"密码错"对外回同一句话：别让人拿登录接口探测哪些邮箱注册过
            if (account == null || !PasswordHasher.Verify(password, account.Salt, account.PasswordHash))
            {
                return LoginVerifyResult.Fail(ErrorCode.ERR_EmailOrPasswordWrong, "邮箱或密码错误");
            }

            await AccountRepository.UpdateLastLogin(scene, normalizedEmail, TimeInfo.Instance.ClientNow());

            scene = sceneRef;
            if (scene == null)
            {
                return LoginVerifyResult.Fail(ErrorCode.ERR_ConnectServerFail, "服务器繁忙，请稍后再试");
            }

            string newToken = await AccountRepository.IssueToken(scene, normalizedEmail, deviceCode);
            Log.Info($"[账号] 登录成功: {normalizedEmail}");
            return LoginVerifyResult.Success(newToken);
        }

        /// <summary>
        /// 凭据与设备必须**成对**命中：凭据在表里、但设备对不上（被拷到别的机器）同样算失效。
        /// ⚠️ 老账号文档没有 tokens 字段 ⇒ <c>account.Tokens</c> 为 null
        /// </summary>
        private static bool HasValidToken(Account account, string token, string deviceCode)
        {
            if (account?.Tokens == null)
            {
                return false;
            }

            foreach (AccountToken accountToken in account.Tokens)
            {
                if (accountToken.Token == token && accountToken.Device == deviceCode)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
