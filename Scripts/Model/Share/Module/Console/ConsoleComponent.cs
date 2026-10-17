using System.Collections.Generic;

namespace ET
{
    public static class ConsoleMode
    {
        public const string ReloadDll = "R";
        public const string ReloadConfig = "C";
    }

    /// <summary>
    /// 一条网页控制台的操作记录：时间 / 操作类型 / 操作结果，直接对应页面上那张表格的三列。
    /// </summary>
    public struct ConsoleLogItem
    {
        public string Time;
        public string Type;
        public string Result;
    }

    [ComponentOf(typeof(Scene))]
    public class ConsoleComponent: Entity, IAwake
    {
        public readonly List<ConsoleLogItem> Logs = new();
    }
}
