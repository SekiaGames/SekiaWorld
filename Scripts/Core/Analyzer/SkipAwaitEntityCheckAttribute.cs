using System;

namespace ET
{
    /// <summary>
    /// ET10：标记方法/类型，跳过 AwaitEntityAnalyzer 的"await 后禁止访问 Entity"检查
    /// （仅用于确实安全、无法用 EntityRef 表达的场景）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class SkipAwaitEntityCheckAttribute: Attribute
    {
    }
}
