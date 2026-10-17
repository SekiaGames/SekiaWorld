using Unity.Mathematics;

namespace ET
{
    public class BTUnitInRangeCheckHandler: ABTHandler<BTUnitInRangeCheck>
    {
        protected override int Run(BTUnitInRangeCheck node, BTEnv env)
        {
            if (!env.TryGetEntity<Unit>("Unit", out Unit unit) || unit == null)
            {
                return 1;
            }

            if (!env.TryGetEntity<Unit>("Target", out Unit target) || target == null)
            {
                return 2;
            }

            float3 offset = target.Position - unit.Position;
            offset.y = 0;

            return math.lengthsq(offset) <= node.Range * node.Range? 0 : 3;
        }
    }
}
