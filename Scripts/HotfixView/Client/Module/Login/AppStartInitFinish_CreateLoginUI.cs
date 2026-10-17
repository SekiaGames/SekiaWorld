namespace ET.Client
{
    [Event(SceneType.Demo)]
    public class AppStartInitFinish_CreateLoginUI: AEvent<Scene, AppStartInitFinish>
    {
        protected override async ETTask Run(Scene root, AppStartInitFinish args)
        {
            EntityRef<Scene> rootRef = root;

            await UIHelper.Create(root, UIType.UILogin, UILayer.Mid);

            root = rootRef;
            if (root == null)
            {
                return;
            }

            root.AddComponent<CharacterChooseComponent>().StartLoad();
        }
    }
}
