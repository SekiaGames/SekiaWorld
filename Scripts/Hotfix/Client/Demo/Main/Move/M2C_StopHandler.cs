using Unity.Mathematics;

namespace ET.Client
{
	[MessageHandler(SceneType.Demo)]
	public class M2C_StopHandler : MessageHandler<Scene, M2C_Stop>
	{
		protected override async ETTask Run(Scene root, M2C_Stop message)
		{
			// 「回选角」后当前场景已卸、服务端却还在推（见 M2C_PathfindingResultHandler 那段注释）⇒ 容忍没有场景
			Unit unit = root.CurrentScene()?.GetComponent<UnitComponent>()?.Get(message.Id);
			if (unit == null)
			{
				return;
			}

			MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
			moveComponent.Stop(message.Error == 0);
			unit.Position = message.Position;
			unit.Rotation = message.Rotation;
			await ETTask.CompletedTask;
		}
	}
}
