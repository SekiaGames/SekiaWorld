using System;
using System.IO;
using System.Text.Json;

namespace ET.Server
{
    /// <summary>
    /// SMTP 发信参数，对应 .FrameWork/Config/Mail/Smtp.json 的字段。
    /// [EnableClass]：Model 程序集默认只许放 Entity，这个是纯配置载体，按分析器的要求显式放行。
    /// </summary>
    [EnableClass]
    public sealed class SmtpConfig
    {
        /// <summary>是否真的走 SMTP 发信。false = 验证码只写服务端日志（开发期默认）</summary>
        public bool Enabled { get; set; }

        /// <summary>SMTP 服务器，如 smtp.qiye.aliyun.com（阿里云企业邮箱）/ smtp.qq.com / smtp.163.com</summary>
        public string Host { get; set; }

        /// <summary>端口：465 = 隐式 TLS，587 = STARTTLS（两种都由 MailKit 自动识别，不用另配开关）</summary>
        public int Port { get; set; } = 465;

        /// <summary>登录名，通常就是发件邮箱</summary>
        public string User { get; set; }

        /// <summary>
        /// 口令。QQ/163 要填后台开出来的"授权码"；阿里云企业邮箱填登录密码，
        /// 或域管开了「强制启用安全密码」时网页端生成的 16 位「三方客户端安全密码」。填错都是认证失败。
        /// </summary>
        public string Password { get; set; }

        /// <summary>发件人地址。留空则用 <see cref="User"/>（多数邮箱要求发件人 = 认证账号）</summary>
        public string From { get; set; }

        /// <summary>发件人显示名</summary>
        public string FromDisplayName { get; set; } = "SekiaWorld";

        /// <summary>连接/收发的超时秒数</summary>
        public int TimeoutSeconds { get; set; } = 20;
    }

    /// <summary>
    /// SMTP 配置的读取入口，读 <see cref="FilePath"/>（服务端工作目录是 Bin，即仓库根 Config/Mail/）。
    /// <list type="bullet">
    /// <item>为什么不做成配表：<b>口令是密钥</b>，不能进 Excel、更不能进仓库。配置文件只留本机并 gitignore，
    ///       由使用者自己维护 —— 仓库里<b>不放模板</b>，缺文件就走下面的降级。</item>
    /// <item>环境变量优先于文件：容器/CI 里可以只注入口令、配置文件里留空。变量名见 <see cref="ApplyEnvironmentOverrides"/>。</item>
    /// <item>文件不存在、解析失败或字段不全 = <b>降级成"验证码只写日志"</b>，不会让服务端起不来 ——
    ///       发信配错是运维问题，不该演变成登录功能整体不可用。</item>
    /// <item>每次取用都比对文件修改时间，改了配置不用重启服务端（发信频率很低，stat 一次的成本可忽略）。</item>
    /// </list>
    /// </summary>
    public static class SmtpSettings
    {
        /// <summary>配置文件名。只留本机、已 gitignore，由使用者自己维护（仓库里不放模板）</summary>
        public const string FileName = "Smtp.json";

        /// <summary>
        /// 配置目录（相对进程工作目录）。两个进程的工作目录差一层：
        /// 服务端从 .FrameWork/Bin 启动（".." 即框架层），客户端跑在 Godot 进程里、工作目录是仓库根
        /// （".FrameWork" 才是框架层）⇒ 两个候选都试，取文件实际存在的那个。
        /// </summary>
        [StaticField]
        public static readonly string ConfigDir = ResolveConfigDir();

        /// <summary>实际生效的配置文件（含口令，已 gitignore，不进仓库）</summary>
        [StaticField]
        public static readonly string FilePath = ConfigDir + "/" + FileName;

        private static string ResolveConfigDir()
        {
            string[] candidates = { "../Config/Mail", ".FrameWork/Config/Mail" };
            foreach (string candidate in candidates)
            {
                if (File.Exists(Path.Combine(candidate, FileName)))
                {
                    return candidate;
                }
            }

            // 都没有 ⇒ 按服务端那份返回，后面的「缺文件降级」会把它降成「验证码只写日志」
            return candidates[0];
        }

        private const string EnvPrefix = "SEKIA_MAIL_";

        // 三项静态缓存都要 [StaticField]（分析器要求显式标注静态字段）。进程级单例，故用锁而不是纤程锁：
        // 取用点只有发码那一条路（纤程内），加锁纯粹是防以后被别处并发调用
        [StaticField]
        private static readonly object SyncRoot = new();

        [StaticField]
        private static SmtpConfig current;

        [StaticField]
        private static DateTime lastWriteTimeUtc;

