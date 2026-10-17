#if GODOT
namespace ET.Client
{
    [Event(SceneType.Main)]
    public class CodeModeEvent_InitClient: AEvent<Scene, CodeModeEvent>
    {
        protected override async ETTask Run(Scene root, CodeModeEvent args)
        {
            Init.codeMode |= CodeMode.Client;

            await ETTask.CompletedTask;
        }
    }
}
#endif
