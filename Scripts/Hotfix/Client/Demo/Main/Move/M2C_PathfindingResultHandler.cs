namespace ET.Client
{
	[MessageHandler(SceneType.Demo)]
	public class M2C_PathfindingResultHandler : MessageHandler<Scene, M2C_PathfindingResult>
	{
		protected override async ETTask Run(Scene root, M2C_PathfindingResult message)
		{
			// ⚠️ 「切换角色」回选角之后当前场景已经被卸掉，但服务端**不知道**客户端退出了（ET 这套没有
			//    「离开地图」协议），还在按 AOI 推地图里其他单位（怪/别人）的移动消息
			//    ⇒ 「没有当前场景」是正常状态，直接忽略；否则每来一条就抛一次 NullReferenceException。
			UnitComponent unitComponent = root.CurrentScene()?.GetComponent<UnitComponent>();
			Unit unit = unitComponent?.Get(message.Id);
			if (unit == null)
			{
				return;
			}

			float speed = unit.NumericComponent.GetAsFloat(NumericType.Speed);

			// string last = message.Points.Count > 0 ? message.Points[^1].ToString() : "（空）";
			// Log.Info($"[寻路] 收到服务端路径：{message.Points.Count} 个路点，终点 {last} → 开始走（速度 {speed}）");

			await unit.GetComponent<MoveComponent>().MoveToAsync(message.Points, speed);
		}
	}
}
