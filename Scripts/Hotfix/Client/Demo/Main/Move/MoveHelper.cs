using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    public static partial class MoveHelper
    {
        public static async ETTask MoveToAsync(this Unit unit, List<float3> path)
        {
            float speed = unit.NumericComponent.GetAsFloat(NumericType.Speed);
            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            await moveComponent.MoveToAsync(path, speed);
        }
    }
}
