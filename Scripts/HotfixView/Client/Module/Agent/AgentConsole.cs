#if DEBUG
using System;
using System.Threading;
using Godot;

namespace ET.Client
{
    public static class AgentConsole
    {
        private const string ThreadName = "AgentConsole";

        private const int ReBroadcastInterval = 100;
        private const int ReBroadcastMaxTimes = 30;

        public static async ETTask StartAsync(Scene root)
        {
            Log.Console("=== Agent 模式已启用（stdin 交互式命令行）===");
            Log.Console("输入数字执行对应项；? 重新广播可操作项；quit 退出游戏");

            StartReadLoop(root);

            EntityRef<Scene> rootRef = root;
            await AgentMenu.WaitScreenAsync(root);
            root = rootRef;
            if (root == null)
            {
                return;
            }

            AgentMenu.Broadcast(root);
        }

        private static void StartReadLoop(Scene root)
        {
            Fiber fiber = root.Fiber();

            Thread thread = new(() =>
            {
                while (true)
                {
                    string line = System.Console.In.ReadLine();

                    if (line == null)
                    {
                        fiber.ThreadSynchronizationContext.Post(() => Quit("stdin 已结束（EOF）"));
                        return;
                    }

                    string text = line;

                    fiber.ThreadSynchronizationContext.Post(() => OnLine(root, text).Coroutine());
                }
            })
            {
                IsBackground = true,
                Name = ThreadName,
            };

            thread.Start();
        }

        private static async ETTask OnLine(Scene root, string line)
        {
            line = line.Trim();

            if (line.Length == 0 || line == "?")
            {
                AgentMenu.Broadcast(root);
                return;
            }

            if (line == "quit")
            {
                Quit("收到 quit");
                return;
            }

            string[] args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(args[0], out int index))
            {
                Log.Console($"不认识这个命令：{line}（输入 ? 看当前可操作项）");
                return;
            }

            string before = AgentMenu.ScreenKey(root);
            Log.Console(AgentMenu.Invoke(root, index, args));

            EntityRef<Scene> rootRef = root;
            await WaitScreenChangeAsync(root, before);
            root = rootRef;
            if (root == null)
            {
                return;
            }

            AgentMenu.Broadcast(root);
        }

        private static async ETTask WaitScreenChangeAsync(Scene root, string before)
        {
            EntityRef<Scene> rootRef = root;
            EntityRef<TimerComponent> timerRef = root.TimerComponent;

            for (int i = 0; i < ReBroadcastMaxTimes; ++i)
            {
                TimerComponent timer = timerRef;
                await timer.WaitAsync(ReBroadcastInterval);

                root = rootRef;
                if (root == null)
                {
                    return;
                }

                if (AgentMenu.ScreenKey(root) != before)
                {
                    return;
                }
            }
        }

        private static void Quit(string reason)
        {
            Log.Console($"Agent 模式退出：{reason}");

            Node node = GlobalComponent.RootNode;
            if (node == null)
            {
                return;
            }

            node.GetTree().Quit();
        }
    }
}
#endif
