using Godot;

namespace ET
{
    [EntitySystemOf(typeof(GlobalComponent))]
    [FriendOf(typeof(GlobalComponent))]
    public static partial class GlobalComponentSystem
    {
        [EntitySystem]
        public static void Awake(this GlobalComponent self)
        {
            self.Global = GlobalComponent.RootNode;
            self.Unit = GlobalComponent.RootNode.GetNode<Node3D>("UnitRoot");
            self.UI = GlobalComponent.RootNode.GetNode<Node2D>("UIRoot");
        }
    }

    [ComponentOf(typeof(Scene))]
    public class GlobalComponent: Entity, IAwake
    {
        /// <summary>
        /// Init 场景的 Global 节点，即 Init.tscn 的根节点。
        ///
        /// 由顶层 SekiaWorld 程序集的 Init._Ready() 反向注入：
        /// 本层（Loader，AOT 固定层）在启动工程之下，引不到 Init 的类型，
        /// 所以用静态字段接住，而不是写 Init.Instance。
        /// </summary>
        public static Node RootNode;

        public Node Global;
        public Node3D Unit;
        public Node2D UI;
    }
}
