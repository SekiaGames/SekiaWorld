using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class GameObjectComponent: Entity, IAwake, IDestroy
    {
        public Node3D GameObject { get; set; }
    }
}
