using Godot;

namespace ET.Client
{
    [UIEvent(UIType.UILogin)]
    public class UILoginEvent: AUIEvent
    {
        private const string LoginScenePath = "res://Resources/GUI/v1/view/StartGamePanel.tscn";

        public override async ETTask<UI> OnCreate(UIComponent uiComponent, UILayer uiLayer)
        {
            string assetsName = LoginScenePath;
            EntityRef<UIComponent> uiComponentRef = uiComponent;
            PackedScene prefab = await uiComponent.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<PackedScene>(assetsName);
            uiComponent = uiComponentRef;
            if (uiComponent == null)
            {
                return null;
            }

            Control control = prefab.Instantiate<Control>();
            uiComponent.UIGlobalComponent.GetLayer((int)uiLayer).AddChild(control);

            UI ui = uiComponent.AddChild<UI, string, Node>(UIType.UILogin, control);
            ui.AddComponent<UILoginComponent>();
            return ui;
        }

        public override void OnRemove(UIComponent uiComponent)
        {
        }
    }
}
