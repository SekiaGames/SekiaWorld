using Godot;

namespace ET.Client
{
    public static class CharacterSelectHelper
    {
        public static async ETTask EnterAsync(Scene scene)
        {
            EntityRef<Scene> sceneRef = scene;

            CharacterSelectComponent select = scene.GetComponent<CharacterSelectComponent>();
            if (select == null)
            {
                select = scene.AddComponent<CharacterSelectComponent>();
            }

            bool ready = await select.WaitSceneAsync();

            scene = sceneRef;
            if (scene == null)
            {
                return;
            }

            select = scene.GetComponent<CharacterSelectComponent>();
            if (select == null)
            {
                return;
            }

            scene.GetComponent<CharacterChooseComponent>()?.Deactivate();

            if (!ready)
            {
                Log.Error("[创角] 场景没能就位，界面照开（3D 部分会是黑的）");
            }
            select.Activate();

            await UIHelper.Remove(scene, UIType.UIViewRolesList);

            scene = sceneRef;
            if (scene == null)
            {
                return;
            }
            await UIHelper.Create(scene, UIType.CreateRoleViewV2, UILayer.Mid);
        }

        public static async ETTask LeaveAsync(Scene scene)
        {
            EntityRef<Scene> sceneRef = scene;

            scene.GetComponent<CharacterSelectComponent>()?.Deactivate();

            await UIHelper.Remove(scene, UIType.CreateRoleViewV2);

            scene = sceneRef;
            if (scene == null)
            {
                return;
            }

            scene.GetComponent<CharacterChooseComponent>()?.Activate();

            await UIHelper.Create(scene, UIType.UIViewRolesList, UILayer.Mid);
        }
    }
}
