#if DEBUG
namespace ET.Client
{
    [Event(SceneType.Demo)]
    public class AppStartInitFinish_StartAgent: AEvent<Scene, AppStartInitFinish>
    {
        protected override async ETTask Run(Scene root, AppStartInitFinish args)
        {
            if (Options.Instance.AgentMode == 0)
            {
                return;
            }

            await AgentConsole.StartAsync(root);
        }
    }
}
#endif
