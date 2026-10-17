using System;
using System.Net;
using System.Text;

namespace ET.Server
{
    /// <summary>
    /// Realm 网页控制台：HttpListener 只绑 localhost，把原先交互式命令行（2026-10-01 已删）能做的事搬到网页上。
    /// 挂在 Realm 的 root Scene 上，所以只有 Realm 进程会起监听。端口见 Options.HttpConsolePort（0 = 关闭）。
    /// </summary>
    [EntitySystemOf(typeof (ConsoleComponent))]
    [FriendOf(typeof (ConsoleComponent))]
    [FriendOf(typeof (ModeContex))]
    public static partial class ConsoleComponentSystem
    {
        private const int MaxLogs = 100;

        private const string IndexHtml = """
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>ET 网页控制台</title>
<style>
  body { font-family: system-ui, "Microsoft YaHei", sans-serif; margin: 2rem auto; max-width: 960px; padding: 0 1rem; }
  button { font-size: 1rem; padding: .5rem 1.1rem; cursor: pointer; margin-right: .6rem; }
  .mem { margin-left: .8rem; color: #444; }
  table { border-collapse: collapse; width: 100%; margin-top: .6rem; }
  th, td { border: 1px solid #ddd; padding: .4rem .6rem; text-align: left; font-size: .92rem; }
  th { background: #f0f0f0; }
  td.time { white-space: nowrap; width: 7rem; }
  td.type { width: 9rem; }
</style>
</head>
<body>
<h3>ET 网页控制台</h3>
<p>
  <button onclick="run('R')">热更 dll</button>
  <button onclick="run('C')">重载全部配置</button>
  <span class="mem">内存占用：<b id="mem">-</b></span>
</p>
<h4>操作日志</h4>
<table>
  <thead><tr><th class="time">时间</th><th class="type">操作类型</th><th>操作结果</th></tr></thead>
  <tbody id="logs"><tr><td colspan="3">暂无</td></tr></tbody>
</table>
<script>
async function run(cmd) {
    try { await fetch('/cmd?cmd=' + encodeURIComponent(cmd)); } catch (e) { }
    await refreshLogs();
}
async function refreshLogs() {
    const tb = document.getElementById('logs');
    try {
        const logs = await (await fetch('/logs')).json();
        if (!logs.length) { tb.innerHTML = '<tr><td colspan="3">暂无</td></tr>'; return; }
        tb.innerHTML = logs.map(l =>
            '<tr><td class="time">' + l.time + '</td><td class="type">' + l.type + '</td><td>' + l.result + '</td></tr>'
        ).join('');
    } catch (e) { }
}
async function refreshMem() {
    const el = document.getElementById('mem');
    try {
        const m = await (await fetch('/mem')).json();
        el.textContent = fmt(m.managed) + ' 托管 / ' + fmt(m.workingSet) + ' 工作集';
    } catch (e) { el.textContent = '-'; }
}
function fmt(n) {
    if (n >= 1073741824) { return (n / 1073741824).toFixed(2) + ' GB'; }
    if (n >= 1048576) { return (n / 1048576).toFixed(1) + ' MB'; }
    if (n >= 1024) { return (n / 1024).toFixed(1) + ' KB'; }
    return n + ' B';
}
refreshLogs();
refreshMem();
setInterval(refreshMem, 60000);
</script>
</body>
</html>
""";

        [EntitySystem]
        private static void Awake(this ConsoleComponent self)
        {
            self.StartListen().Coroutine();
        }

        private static async ETTask StartListen(this ConsoleComponent self)
        {
            int port = Options.Instance.HttpConsolePort;
            if (port <= 0)
            {
                return;
            }

            HttpListener listener = new();
            listener.Prefixes.Add($"http://localhost:{port}/");
            try
            {
                listener.Start();
            }
            catch (Exception e)
            {
                Log.Error($"网页控制台启动失败（端口 {port}）：{e.Message}");
                return;
            }

            Log.Console($"网页控制台已启动：http://localhost:{port}/");

            EntityRef<ConsoleComponent> selfRef = self;
            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                self = selfRef;
                if (self == null)
                {
                    return;
                }
                await self.Handle(context);
            }
        }

        private static async ETTask Handle(this ConsoleComponent self, HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string body;
            string path = request.Url.AbsolutePath;
            switch (path)
            {
                case "/":
                case "/index.html":
                    body = IndexHtml;
                    response.ContentType = "text/html; charset=utf-8";
                    break;
                case "/cmd":
                    response.ContentType = "text/plain; charset=utf-8";
                    body = await self.RunCommand(request.QueryString["cmd"]);
                    break;
                case "/logs":
                    response.ContentType = "application/json; charset=utf-8";
                    body = self.BuildLogsJson();
                    break;
                case "/mem":
                    response.ContentType = "application/json; charset=utf-8";
                    body = BuildMemJson();
                    break;
                default:
                    response.StatusCode = 404;
                    response.ContentType = "text/plain; charset=utf-8";
                    body = "not found";
                    break;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(body);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }

        private static async ETTask<string> RunCommand(this ConsoleComponent self, string line)
        {
            line = line?.Trim() ?? "";
            if (line.Length == 0)
            {
                return "命令为空";
            }

            string mode = line.Split(' ', 2)[0];
            if (!ConsoleDispatcher.Instance.Contains(mode))
            {
                string unknown = $"未知命令：{mode}（可用：R / C）";
                self.AddLog(mode, unknown);
                return unknown;
            }

            EntityRef<ConsoleComponent> selfRef = self;
            ModeContex modeContex = self.AddComponent<ModeContex>();
            modeContex.Mode = mode;

            string result;
            try
            {
                IConsoleHandler handler = ConsoleDispatcher.Instance.Get(mode);
                result = await handler.Run(self.Fiber(), modeContex, line);
            }
            catch (Exception e)
            {
                result = $"执行出错：{e.Message}";
            }
            finally
            {
                self = selfRef;
                self?.RemoveComponent<ModeContex>();
            }

            self?.AddLog(mode, result);
            return result;
        }

        private static void AddLog(this ConsoleComponent self, string mode, string result)
        {
            if (self.Logs.Count >= MaxLogs)
            {
                self.Logs.RemoveAt(0);
            }

            self.Logs.Add(new ConsoleLogItem
            {
                Time = TimeInfo.Instance.ToDateTime(TimeInfo.Instance.ClientNow()).ToString("HH:mm:ss"),
                Type = GetTypeName(mode),
                Result = result,
            });
        }

        private static string GetTypeName(string mode)
        {
            switch (mode)
            {
                case ConsoleMode.ReloadDll:
                    return "热更 dll";
                case ConsoleMode.ReloadConfig:
                    return "重载配置";
                default:
                    return mode;
            }
        }

        private static string BuildLogsJson(this ConsoleComponent self)
        {
            StringBuilder sb = new();
            sb.Append('[');
            for (int i = self.Logs.Count - 1; i >= 0; --i)
            {
                ConsoleLogItem item = self.Logs[i];
                if (i != self.Logs.Count - 1)
                {
                    sb.Append(',');
                }
                sb.Append($"{{\"time\":\"{item.Time}\",\"type\":\"{EscapeJson(item.Type)}\",\"result\":\"{EscapeJson(item.Result)}\"}}");
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string BuildMemJson()
        {
            long managed = GC.GetTotalMemory(false);
            long workingSet = Environment.WorkingSet;
            return $"{{\"managed\":{managed},\"workingSet\":{workingSet}}}";
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }
    }
}
