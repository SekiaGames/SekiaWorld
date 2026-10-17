namespace ET
{
    public class BTSequenceHandler: ABTHandler<BTSequence>
    {
        protected override int Run(BTSequence node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children)
            {
                int ret = BTDispatcher.Instance.Handle(subNode, env);
                if (ret != 0)
                {
                    return ret;
                }
            }

            return 0;
        }
    }
}
