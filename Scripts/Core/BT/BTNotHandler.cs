namespace ET
{
    public class BTNotHandler: ABTHandler<BTNot>
    {
        protected override int Run(BTNot node, BTEnv env)
        {
            if (node.Children == null || node.Children.Count == 0)
            {
                return node.ErrorCode;
            }

            int ret = BTDispatcher.Instance.Handle(node.Children[0], env);
            return ret == 0 ? node.ErrorCode : 0;
        }
    }
}
