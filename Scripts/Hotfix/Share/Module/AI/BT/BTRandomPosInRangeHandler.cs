using Unity.Mathematics;

namespace ET
{
    public class BTRandomPosInRangeHandler: ABTHandler<BTRandomPosInRange>
    {
        protected override int Run(BTRandomPosInRange node, BTEnv env)
        {
            if (!env.TryGetStruct<float3>(node.HomeKey, out float3 home))
            {
                return 1;
            }

            if (node.Radius <= 0f)
            {
                return 2;
            }

            // 圆内均匀取点：半径取 sqrt(u) —— 直接取 u 会让点往圆心堆。
            float angle = RandomGenerator.RandFloat01() * math.PI * 2f;
            float radius = node.Radius * math.sqrt(RandomGenerator.RandFloat01());
            float3 pos = home + new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);

            env.AddStruct(node.Key, pos);
            return 0;
        }
    }
}
