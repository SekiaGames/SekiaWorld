namespace ET.Client
{
	public enum MotionType
	{
		None,
		Idle,
		Move,
	}

	[ComponentOf(typeof(Unit))]
	public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
	{
		public EntityRef<RoleComponent> Role { get; set; }

		public MotionType MotionType { get; set; }

		public string IdleAction { get; set; } = "wait";

		public string MoveAction { get; set; } = "walk";
	}
}
