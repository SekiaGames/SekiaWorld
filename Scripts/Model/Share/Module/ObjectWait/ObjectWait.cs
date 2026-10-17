using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class WaitTypeError
    {
        public const int Success = 0;
        public const int Destroy = 1;
        public const int Cancel = 2;
        public const int Timeout = 3;
    }
    
    public interface IWaitType
    {
        int Error
        {
            get;
            set;
        }
    }

    [EntitySystemOf(typeof(ObjectWait))]
    [FriendOf(typeof(ObjectWait))]
    public static partial class ObjectWaitSystem
    {
        [EntitySystem]
        private static void Awake(this ObjectWait self)
        {
            self.tcss.Clear();
        }
        
        [EntitySystem]
        private static void Destroy(this ObjectWait self)
        {
            foreach(var p in self.tcss)
            {
                foreach(object v in p.Value)
                {
                    ((IDestroyRun)v).SetResult();
                }
            }
            self.tcss.Clear();
        }

        private interface IDestroyRun
        {
            void SetResult();
        }

        private class ResultCallback<K>: Object, IDestroyRun where K : struct, IWaitType
        {
            private ETTask<K> tcs;

            public ResultCallback()
            {
                this.tcs = ETTask<K>.Create(true);
            }

            public bool IsDisposed
            {
                get
                {
                    return this.tcs == null;
                }
            }

            public ETTask<K> Task => this.tcs;

            public void SetResult(K k)
            {
                var t = tcs;
                this.tcs = null;
                t.SetResult(k);
            }

            public void SetResult()
            {
                var t = tcs;
                this.tcs = null;
                t.SetResult(new K() { Error = WaitTypeError.Destroy });
            }
        }
        
        public static async ETTask<T> Wait<T>(this ObjectWait self) where T : struct, IWaitType
        {
            ResultCallback<T> tcs = new ResultCallback<T>();
            Type type = typeof (T);
            self.Add(type, tcs);

            void CancelAction()
            {
                self.Notify(new T() { Error = WaitTypeError.Cancel });
            }

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            T ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs.Task;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }

        public static async ETTask<T> Wait<T>(this ObjectWait self, int timeout) where T : struct, IWaitType
        {
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            ResultCallback<T> tcs = new ResultCallback<T>();
            Type type = typeof(T);
            async ETTask WaitTimeout()
            {
                EntityRef<ObjectWait> selfRef = self;
                await self.Root().TimerComponent.WaitAsync(timeout);
                self = selfRef;
                if (self == null)
                {
                    return;
                }

                if (cancellationToken.IsCancel())
                {
                    return;
                }
                if (tcs.IsDisposed)
                {
                    return;
                }
                
                if (!self.tcss.TryGetValue(type, out var tcsList))
                {
                    return;
                }
                tcsList.Remove(tcs);
                tcs.SetResult(new T() { Error = WaitTypeError.Timeout });
            }
            
            WaitTimeout().Coroutine();
            
            self.Add(type, tcs);
            
            void CancelAction()
            {
                self.Notify(new T() { Error = WaitTypeError.Cancel });
            }
            
            T ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs.Task;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }

        public static void Notify<T>(this ObjectWait self, T obj) where T : struct, IWaitType
        {
            Type type = typeof (T);
            if (!self.tcss.TryGetValue(type, out var tcsList) || tcsList.Count == 0)
            {
                return;
            }

            // 先取快照并清空，再逐个 SetResult。
            // SetResult 会同步恢复 await 的续体，而续体里可能立刻再次 Wait/Notify
            // （比如 MoveHelper.MoveToAsync 收到 M2C_Stop 后马上发起下一次移动）。
            // 如果边遍历边回调：
            //   1. 新加入的 ResultCallback 会被末尾的 Clear 清掉，新等待者永远收不到通知；
            //   2. 已完成的 ResultCallback 会被重复 SetResult，其内部 tcs 已置 null → NullReferenceException。
            List<object> callbacks = new(tcsList);
            tcsList.Clear();
            foreach (object tcs in callbacks)
            {
                ((ResultCallback<T>) tcs).SetResult(obj);
            }
        }


        private static void Add(this ObjectWait self, Type type, object obj)
        {
            if (self.tcss.TryGetValue(type, out var list))
            {
                list.Add(obj);
            }
            else
            {
                self.tcss.Add(type, new List<object> { obj });
            }
        }
    }

    [ComponentOf]
    public class ObjectWait: Entity, IAwake, IDestroy
    {
        public Dictionary<Type, List<object>> tcss = new();
    }
}