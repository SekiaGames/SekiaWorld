using System;
using System.Collections;
using System.Collections.Generic;

namespace ET
{
    public class BTEnv: DisposeObject, IPool
    {
        public EntityRef<Scene> Scene;

        public long EntityId;

        public bool IsFromPool { get; set; }

        private Dictionary<string, object> dict = new();

        private List<int> runPath;

        public List<int> RunPath => this.runPath;

        public void AddRunPath(int nodeId)
        {
            this.runPath ??= new List<int>();
            this.runPath.Add(nodeId);
        }

        public static BTEnv Create(Scene scene, long entityId = 0, bool isFromPool = true)
        {
            BTEnv env = (BTEnv)ObjectPool.Instance.Fetch(typeof (BTEnv), isFromPool);
            env.Scene = scene;
            env.EntityId = entityId;
            return env;
        }

        public override void Dispose()
        {
            ObjectPool.Instance.Recycle(this);
        }

        void IPool.Clear()
        {
            foreach (var kv in this.dict)
            {
                if (kv.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            this.dict.Clear();
            this.runPath?.Clear();
            this.Scene = null;
            this.EntityId = 0;
        }

        public BTEnv Clone()
        {
            BTEnv env = Create(this.Scene, this.EntityId);
            ObjectHelper.Swap(ref env.dict, ref this.dict);
            return env;
        }

        public T GetCollection<T>(string key) where T: IEnumerable
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                throw new Exception($"btenv not found key: {key} {typeof (T).FullName}");
            }

            return (T)value;
        }

        public bool TryGetStruct<T>(string key, out T t) where T: struct
        {
            t = default;
            if (key == null)
            {
                return false;
            }

            if (!this.dict.TryGetValue(key, out object value))
            {
                return false;
            }

            try
            {
                IValue<T> iValue = (IValue<T>)value;
                t = iValue.Value;
                return true;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把 {value.GetType()} 转为 {typeof (T)}", e);
            }
        }

        public T GetStruct<T>(string key) where T: struct
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                throw new Exception($"btenv not found key: {key} {typeof (T).FullName}");
            }

            try
            {
                IValue<T> iValue = (IValue<T>)value;
                return iValue.Value;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把 {value.GetType()} 转为 {typeof (T)}", e);
            }
        }

        public bool TryGetEntity<T>(string key, out T t) where T: Entity
        {
            t = null;

            if (key == null)
            {
                return false;
            }

            if (!this.dict.TryGetValue(key, out object value))
            {
                return false;
            }

            try
            {
                IValue<EntityRef<T>> iValue = (ValueTypeWrap<EntityRef<T>>)value;
                t = iValue.Value;
                return true;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把 {value.GetType()} 转为 {typeof (T)}", e);
            }
        }

        public T GetEntity<T>(string key) where T: Entity
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                throw new Exception($"btenv not found key: {key} {typeof (T).FullName}");
            }

            try
            {
                IValue<EntityRef<T>> iValue = (ValueTypeWrap<EntityRef<T>>)value;
                return iValue.Value;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把 {value.GetType()} 转为 {typeof (T)}", e);
            }
        }

        public bool ContainKey(string key)
        {
            return this.dict.ContainsKey(key);
        }

        public void AddCollection<T>(string key, T list) where T: IEnumerable
        {
            this.dict[key] = list;
        }

        public void AddEntity<T>(string key, T entity) where T: Entity
        {
            ValueTypeWrap<EntityRef<T>> wrap;
            if (this.dict.TryGetValue(key, out object value))
            {
                wrap = (ValueTypeWrap<EntityRef<T>>)value;
                wrap.Value = entity;
            }
            else
            {
                wrap = ValueTypeWrap<EntityRef<T>>.Create(entity);
                this.dict.Add(key, wrap);
            }
        }

        public void AddStruct<T>(string key, T value) where T: struct
        {
            ValueTypeWrap<T> wrap;
            if (this.dict.TryGetValue(key, out object obj))
            {
                wrap = (ValueTypeWrap<T>)obj;
                wrap.Value = value;
            }
            else
            {
                wrap = ValueTypeWrap<T>.Create(value);
                this.dict.Add(key, wrap);
            }
        }

        public bool TryGetObject<T>(string key, out T t) where T: class
        {
            t = null;
            if (key == null)
            {
                return false;
            }

            if (!this.dict.TryGetValue(key, out object value))
            {
                return false;
            }

            try
            {
                t = (T)value;
                return true;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把 {value.GetType()} 转为 {typeof (T)}", e);
            }
        }

        public T GetObject<T>(string key) where T: class
        {
            if (!this.dict.TryGetValue(key, out object value))
            {
                throw new Exception($"btenv not found key: {key} {typeof (T).FullName}");
            }

            try
            {
                return (T)value;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"不能把 {value.GetType()} 转为 {typeof (T)}", e);
            }
        }

        public void AddObject<T>(string key, T value) where T: class
        {
            this.dict[key] = value;
        }
    }
}
