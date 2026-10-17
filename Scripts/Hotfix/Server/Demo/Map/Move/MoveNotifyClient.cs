using Unity.Mathematics;

namespace ET.Server
{
    [Event(SceneType.Gate)]
    [FriendOf(typeof(MoveComponent))]
    public class MoveStart_NotifyClient: AEvent<Scene, MoveStart>
    {
        protected override async ETTask Run(Scene scene, MoveStart args)
        {
            Unit unit = args.Unit;
            if (unit.GetComponent<AIComponent>() == null)
            {
                return;
            }

            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            if (moveComponent == null || moveComponent.Targets.Count < 2)
            {
                return;
            }

            M2C_PathfindingResult message = M2C_PathfindingResult.Create();
            message.Id = unit.Id;
            message.Position = moveComponent.Targets[moveComponent.Targets.Count - 1];
            message.Points.AddRange(moveComponent.Targets);
            MapMessageHelper.Broadcast(unit, message);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Gate)]
    public class MoveStop_NotifyClient: AEvent<Scene, MoveStop>
    {
        protected override async ETTask Run(Scene scene, MoveStop args)
        {
            Unit unit = args.Unit;
            if (unit.GetComponent<AIComponent>() == null)
            {
                return;
            }

            M2C_Stop message = M2C_Stop.Create();
            message.Error = 0;
            message.Id = unit.Id;
            message.Position = unit.Position;
            message.Rotation = unit.Rotation;
            MapMessageHelper.Broadcast(unit, message);
            await ETTask.CompletedTask;
        }
    }
}
