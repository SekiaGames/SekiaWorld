using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(CoroutineLockQueue))]
    public static partial class CoroutineLockQueueSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockQueue self, int type)
        {
            self.type = type;
            self.runningCount = 0;
            self.maxConcurrency = 1;
        }
        
        [EntitySystem]
        private static void Destroy(this CoroutineLockQueue self)
        {
            self.queue.Clear();
            self.type = 0;
            self.maxConcurrency = 0;
            self.runningCount = 0;
        }
        
        internal static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockQueue self, int timeout, int line, string filePath)
        {
            CoroutineLock coroutineLock = null;
            if (self.runningCount < self.maxConcurrency)
            {
                ++self.runningCount;
                coroutineLock = self.AddChild<CoroutineLock, int, long, int>(self.type, self.Id, 1, true);
            }
            else
            {
                ETTask<EntityRef<CoroutineLock>> tcs = ETTask<EntityRef<CoroutineLock>>.Create(true);
                self.queue.Enqueue(tcs);
                coroutineLock = await tcs;
            }

            coroutineLock.SetTimeout(timeout, line, filePath).Coroutine();

            return coroutineLock;
        }

        internal static void Notify(this CoroutineLockQueue self, int level)
        {
            --self.runningCount;
            
            while (self.queue.Count > 0)
            {
                if (self.runningCount >= self.maxConcurrency)
                {
                    break;
                }
                ++self.runningCount;
                
                ETTask<EntityRef<CoroutineLock>> tcs = self.queue.Dequeue();
                CoroutineLock coroutineLock = self.AddChild<CoroutineLock, int, long, int>(self.type, self.Id, level, true);
                tcs.SetResult(coroutineLock);
            }
        }
    }
    
    [ChildOf(typeof(CoroutineLockQueueType))]
    public class CoroutineLockQueue: Entity, IAwake<int>, IDestroy
    {
        public int type;

        public int maxConcurrency { get; set; }

        public int runningCount;
        
        public Queue<ETTask<EntityRef<CoroutineLock>>> queue = new();

        public int Count
        {
            get
            {
                return this.queue.Count;
            }
        }
    }
}
