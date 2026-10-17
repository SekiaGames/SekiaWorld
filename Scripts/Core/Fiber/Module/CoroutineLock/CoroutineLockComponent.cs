using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ET
{
    [EntitySystemOf(typeof(CoroutineLockComponent))]
    public static partial class CoroutineLockComponentSystem
    {
        [EntitySystem]
        public static void Awake(this CoroutineLockComponent self)
        {
            self.GetParent<Scene>().CoroutineLockComponent = self;
        }
        
        [EntitySystem]
        public static void Update(this CoroutineLockComponent self)
        {
            int count = self.nextFrameRun.Count;
            for (int i = 0; i < count; i++)
            {
                (int coroutineLockType, long key, int level) = self.nextFrameRun.Dequeue();
                self.Notify(coroutineLockType, key, level);
            }
        }

        public static void RunNextCoroutine(this CoroutineLockComponent self, int coroutineLockType, long key, int level)
        {
            if (level == 100)
            {
                Log.Warning($"too much coroutine level: {coroutineLockType} {key}");
            }

            self.nextFrameRun.Enqueue((coroutineLockType, key, level));
        }

        private static CoroutineLockQueueType Get(this CoroutineLockComponent self, int coroutineLockType)
        {
            return self.GetChild<CoroutineLockQueueType>(coroutineLockType) ?? self.AddChildWithId<CoroutineLockQueueType>(coroutineLockType);
        }

        public static void SetMaxConcurrency(this CoroutineLockComponent self, int coroutineLockType, long key, int maxConcurrency)
        {
            CoroutineLockQueueType coroutineLockQueueType = self.Get(coroutineLockType);
            coroutineLockQueueType.SetMaxConcurrency(key, maxConcurrency);
        }

        public static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockComponent self, int coroutineLockType, long key, int timeout = 30000,
        [CallerLineNumber] int line = 0, [CallerFilePath] string filePath = "")
        {
            if (timeout <= 0)
            {
                timeout = 30000;
            }

            CoroutineLockQueueType coroutineLockQueueType = self.Get(coroutineLockType);
            return await coroutineLockQueueType.Wait(key, timeout, line, filePath);
        }

        private static void Notify(this CoroutineLockComponent self, int coroutineLockType, long key, int level)
        {
            CoroutineLockQueueType coroutineLockQueueType = self.GetChild<CoroutineLockQueueType>(coroutineLockType);
            if (coroutineLockQueueType == null)
            {
                return;
            }
            coroutineLockQueueType.Notify(key, level);
        }
    }
    
    public partial class Scene
    {
        private EntityRef<CoroutineLockComponent> coroutineLockComponent;

        public CoroutineLockComponent CoroutineLockComponent
        {
            get
            {
                return this.coroutineLockComponent;
            }
            set
            {
                this.coroutineLockComponent = value;
            }
        }
    }

    [DisableGetComponent]
    [ComponentOf(typeof(Scene))]
    public class CoroutineLockComponent: Entity, IAwake, IScene, IUpdate
    {
        public Fiber Fiber { get; set; }
        public SceneType SceneType { get; set; }
        
        public readonly Queue<(int, long, int)> nextFrameRun = new();
    }
}
