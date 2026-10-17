namespace ET
{
    public interface IConsoleHandler
    {
        ETTask<string> Run(Fiber fiber, ModeContex contex, string content);
    }
}