using System;
using System.Threading;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ET.Server
{
    /// <summary>
    /// 验证码发送。
    /// <para>
    /// 两种模式，由 <see cref="SmtpSettings.Current"/> 的 Enabled 决定：
    /// <list type="bullet">
    /// <item><b>未启用</b>（没配 Smtp.json，或配得不全）：只把验证码写服务端日志
    ///       （工作目录是 Bin，日志落在 ../Logs/*.Info.log，搜 [MailCode] 就能拿到码）。</item>
    /// <item><b>已启用</b>：走 SMTP 真发邮件，并把结果回给调用方 —— 发不出去要让玩家知道，
    ///       不能默默吞掉让他干等一封永远不来的邮件。</item>
    /// </list>
    /// </para>
    /// <para>
    /// 线程模型：MailKit 全是同步阻塞 API，直接在纤程里调会卡住整个 Realm。所以发信挪到后台线程，
    /// 结果经 <c>Fiber.ThreadSynchronizationContext</c> 投回纤程线程 —— 和框架自己的
    /// <c>FiberManager.Create</c> / <c>Remove</c> 是同一套路（那边用 TaskCompletionSource，
    /// 这里用 <c>ETTask&lt;string&gt;.Create(true)</c>），续体与后续实体操作仍在纤程线程上跑。
    /// </para>
    /// </summary>
    public static class MailSender
    {
        /// <summary>
        /// 发验证码。返回是否真的送达 SMTP 服务器（未启用 SMTP 时恒为 true —— 码已在日志里）。
        /// <para>
        /// <paramref name="isResetPassword"/> 只影响邮件文案与日志用途标注 ——
        /// 注册与重置是两套互斥的邮箱状态（未注册 / 已注册），同一个邮箱不会同时持有两种用途的码，
        /// 所以验证码本身不需要再记用途。
        /// </para>
        /// </summary>
        /// <param name="fiber">调用方所在纤程，用于把后台线程的结果投回来。必须是发起发码请求的那个 Realm 纤程。</param>
        public static async ETTask<bool> SendEmailCode(Fiber fiber, string email, string code, bool isResetPassword = false)
        {
            SmtpConfig config = SmtpSettings.Current;
            string purpose = isResetPassword ? "重置密码" : "注册";

            if (!config.Enabled)
            {
                Log.Info($"[MailCode] {email} 验证码={code} 用途={purpose}（{MailConfig.CodeExpireSeconds} 秒内有效；SMTP 未启用，只写日志）");
                return true;
            }

            string error = await SendAsync(fiber, config, email, code, isResetPassword);
            if (error != null)
            {
                Log.Error($"发往 {email} 的{purpose}验证码邮件发送失败：{error}");
                return false;
            }

            // 真发信时不记验证码本身：日志文件不该变成绕开邮箱验证的入口
            Log.Info($"{purpose}验证码邮件已发送至 {email}（{MailConfig.CodeExpireSeconds} 秒内有效）");
            return true;
        }

        /// <summary>
        /// 把发信扔到后台线程，返回 null 表示成功、否则是失败原因。
        /// 与框架 <c>FiberManager.Create</c> 同一套路：阻塞 I/O 在后台线程，
        /// 结果经纤程同步上下文投回，保证续体仍在纤程线程上执行。
        /// </summary>
        private static ETTask<string> SendAsync(Fiber fiber, SmtpConfig config, string to, string code, bool isResetPassword)
        {
            ETTask<string> task = ETTask<string>.Create(true);

            Thread thread = new(() =>
            {
                string error;
                try
                {
                    Send(config, to, code, isResetPassword);
                    error = null;
                }
                catch (Exception e)
                {
                    // 后台线程绝不能调 Log：Log 取的是全局 Fiber.Instance，从别的线程调会把这条日志
                    // 挂到当时正在跑的纤程上、甚至空引用。只把文本带回纤程线程再记。
                    error = e.Message;
                }

                fiber.ThreadSynchronizationContext.Post(() => task.SetResult(error));
            })
            {
                IsBackground = true,
                Name = "SendMail",
            };

            thread.Start();
            return task;
        }

        /// <summary>同步发信。只允许在后台线程调用（MailKit 是阻塞 API）</summary>
        private static void Send(SmtpConfig config, string to, string code, bool isResetPassword)
        {
            MimeMessage message = BuildMessage(config, to, code, isResetPassword);

            using SmtpClient client = new();
            client.Timeout = config.TimeoutSeconds * 1000;

            // Auto：465 走隐式 TLS，其余端口走 STARTTLS（服务器没广告 STARTTLS 时自动退回明文，
            // 本地假 SMTP 调试验证用得上）。不用自己按端口分支。
            client.Connect(config.Host, config.Port, SecureSocketOptions.Auto);

            try
            {
                client.Authenticate(config.User, config.Password);
                client.Send(message);
            }
            finally
            {
                // 认证/发信失败时也要把连接退干净；Disconnect 自己再出异常不该盖掉上面真正的失败原因
                try
                {
                    client.Disconnect(true);
                }
                catch (Exception)
                {
                    // 忽略：走到这里连接已经在失败路径上，Disconnect 只是尽力释放
                }
            }
        }

        private static MimeMessage BuildMessage(SmtpConfig config, string to, string code, bool isResetPassword)
        {
            string purpose = isResetPassword ? "重置密码" : "注册";
            string minutes = (MailConfig.CodeExpireSeconds / 60).ToString();

            MimeMessage message = new();
            message.From.Add(new MailboxAddress(config.FromDisplayName, config.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = $"【{config.FromDisplayName}】{purpose}验证码";

            // 中文主题/正文交给 MimeKit 编码，不用自己拼 base64；纯文本与 HTML 各一份，客户端挑一份渲染
            BodyBuilder body = new()
            {
                TextBody = $"你的{purpose}验证码是 {code}，{minutes} 分钟内有效。\n如非本人操作，请忽略本邮件。",
                HtmlBody = "<div style=\"font-family:sans-serif;font-size:14px;color:#222;line-height:1.7\">"
                        + $"<p>你的{purpose}验证码是：</p>"
                        + $"<p style=\"font-size:26px;font-weight:600;letter-spacing:4px;color:#1a5fb4\">{code}</p>"
                        + $"<p>{minutes} 分钟内有效。如非本人操作，请忽略本邮件。</p>"
                        + "</div>",
            };

            message.Body = body.ToMessageBody();
            return message;
        }
    }
}
