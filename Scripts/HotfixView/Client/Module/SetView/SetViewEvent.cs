using Godot;

namespace ET.Client
{
    [UIEvent(UIType.SetView)]
    public class SetViewEvent: AUIEvent
    {
        private const string ScenePath = "res://Resources/GUI/v1/view/SetView.tscn";

        public override async ETTask<UI> OnCreate(UIComponent uiComponent, UILayer uiLayer)
        {
            EntityRef<UIComponent> uiComponentRef = uiComponent;
            PackedScene prefab = await uiComponent.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<PackedScene>(ScenePath);
            uiComponent = uiComponentRef;
            if (uiComponent == null)
            {
                return null;
            }

            Control control = prefab.Instantiate<Control>();
            uiComponent.UIGlobalComponent.GetLayer((int)uiLayer).AddChild(control);

            UI ui = uiComponent.AddChild<UI, string, Node>(UIType.SetView, control);
            ui.AddComponent<UISetViewComponent>();
            return ui;
        }

        public override void OnRemove(UIComponent uiComponent)
        {
        }
    }
}
