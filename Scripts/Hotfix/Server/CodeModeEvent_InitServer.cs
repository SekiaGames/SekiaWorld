#if GODOT
namespace ET.Server
{
    [Event(SceneType.Main)]
    public class CodeModeEvent_InitServer: AEvent<Scene, CodeModeEvent>
    {
        protected override async ETTask Run(Scene root, CodeModeEvent args)
        {
            Init.codeMode |= CodeMode.Server;

            await ETTask.CompletedTask;
        }
    }
}
#endif
