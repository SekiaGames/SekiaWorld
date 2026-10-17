using System;
using Godot;

namespace ET.Client
{
    [Event(SceneType.Demo)]
    public class SceneChangeStart_AddComponent: AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            try
            {
                Scene currentScene = root.CurrentScene();
                EntityRef<Scene> rootRef = root;

                ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();

                string mapPath = MapSceneHelper.Resolve(currentScene.Name);
                Log.Info($"[进图] 加载地图场景：{mapPath}");

                EntityRef<Scene> currentSceneRef = currentScene;
                Node mapNode = await resourcesLoaderComponent.LoadSceneAsync(mapPath);
                currentScene = currentSceneRef;
                if (currentScene == null)
                {
                    return;
                }

                int groundColliders = MapGroundCollision.Ensure(mapNode);
                Log.Info($"[地面碰撞] {currentScene.Name}：新增 {groundColliders} 个可行走网格的 trimesh 碰撞"
                        + "（点地寻路靠它；0 = 该图的可行走网格都已带碰撞体）");

                int warps = MapWarp.Setup(mapNode, currentScene.Name);
                if (warps > 0 && currentScene.GetComponent<WarpComponent>() == null)
                {
                    currentScene.AddComponent<WarpComponent>();
                }

                Scene mainScene = rootRef;
                if (mainScene == null)
                {
                    return;
                }

                MainCameraComponent mainCamera = mainScene.GetComponent<MainCameraComponent>();
                if (mainCamera == null)
                {
                    Log.Error("[进图] 没拿到 MainCameraComponent（全局主相机），画面会没有相机");
                }
                else
                {
                    mainCamera.Activate(currentScene.Name);
                }

                currentScene.AddComponent<OperaComponent>();

                await UIHelper.Remove(mainScene, UIType.MainView);
                mainScene = rootRef;
                if (mainScene == null)
                {
                    return;
                }

                await UIHelper.Create(mainScene, UIType.MainView, UILayer.Low);
                Log.Info("[进图] 主界面 MainView 已打开");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
