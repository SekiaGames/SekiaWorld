using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class CharacterChooseComponent: Entity, IAwake
    {
        public ETTask loadTcs;

        public Node sceneNode;

        public Camera3D camera;

        public bool active;

        public Node3D roleMount;

        public EntityRef<RoleComponent> role;
    }
}
