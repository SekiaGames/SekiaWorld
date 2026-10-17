using Godot;

namespace ET
{
    public static class Define
    {
#if DEBUG
        public static bool IsDebug = true;
#else
        public static bool IsDebug = false;
#endif

        /// <summary>在编辑器中运行（导出后为 false）</summary>
        public static bool IsEditor = OS.HasFeature("editor");
    }
}
