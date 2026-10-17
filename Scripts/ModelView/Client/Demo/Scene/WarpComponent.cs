using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class WarpComponent: Entity, IAwake, IUpdate
    {
        public List<WarpPoint> Warps = new();

        public bool Firing;

        public bool Armed;
    }
}
