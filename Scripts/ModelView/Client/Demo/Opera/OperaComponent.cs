using Godot;

namespace ET.Client
{
	[ComponentOf(typeof(Scene))]
	public class OperaComponent: Entity, IAwake, IUpdate
    {
        public Vector3 ClickPoint;

        public bool mouseDown;
    }
}