        /// <summary>当前生效的配置（首次取用时加载，之后按文件修改时间热重载）</summary>
        public static SmtpConfig Current
        {
            get
            {
                lock (SyncRoot)
                {
                    DateTime writeTime = GetLastWriteTimeUtc();

                    // 文件不存在时 GetLastWriteTimeUtc 恒为 MinValue，两次相等，所以"缺文件"不会反复重载刷日志
                    if (current == null || writeTime != lastWriteTimeUtc)
                    {
                        lastWriteTimeUtc = writeTime;
                        current = Load();
                        LogEffectiveConfig(current);
                    }

                    return current;
                }
            }
        }

        /// <summary>丢掉缓存，下次取用时重新读（改完配置想立刻生效时用）</summary>
        public static void Reload()
        {
            lock (SyncRoot)
            {
                current = null;
                lastWriteTimeUtc = default;
            }
        }

        private static DateTime GetLastWriteTimeUtc()
        {
            try
            {
                return File.Exists(FilePath)? File.GetLastWriteTimeUtc(FilePath) : DateTime.MinValue;
            }
            catch (Exception)
            {
                // 文件被占用/权限异常时按"没有文件"处理，不能让配置读取把发码流程打断
                return DateTime.MinValue;
            }
        }

        private static SmtpConfig Load()
        {
            SmtpConfig config = null;

            if (File.Exists(FilePath))
            {
                try
                {
                    JsonSerializerOptions options = new()
                    {
                        PropertyNameCaseInsensitive = true,
                        // 允许注释和尾逗号：配置文件里要能写"授权码去哪开"这类说明
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    };
                    config = JsonSerializer.Deserialize<SmtpConfig>(File.ReadAllText(FilePath), options);
                }
                catch (Exception e)
                {
                    Log.Error($"解析 {FilePath} 失败，已降级为「验证码只写日志」：{e.Message}");
                }
            }

            config ??= new SmtpConfig();

            ApplyEnvironmentOverrides(config);

            if (string.IsNullOrWhiteSpace(config.From))
            {
                config.From = config.User;
            }

            if (!config.Enabled)
            {
                return config;
            }

            string reason = Validate(config);
            if (reason != null)
            {
                Log.Error($"SMTP 配置不完整（{reason}），已降级为「验证码只写日志」；检查 {ConfigDir}/{FileName}");
                config.Enabled = false;
            }

            return config;
        }

        /// <summary>返回第一条不合格的原因；全部合格返回 null</summary>
        private static string Validate(SmtpConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Host))
            {
                return "Host 为空";
            }

            if (config.Port <= 0 || config.Port > 65535)
            {
                return $"Port 非法（{config.Port}）";
            }

            if (string.IsNullOrWhiteSpace(config.User))
            {
                return "User 为空";
            }

            if (string.IsNullOrWhiteSpace(config.Password))
            {
                return "Password 为空（QQ/163 填授权码；阿里云企业邮箱填登录密码，或域管强制时的 16 位三方客户端安全密码）";
            }

            if (string.IsNullOrWhiteSpace(config.From))
            {
                return "From 为空";
            }

            if (config.TimeoutSeconds <= 0)
            {
                // 超时配成 0/负数会让 MailKit 直接抛参数异常，兜一个默认值比发不出信强
                config.TimeoutSeconds = 20;
            }

            return null;
        }

        /// <summary>
        /// 环境变量覆盖文件配置。变量名：SEKIA_MAIL_ENABLED / HOST / PORT / USER / PASSWORD / FROM / DISPLAYNAME。
        /// 空字符串视为"没设置"，好让配置文件里留空的项继续生效。
        /// </summary>
        private static void ApplyEnvironmentOverrides(SmtpConfig config)
        {
            config.Enabled = ReadBool("ENABLED") ?? config.Enabled;
            config.Host = ReadString("HOST") ?? config.Host;
            config.Port = ReadInt("PORT") ?? config.Port;
            config.User = ReadString("USER") ?? config.User;
            config.Password = ReadString("PASSWORD") ?? config.Password;
            config.From = ReadString("FROM") ?? config.From;
            config.FromDisplayName = ReadString("DISPLAYNAME") ?? config.FromDisplayName;
        }

        private static string ReadString(string name)
        {
            string value = Environment.GetEnvironmentVariable(EnvPrefix + name);
            return string.IsNullOrWhiteSpace(value)? null : value;
        }

        private static bool? ReadBool(string name)
        {
            string value = ReadString(name);
            if (value == null)
            {
                return null;
            }

            return value is "1" or "true" or "True" or "TRUE" or "yes" or "on";
        }

        private static int? ReadInt(string name)
        {
            string value = ReadString(name);
            return int.TryParse(value, out int port)? port : null;
        }

        /// <summary>日志里只报"启没启用 + 连哪台服务器"，口令一个字都不出现</summary>
        private static void LogEffectiveConfig(SmtpConfig config)
        {
            if (!config.Enabled)
            {
                Log.Info("SMTP 未启用，邮箱验证码只写服务端日志（搜 [MailCode]）");
                return;
            }

            Log.Info($"SMTP 已启用：{config.Host}:{config.Port}，发件人 {config.FromDisplayName} <{config.From}>");
        }
    }
}
