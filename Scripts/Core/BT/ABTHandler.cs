using System;

namespace ET
{
    [BTHandler]
    public abstract class ABTHandler<Node>: Object, IBTHandler where Node: BTNode
    {
        protected abstract int Run(Node node, BTEnv env);

        public virtual int Handle(BTNode node, BTEnv env)
        {
            if (node is not Node c)
            {
                throw new Exception($"bt handler node type mismatch: {node.GetType().FullName} to {typeof (Node).Name}");
            }

            if (BTDebug.Enabled)
            {
                env.AddRunPath(node.Id);
            }

            return this.Run(c, env);
        }

        public virtual Type GetNodeType()
        {
            return typeof (Node);
        }
    }
}
