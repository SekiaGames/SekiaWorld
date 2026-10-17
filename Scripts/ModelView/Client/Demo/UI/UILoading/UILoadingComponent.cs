using Godot;

namespace ET.Client
{
	[ComponentOf(typeof(UI))]
	public class UILoadingComponent : Entity, IAwake
	{
		public Label text;
	}
}
