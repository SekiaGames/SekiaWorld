

namespace ET.Server
{
	[MessageHandler(SceneType.Gate)]
	public class G2M_SessionDisconnectHandler : MessageHandler<Unit, G2M_SessionDisconnect>
	{
		protected override async ETTask Run(Unit unit, G2M_SessionDisconnect message)
		{
			await ETTask.CompletedTask;
		}
	}
}