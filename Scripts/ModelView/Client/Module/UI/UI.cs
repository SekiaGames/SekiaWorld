using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(UI))]
    [FriendOf(typeof(UI))]
    public static partial class UISystem
    {
        [EntitySystem]
        private static void Awake(this UI self, string name, Node node)
        {
            self.nameChildren.Clear();
            self.Name = name;
            self.GameObject = node;
        }

        [EntitySystem]
        private static void Destroy(this UI self)
        {
            foreach (UI ui in self.nameChildren.Values)
            {
                ui.Dispose();
            }

            self.GameObject.QueueFree();
            self.nameChildren.Clear();
        }

        public static void SetAsFirstSibling(this UI self)
        {
            Node parent = self.GameObject.GetParent();
            if (parent != null)
            {
                parent.MoveChild(self.GameObject, 0);
            }
        }

        public static void Add(this UI self, UI ui)
        {
            self.nameChildren.Add(ui.Name, ui);
        }

        public static void Remove(this UI self, string name)
        {
            EntityRef<UI> uiRef;
            if (!self.nameChildren.Remove(name, out uiRef))
            {
                return;
            }

            UI ui = uiRef;
            ui?.Dispose();
        }

        public static UI Get(this UI self, string name)
        {
            EntityRef<UI> uiRef;
            if (self.nameChildren.TryGetValue(name, out uiRef))
            {
                return uiRef;
            }

            Node childNode = self.GameObject.GetNodeOrNull<Node>(name);
            if (childNode == null)
            {
                return null;
            }

            UI child = self.AddChild<UI, string, Node>(name, childNode);
            self.Add(child);
            return child;
        }
    }

    [ChildOf()]
    public sealed class UI: Entity, IAwake<string, Node>, IDestroy
    {
        public Node GameObject { get; set; }

        public string Name { get; set; }

        public Dictionary<string, EntityRef<UI>> nameChildren = new();
    }
}
