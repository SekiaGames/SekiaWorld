using Godot;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangeRotation_SyncGameObjectRotation: AEvent<Scene, ChangeRotation>
    {
        protected override async ETTask Run(Scene scene, ChangeRotation args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }

            var rotation = unit.Rotation;
            gameObjectComponent.GameObject.Quaternion = new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
            await ETTask.CompletedTask;
        }
    }
}
