using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class UIGlobalComponent: Entity, IAwake
    {
        public Dictionary<int, Node> UILayers = new();
    }
}
