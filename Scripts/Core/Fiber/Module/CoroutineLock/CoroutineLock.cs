using System.Runtime.CompilerServices;

namespace ET
{
    [EntitySystemOf(typeof(CoroutineLock))]
    public static partial class CoroutineLockSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLock self, int type, long k, int count)
        {
            self.type = type;
            self.key = k;
            self.level = count;
        }
        
        [EntitySystem]
        private static void Destroy(this CoroutineLock self)
        {
            self.Scene<CoroutineLockComponent>().RunNextCoroutine(self.type, self.key, self.level + 1);
            self.type = CoroutineLockType.None;
            self.key = 0;
            self.level = 0;
        }

        internal static async ETTask SetTimeout(this CoroutineLock self, int timeout, [CallerLineNumber] int line = 0, [CallerFilePath] string filePath = "")
        {
            EntityRef<CoroutineLock> selfRef = self;
            int type = self.type;
            long key = self.key;
            int level = self.level;

            await self.Root().TimerComponent.WaitAsync(timeout);

            self = selfRef;

            if (self != null)
            {
                Log.Error($"{filePath}:{line} Coroutine lock timeout after {timeout}ms, type: {type}, key: {key}, level: {level}");
                selfRef.Dispose();
            }
        }
    }
    
    [ChildOf(typeof(CoroutineLockQueue))]
    public class CoroutineLock: Entity, IAwake<int, long, int>, IDestroy
    {
        public int type;
        public long key;
        public int level;
    }
}
