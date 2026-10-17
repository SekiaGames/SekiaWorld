using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [ChildOf]
    public class RoleComponent: Entity, IAwake<RoleParts>, IDestroy
    {
        public RoleParts Parts { get; set; }

        public Node3D Root { get; set; }

        public Skeleton3D Skeleton { get; set; }

        public AnimationPlayer AnimPlayer { get; set; }

        public AnimationTree AnimTree { get; set; }

        public Dictionary<int, Node3D> Mounted { get; set; } = new();

        public string CurrentAction { get; set; }
    }
}
