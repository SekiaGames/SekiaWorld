using CommandLine;
using System;
using System.Collections.Generic;

namespace ET
{
    public enum AppType
    {
        Server,
        Watcher, // 每台物理机一个守护进程，用来启动该物理机上的所有进程
        GameTool,
        Demo,
    }
    
    public class Options: Singleton<Options>
    {
        [Option("AppType", Required = false, Default = AppType.Server, HelpText = "AppType enum")]
        public AppType AppType { get; set; }

        [Option("StartConfig", Required = false, Default = "StartConfig/Localhost")]
        public string StartConfig { get; set; }

        [Option("Process", Required = false, Default = 1)]
        public int Process { get; set; }
        
        [Option("Develop", Required = false, Default = 0, HelpText = "develop mode, 0正式 1开发 2压测")]
        public int Develop { get; set; }

        [Option("LogLevel", Required = false, Default = 0)]
        public int LogLevel { get; set; }

        [Option("HttpConsolePort", Required = false, Default = 8080, HelpText = "Realm 网页控制台端口，0 = 关闭")]
        public int HttpConsolePort { get; set; }

        [Option("AgentMode", Required = false, Default = 0, HelpText = "1 = 启用 Agent 模式（stdin 交互式命令行，见 HotfixView/Client/Module/Agent）")]
        public int AgentMode { get; set; }
    }
}