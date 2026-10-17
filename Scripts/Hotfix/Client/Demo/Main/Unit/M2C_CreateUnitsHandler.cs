namespace ET.Client
{
	[MessageHandler(SceneType.Demo)]
	public class M2C_CreateUnitsHandler: MessageHandler<Scene, M2C_CreateUnits>
	{
		protected override async ETTask Run(Scene root, M2C_CreateUnits message)
		{
			// 「回选角」后当前场景已卸、服务端却还在推（见 M2C_PathfindingResultHandler 那段注释）⇒ 容忍没有场景
			Scene currentScene = root.CurrentScene();
			UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
			if (unitComponent == null)
			{
				return;
			}
			
			foreach (UnitInfo unitInfo in message.Units)
			{
				if (unitComponent.Get(unitInfo.UnitId) != null)
				{
					continue;
				}
				Unit unit = UnitFactory.Create(currentScene, unitInfo);
			}
			await ETTask.CompletedTask;
		}
	}
}
