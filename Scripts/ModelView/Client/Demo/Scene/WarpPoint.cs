using Godot;

namespace ET.Client
{
    [EnableClass]
    public sealed class WarpPoint
    {
        public int TargetMapId;

        public int TargetBornPoint;

        public Vector3 Position;

        public float Radius;
    }
}
