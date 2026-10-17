namespace ET.Server
{
    [MessageHandler(SceneType.Gate)]
    public class C2M_StopHandler: MessageHandler<Unit, C2M_Stop>
    {
        protected override async ETTask Run(Unit unit, C2M_Stop message)
        {
            unit.Stop(1);
            await ETTask.CompletedTask;
        }
    }
}