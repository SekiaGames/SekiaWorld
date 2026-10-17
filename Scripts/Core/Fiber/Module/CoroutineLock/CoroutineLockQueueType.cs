namespace ET
{
    [EntitySystemOf(typeof(CoroutineLockQueueType))]
    [FriendOf(typeof(CoroutineLockQueueType))]
    public static partial class CoroutineLockQueueTypeSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockQueueType self)
        {
        }

        private static CoroutineLockQueue Get(this CoroutineLockQueueType self, long key)
        {
            return self.GetChild<CoroutineLockQueue>(key) ?? self.AddChildWithId<CoroutineLockQueue, int>(key, (int)self.Id, true);
        }

        private static void Remove(this CoroutineLockQueueType self, long key)
        {
            self.RemoveChild(key);
        }

        internal static void SetMaxConcurrency(this CoroutineLockQueueType self, long key, int maxConcurrency)
        {
            CoroutineLockQueue coroutineLockQueue = self.Get(key);
            coroutineLockQueue.maxConcurrency = maxConcurrency;
        }

        internal static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockQueueType self, long key, int timeout, int line, string filePath)
        {
            CoroutineLockQueue queue = self.Get(key);
            return await queue.Wait(timeout, line, filePath);
        }

        internal static void Notify(this CoroutineLockQueueType self, long key, int level)
        {
            CoroutineLockQueue queue = self.Get(key);
            if (queue == null)
            {
                return;
            }
            
            if (queue.Count == 0)
            {
                self.Remove(key);
                return;
            }
            
            queue.Notify(level);
        }
    }
    
    [ChildOf(typeof(CoroutineLockComponent))]
    public class CoroutineLockQueueType: Entity, IAwake
    {
    }
}
