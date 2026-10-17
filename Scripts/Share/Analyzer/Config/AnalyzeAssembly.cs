using System.IO;

namespace ET.Analyzer
{
    /// <summary>
    /// 「需要参与分析的程序集」名单。
    ///
    /// ⚠️ 这里的字符串必须与目标工程 &lt;AssemblyName&gt; **完全相等**：
    /// AnalyzerHelper.IsAssemblyNeedAnalyze() 用的是 `assemblyName == analyzeAssemblyName`
    /// 精确比较，不做后缀/通配匹配。名字对不上 → 分析器 / 源生成器**静默失效**
    /// （不报错、只是不跑），极难发现，所以**改程序集名时必须回来同步这里**。
    ///
    /// 命名来历（不要照抄上游 ET8.1 的旧名）：
    /// - ET8.1 原版服务端工程目录叫 DotNet/、客户端叫 Unity/，常量名即由此而来。
    /// - 本仓库拆包时两端目录已分别改名 Server/ 与 Godot/，
    ///   客户端程序集前缀也由 Unity. 改成 SekiaWorld.，故此处同步为 ServerXxx / SekiaWorldXxx。
    /// - ⚠️ 服务端 &lt;AssemblyName&gt; **没有**跟着目录改名，仍是 Core/Model/Hotfix
    ///   （上游的 DotNet. 前缀只体现在文件名上），所以 ServerCore 的值是 "Core" 而非 "Server.Core"。
    /// </summary>
    public static class AnalyzeAssembly
    {
        public const string ServerCore = "Core";
        public const string ServerModel = "Model";
        public const string ServerHotfix = "Hotfix";

        public const string SekiaWorldCore = "SekiaWorld.Core";
        public const string SekiaWorldModel = "SekiaWorld.Model";
        public const string SekiaWorldHotfix = "SekiaWorld.Hotfix";
        public const string SekiaWorldModelView = "SekiaWorld.ModelView";
        public const string SekiaWorldHotfixView = "SekiaWorld.HotfixView";

        public static readonly string[] AllHotfix =
        {
            ServerHotfix, SekiaWorldHotfix, SekiaWorldHotfixView,
        };

        public static readonly string[] AllModel =
        {
            ServerModel, SekiaWorldModel, SekiaWorldModelView
        };

        public static readonly string[] AllModelHotfix =
        {
            ServerModel, ServerHotfix, 
            SekiaWorldModel, SekiaWorldHotfix, SekiaWorldModelView, SekiaWorldHotfixView, 
        };
        
        public static readonly string[] All =
        {
            ServerCore, ServerModel, ServerHotfix, 
            SekiaWorldCore, SekiaWorldModel, SekiaWorldHotfix, SekiaWorldModelView, SekiaWorldHotfixView, 
        };

        public static readonly string[] ServerModelHotfix =
        {
            ServerModel,ServerHotfix,
        };
        
        public static readonly string[] AllLogicModel =
        {
            ServerModel, SekiaWorldModel
        };
    }
    
}