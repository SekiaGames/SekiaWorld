using Godot;

namespace ET.Client
{
    public static class CharacterChooseHelper
    {
        private const string Tag = "[回选角]";

        public static async ETTask ReturnAsync(Scene scene)
        {
            if (scene == null)
            {
                return;
            }

            EntityRef<Scene> sceneRef = scene;

            Log.Info($"{Tag} 开始：收界面 → 卸地图与当前场景 → 重载选角场景 → 开选角界面");

            await UIHelper.Remove(scene, UIType.SetView);
            scene = sceneRef;
            if (scene == null)
            {
                return;
            }

            await UIHelper.Remove(scene, UIType.MainView);
            scene = sceneRef;
            if (scene == null)
            {
                return;
            }

            CurrentScenesComponent currentScenes = scene.GetComponent<CurrentScenesComponent>();
            Scene currentScene = currentScenes?.Scene;
            if (currentScene != null)
            {
                string mapPath = MapSceneHelper.Resolve(currentScene.Name);

                ResourcesLoaderComponent loader = currentScene.GetComponent<ResourcesLoaderComponent>();
                if (loader != null)
                {
                    loader.UnloadScene(mapPath);
                }

                currentScenes.Scene = null;
                currentScene.Dispose();

                scene.GetComponent<MainCameraComponent>()?.Park();

                Log.Info($"{Tag} 已卸掉地图与当前场景：{mapPath}");

                scene = sceneRef;
                if (scene == null)
                {
                    return;
                }
            }

            CharacterChooseComponent choose = scene.GetComponent<CharacterChooseComponent>();
            if (choose == null)
            {
                Log.Error($"{Tag} 没有 CharacterChooseComponent（选角模块），回不去");
                return;
            }

            EntityRef<CharacterChooseComponent> chooseRef = choose;
            bool ready = await choose.WaitSceneAsync();

            scene = sceneRef;
            choose = chooseRef;
            if (scene == null || choose == null)
            {
                return;
            }

            if (!ready)
            {
                Log.Error($"{Tag} 选角场景没能就位，界面照开（3D 部分会是黑的）");
            }

            choose.Activate();

            await UIHelper.Create(scene, UIType.UIViewRolesList, UILayer.Mid);
            Log.Info($"{Tag} 已回到角色选择界面");
        }
    }
}
