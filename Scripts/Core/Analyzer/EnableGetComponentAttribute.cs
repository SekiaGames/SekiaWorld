using System;

namespace ET
{
    /// <summary>
    /// 允许在标记的方法或类型内通过 GetComponent 获取指定组件。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class EnableGetComponentAttribute : Attribute
    {
        public Type ComponentType { get; }

        public EnableGetComponentAttribute(Type componentType)
        {
            this.ComponentType = componentType;
        }
    }
}
