using System;

namespace ET
{
    /// <summary>
    /// 标记的组件类型禁止通过GetComponent直接获取
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class DisableGetComponentAttribute : Attribute
    {
    }
}
