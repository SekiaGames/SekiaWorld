namespace ET.Client
{
    [Event(SceneType.Demo)]
    public class LoginFinish_EnterCharacterChoose: AEvent<Scene, LoginFinish>
    {
        protected override async ETTask Run(Scene scene, LoginFinish args)
        {
            EntityRef<Scene> sceneRef = scene;

            CharacterChooseComponent chooseCharacter = scene.GetComponent<CharacterChooseComponent>();
            EntityRef<CharacterChooseComponent> chooseCharacterRef = chooseCharacter;

            bool ready = false;
            if (chooseCharacter != null)
            {
                ready = await chooseCharacter.WaitSceneAsync();
            }

            scene = sceneRef;
            chooseCharacter = chooseCharacterRef;
            if (scene == null)
            {
                return;
            }

            await UIHelper.Remove(scene, UIType.UILogin);

            chooseCharacter = chooseCharacterRef;

            if (chooseCharacter == null)
            {
                Log.Warning("[选角] 没有 CharacterChooseComponent，只收掉登录界面，不激活选角");
                return;
            }

            if (!ready)
            {
                Log.Error("[选角] 场景没能就位，登录界面已收掉但选角模块没激活");
                return;
            }

            chooseCharacter.Activate();

            scene = sceneRef;
            if (scene == null)
            {
                return;
            }
            await UIHelper.Create(scene, UIType.UIViewRolesList, UILayer.Mid);
        }
    }
}
