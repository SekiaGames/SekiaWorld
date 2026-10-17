namespace ET
{
    [Invoke((long)SceneType.Main)]
    [SkipAwaitEntityCheck]
    public class FiberInit_Main: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            //加载表格
            await EventSystem.Instance.PublishAsync(root, new CodeModeEvent());
            await World.Instance.AddSingleton<ConfigLoader>().LoadAsync();

            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
        }
    }
}
