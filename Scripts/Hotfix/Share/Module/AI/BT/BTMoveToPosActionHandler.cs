using Unity.Mathematics;

namespace ET
{
    public class BTMoveToPosActionHandler: ABTHandler<BTMoveToPosAction>
    {
        protected override int Run(BTMoveToPosAction node, BTEnv env)
        {
            if (!env.TryGetEntity<Unit>("Unit", out Unit unit) || unit == null)
            {
                return 1;
            }

            if (!env.TryGetStruct<float3>("TargetPos", out float3 targetPos))
            {
                return 2;
            }

            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            if (moveComponent == null)
            {
                return 3;
            }

            if (math.distance(unit.Position, targetPos) <= 0.1f)
            {
                return 0;
            }

            if (!moveComponent.IsArrived())
            {
                return 0;
            }

            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            if (pathfindingComponent == null)
            {
                return 4;
            }

            using ListComponent<float3> path = ListComponent<float3>.Create();
            pathfindingComponent.Find(unit.Position, targetPos, path);
            if (path.Count == 0)
            {
                return 5;
            }

            if (math.distance(path[0], unit.Position) > 0.01f)
            {
                path.Insert(0, unit.Position);
            }

            if (path.Count == 1)
            {
                path.Add(targetPos);
            }

            if (math.distance(path[path.Count - 1], unit.Position) <= 0.1f)
            {
                return 0;
            }

            // Log.Info($"[BT AI] move unit={unit.Id} path={path.Count} from={unit.Position} to={targetPos}");

            float speed = node.Speed;
            NumericComponent numericComponent = unit.NumericComponent;
            if (numericComponent != null)
            {
                float numericSpeed = numericComponent.GetAsFloat(NumericType.Speed);
                if (numericSpeed > 0)
                {
                    speed = numericSpeed;
                }
            }

            moveComponent.MoveToAsync(path, speed).Coroutine();
            return 0;
        }
    }
}
