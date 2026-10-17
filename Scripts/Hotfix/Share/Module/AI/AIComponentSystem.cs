using System;

namespace ET
{
    [EntitySystemOf(typeof(AIComponent))]
    [FriendOf(typeof(AIComponent))]
    [FriendOf(typeof(AIDispatcherComponent))]
    public static partial class AIComponentSystem
    {
        [Invoke(TimerInvokeType.AITimer)]
        public class AITimer: ATimer<AIComponent>
        {
            protected override void Run(AIComponent self)
            {
                try
                {
                    self.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Id}\n{e}");
                }
            }
        }

        [EntitySystem]
        private static void Awake(this AIComponent self, int aiConfigId)
        {
            self.AIConfigId = aiConfigId;

            // 出生点 = 挂 AI 那一刻的位置（服务端巡逻的圆心 birthPos）；必须在 Unit 建好、摆到位之后挂 AI
            if (self.Parent is Unit unit)
            {
                self.BirthPos = unit.Position;
            }

            self.Timer = self.Root().TimerComponent.NewRepeatedTimer(1000, TimerInvokeType.AITimer, self);
        }

        [EntitySystem]
        private static void Destroy(this AIComponent self)
        {
            self.Root().TimerComponent?.Remove(ref self.Timer);
            self.CancellationToken?.Cancel();
            self.CancellationToken = null;
            self.Current = 0;
        }

        private static void Check(this AIComponent self)
        {
            Fiber fiber = self.Fiber();
            if (self.Parent == null)
            {
                fiber.Root.TimerComponent.Remove(ref self.Timer);
                return;
            }

            var oneAI = AIConfigCategory.Instance.AIConfigs[self.AIConfigId];

            foreach (AIConfig aiConfig in oneAI.Values)
            {

                AAIHandler aaiHandler = AIDispatcherComponent.Instance.Get(aiConfig.Name);

                if (aaiHandler == null)
                {
                    Log.Error($"not found aihandler: {aiConfig.Name}");
                    continue;
                }

                int ret = aaiHandler.Check(self, aiConfig);
                if (ret != 0)
                {
                    continue;
                }

                if (self.Current == aiConfig.Id)
                {
                    break;
                }

                self.Cancel();
                ETCancellationToken cancellationToken = new();
                self.CancellationToken = cancellationToken;
                self.Current = aiConfig.Id;

                aaiHandler.Execute(self, aiConfig, cancellationToken).Coroutine(cancellationToken);
                return;
            }

        }

        private static void Cancel(this AIComponent self)
        {
            self.CancellationToken?.Cancel();
            self.Current = 0;
            self.CancellationToken = null;
        }
    }
}
