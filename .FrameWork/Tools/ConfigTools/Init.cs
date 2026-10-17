using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace ET.Server
{
    internal static class Init
    {
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };
            
            try
            {
                // 命令行参数
                string tool = args.Length > 0? args[0] : string.Empty;

                World.Instance.AddSingleton(new Options { Process = 1 });
                World.Instance.AddSingleton<Logger>().Log = new NLogger(tool, Options.Instance.Process, 0);
                
                List<Assembly> assemblies = new() { typeof (Init).Assembly, typeof (World).Assembly };
                if (tool == "BTImport")
                {
                    assemblies.Add(BTImport.BuildNodeAssembly());
                }

                World.Instance.AddSingleton<CodeTypes, Assembly[]>(assemblies.ToArray());
                World.Instance.AddSingleton<EventSystem>();
                
                // 强制调用一下mongo，避免mongo库被裁剪
                MongoHelper.ToJson(1);
                
                ETTask.ExceptionHandler += Log.Error;
                
                Log.Info($"server start........................ ");
				
                switch (tool)
                {
                    case "ExcelExporter":
                    {
                        ExcelExporter.Export();
                        return 0;
                    }
                    case "Proto2CS":
                    {
                        Proto2CS.Export();
                        return 0;
                    }
                    case "BTImport":
                    {
                        BTImport.Export();
                        return 0;
                    }
                    default:
                    {
                        Log.Console($"unknown tool: {tool}");
                        return 1;
                    }
                }
            }
            catch (Exception e)
            {
                // 这里必须直接用 System.Console.WriteLine，不要换成 Log.Console：
                //   本 catch 最主要的场景就是「命令行参数解析失败」，而 Log.Console 依赖 Logger 单例；
                //   AddSingleton<Logger>() 在 try 里、解析之后才执行 —— 此刻 Logger.Instance 仍为 null，
                //   Log.GetLog() 会二次抛 NullReferenceException，把真正的错误信息整个吞掉。
                System.Console.WriteLine(e.ToString());
            }
            return 1;
        }
    }
}