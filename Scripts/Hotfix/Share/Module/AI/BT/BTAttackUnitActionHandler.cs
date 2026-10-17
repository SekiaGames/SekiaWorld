namespace ET
{
    public class BTAttackUnitActionHandler: ABTHandler<BTAttackUnitAction>
    {
        protected override int Run(BTAttackUnitAction node, BTEnv env)
        {
            if (!env.TryGetEntity<Unit>("Unit", out Unit unit) || unit == null)
            {
                return 1;
            }

            if (!env.TryGetEntity<Unit>("Target", out Unit target) || target == null)
            {
                return 2;
            }

            NumericComponent numericComponent = target.NumericComponent;
            if (numericComponent == null)
            {
                return 3;
            }

            int hp = numericComponent.GetAsInt(NumericType.Hp);
            if (hp <= 0)
            {
                return 4;
            }

            numericComponent.Set(NumericType.Hp, hp - node.Damage);
            return 0;
        }
    }
}
