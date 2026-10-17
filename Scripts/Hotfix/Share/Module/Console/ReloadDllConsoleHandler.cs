namespace ET
{
    [ConsoleHandler(ConsoleMode.ReloadDll)]
    public class ReloadDllConsoleHandler: IConsoleHandler
    {
        public async ETTask<string> Run(Fiber fiber, ModeContex contex, string content)
        {
            await ETTask.CompletedTask;
            CodeLoader.Instance.Reload();
            return "dll 重载完成";
        }
    }
}