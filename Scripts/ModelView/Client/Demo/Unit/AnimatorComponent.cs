using System.Collections.Generic;

namespace ET.Client
{
	public enum MotionType
	{
		None,
		Idle,
		Run,
	}

	[ComponentOf]
	public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
	{
		public HashSet<string> Parameter = new();

		public MotionType MotionType;
		public float MontionSpeed;
		public bool isStop;
		public float stopSpeed;
	}
}
