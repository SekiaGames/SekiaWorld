using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ET
{
    public class ObjectPool: Singleton<ObjectPool>, ISingletonAwake
    {
        private ConcurrentDictionary<Type, Pool> objPool;

        private readonly Func<Type, Pool> AddPoolFunc = type => new Pool(type, 1000);

        public void Awake()
        {
            lock (this)
            {
                objPool = new ConcurrentDictionary<Type, Pool>();
            }
        }

        public T Fetch<T>() where T : class
        {
            return this.Fetch(typeof (T)) as T;
        }

        public object Fetch(Type type, bool isFromPool = true)
        {
            if (!isFromPool)
            {
                return Activator.CreateInstance(type);
            }
            
            Pool pool = GetPool(type);
            object obj = pool.Get();
            if (obj is IPool p)
            {
                p.IsFromPool = true;
            }
            return obj;
        }

        public void Recycle(object obj)
        {
            if (obj is IPool p)
            {
                if (!p.IsFromPool)
                {
                    return;
                }

                // ET10:回收前清理残留状态(默认空实现,具体类型自行重写/生成)
                p.Clear();

                // 注意:这里不把 IsFromPool 置回 false,配合调试期池的重复回收检测(见 Pool)
                // p.IsFromPool = false;
            }

            Type type = obj.GetType();
            Pool pool = GetPool(type);
            pool.Return(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Pool GetPool(Type type)
        {
            return this.objPool.GetOrAdd(type, AddPoolFunc);
        }

#if DEBUG || POOL_CHECK
        /// <summary>
        /// 调试期对象池:带重复回收检测(同一对象回收两次直接抛异常,便于尽早暴露问题)
        /// </summary>
        private class Pool
        {
            private readonly Type ObjectType;
            private readonly int MaxCapacity;
            private readonly HashSet<object> items = new();

            public Pool(Type objectType, int maxCapacity)
            {
                ObjectType = objectType;
                MaxCapacity = maxCapacity;
            }

            public object Get()
            {
                lock (this)
                {
                    if (this.items.Count <= 0)
                    {
                        return Activator.CreateInstance(this.ObjectType);
                    }

                    object obj = this.items.First();
                    this.items.Remove(obj);
                    return obj;
                }
            }

            public void Return(object obj)
            {
                lock (this)
                {
                    if (this.items.Count >= this.MaxCapacity)
                    {
                        return;
                    }

                    if (!this.items.Add(obj))
                    {
                        throw new Exception($"对象重复回收到对象池: {this.ObjectType.FullName}");
                    }
                }
            }
        }
#else
        /// <summary>
        /// 发布版对象池:线程安全的无锁实现
        /// </summary>
        private class Pool
        {
            private readonly Type ObjectType;
            private readonly int MaxCapacity;
            private int NumItems;
            private readonly ConcurrentQueue<object> _items = new();
            private object FastItem;

            public Pool(Type objectType, int maxCapacity)
            {
                ObjectType = objectType;
                MaxCapacity = maxCapacity;
            }

            public object Get()
            {
                object item = FastItem;
                if (item == null || Interlocked.CompareExchange(ref FastItem, null, item) != item)
                {
                    if (_items.TryDequeue(out item))
                    {
                        Interlocked.Decrement(ref NumItems);
                        return item;
                    }

                    return Activator.CreateInstance(this.ObjectType);
                }

                return item;
            }

            public void Return(object obj)
            {
                if (FastItem != null || Interlocked.CompareExchange(ref FastItem, obj, null) != null)
                {
                    if (Interlocked.Increment(ref NumItems) <= MaxCapacity)
                    {
                        _items.Enqueue(obj);
                        return;
                    }

                    Interlocked.Decrement(ref NumItems);
                }
            }
        }
#endif
    }
}
