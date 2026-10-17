namespace ET.Client
{
    [Event(SceneType.Main)]
    public class EntryEvent3_InitClient: AEvent<Scene, EntryEvent3>
    {
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            root.AddComponent<GlobalComponent>();
            root.AddComponent<UIGlobalComponent>();
            root.AddComponent<UIComponent>();
            root.AddComponent<ResourcesLoaderComponent>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<CurrentScenesComponent>();

            root.AddComponent<MainCameraComponent>();

            root.SceneType = SceneType.Demo;

            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
    }
}
