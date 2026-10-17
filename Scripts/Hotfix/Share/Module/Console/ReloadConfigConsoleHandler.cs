using System;

namespace ET
{
    [ConsoleHandler(ConsoleMode.ReloadConfig)]
    public class ReloadConfigConsoleHandler: IConsoleHandler
    {
        public async ETTask<string> Run(Fiber fiber, ModeContex contex, string content)
        {
            string[] ss = content.Split(" ");
            if (ss.Length < 2)
            {
                // 不带表名 = 重载全部表格（网页上那个按钮就走这条）
                await ConfigLoader.Instance.LoadAsync();
                string all = "全部配置重载完成";
                Log.Console(all);
                return all;
            }

            string configName = ss[1];
            string category = $"{configName}Category";
            Type type = CodeTypes.Instance.GetType($"ET.{category}");
            if (type == null)
            {
                return $"没找到配置 {category}";
            }

            await ConfigLoader.Instance.Reload(type);
            string result = $"配置 {configName} 重载完成";
            Log.Console(result);
            return result;
        }
    }
}
