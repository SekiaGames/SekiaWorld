using System;
using System.Collections.Generic;

namespace ET
{
    public partial class ETTask
    {
        [SkipAwaitEntityCheck]
        public static async ETTask<T> GetContextAsync<T>() where T: class
        {
            ETTask<object> tcs = ETTask<object>.Create(true);
            tcs.TaskType = TaskType.ContextTask;
            object ret = await tcs;
            return ret as T;
        }
        
        [SkipAwaitEntityCheck]
        public static async ETTask<object> GetContextAsync()
        {
            ETTask<object> tcs = ETTask<object>.Create(true);
            tcs.TaskType = TaskType.ContextTask;
            object ret = await tcs;
            return ret;
        }
        
        private class CoroutineBlocker
        {
            private int count;

            private ETTask tcs;

            public CoroutineBlocker(int count)
            {
                this.count = count;
            }
            
            public async ETTask RunSubCoroutineAsync(ETTask task)
            {
                try
                {
                    await task;
                }
                finally
                {
                    --this.count;
                
                    if (this.count <= 0 && this.tcs != null)
                    {
                        ETTask t = this.tcs;
                        this.tcs = null;
                        t.SetResult();
                    }
                }
            }

            public async ETTask WaitAsync()
            {
                if (this.count <= 0)
                {
                    return;
                }
                this.tcs = ETTask.Create(true);
                await tcs;
            }
        }

        public static async ETTask WaitAny(List<ETTask> tasks)
        {
            if (tasks.Count == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            
            CoroutineBlocker coroutineBlocker = new(1);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }

        public static async ETTask WaitAny(ETTask[] tasks)
        {
            if (tasks.Length == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            CoroutineBlocker coroutineBlocker = new(1);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }

        public static async ETTask WaitAll(ETTask[] tasks)
        {
            if (tasks.Length == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            CoroutineBlocker coroutineBlocker = new(tasks.Length);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }

        public static async ETTask WaitAll(List<ETTask> tasks)
        {
            if (tasks.Count == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            CoroutineBlocker coroutineBlocker = new(tasks.Count);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }
    }
}