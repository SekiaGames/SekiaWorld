using System;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIGlobalComponent))]
    [FriendOf(typeof(UIGlobalComponent))]
    public static partial class UIGlobalComponentSystem
    {
        [EntitySystem]
        public static void Awake(this UIGlobalComponent self)
        {
            Node uiRoot = GlobalComponent.RootNode.GetNode("UIRoot");

            self.UILayers.Add((int)UILayer.Hidden, uiRoot.GetNode<CanvasLayer>(UILayer.Hidden.ToString()));
            self.UILayers.Add((int)UILayer.Low, uiRoot.GetNode<CanvasLayer>(UILayer.Low.ToString()));
            self.UILayers.Add((int)UILayer.Mid, uiRoot.GetNode<CanvasLayer>(UILayer.Mid.ToString()));
            self.UILayers.Add((int)UILayer.High, uiRoot.GetNode<CanvasLayer>(UILayer.High.ToString()));
        }

        public static async ETTask<UI> OnCreate(this UIGlobalComponent self, UIComponent uiComponent, string uiType, UILayer uiLayer)
        {
            try
            {
                UI ui = await UIEventComponent.Instance.UIEvents[uiType].OnCreate(uiComponent, uiLayer);
                return ui;
            }
            catch (Exception e)
            {
                throw new Exception($"on create ui error: {uiType}", e);
            }
        }

        public static Node GetLayer(this UIGlobalComponent self, int layer)
        {
            return self.UILayers[layer];
        }

        public static void OnRemove(this UIGlobalComponent self, UIComponent uiComponent, string uiType)
        {
            try
            {
                UIEventComponent.Instance.UIEvents[uiType].OnRemove(uiComponent);
            }
            catch (Exception e)
            {
                throw new Exception($"on remove ui error: {uiType}", e);
            }
        }
    }
}
