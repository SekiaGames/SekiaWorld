namespace ET
{
    public class BTChanceHandler: ABTHandler<BTChance>
    {
        protected override int Run(BTChance node, BTEnv env)
        {
            if (node.Percent <= 0)
            {
                return node.FailCode;
            }

            // RandomNumber 含下限、不含上限 ⇒ [0, 100)
            if (node.Percent < 100 && RandomGenerator.RandomNumber(0, 100) >= node.Percent)
            {
                return node.FailCode;
            }

            if (node.Children == null || node.Children.Count == 0)
            {
                return 0;
            }

            return BTDispatcher.Instance.Handle(node.Children[0], env);
        }
    }
}
