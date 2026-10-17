using System;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public abstract class DisposeObject: Object, IDisposable, ISupportInitialize
    {
        public virtual void Dispose()
        {
        }
        
        public virtual void BeginInit()
        {
        }
        
        public virtual void EndInit()
        {
        }
    }

    public interface IPool
    {
        bool IsFromPool
        {
            get;
            set;
        }

        /// <summary>
        /// 回收到对象池前调用，用于清理残留状态（ET10：默认空实现，需要的类型自行重写/生成）
        /// </summary>
        void Clear()
        {
        }
    }
}