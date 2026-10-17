namespace ET
{
    public class BTMoveArrivedHandler: ABTHandler<BTMoveArrived>
    {
        protected override int Run(BTMoveArrived node, BTEnv env)
        {
            if (!env.TryGetEntity<Unit>("Unit", out Unit unit) || unit == null)
            {
                return 1;
            }

            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            if (moveComponent == null)
            {
                return 2;
            }

            return moveComponent.IsArrived()? 0 : 3;
        }
    }
}
