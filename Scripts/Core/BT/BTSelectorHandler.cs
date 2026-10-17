namespace ET
{
    public class BTSelectorHandler: ABTHandler<BTSelector>
    {
        protected override int Run(BTSelector node, BTEnv env)
        {
            int firstFailRet = -1;
            bool hasFailRet = false;
            foreach (BTNode subNode in node.Children)
            {
                int ret = BTDispatcher.Instance.Handle(subNode, env);
                if (ret == 0)
                {
                    return ret;
                }

                if (!hasFailRet)
                {
                    firstFailRet = ret;
                    hasFailRet = true;
                }
            }

            return firstFailRet;
        }
    }
}
