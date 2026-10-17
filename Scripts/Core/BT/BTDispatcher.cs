using System;
using System.Collections.Generic;

namespace ET
{
    public struct BTRunTreeEvent
    {
        public BTNode Root;
        public BTEnv Env;
    }

    public class BTHandlerAttribute: BaseAttribute
    {
    }

    public interface IBTHandler
    {
        int Handle(BTNode node, BTEnv env);

        Type GetNodeType();
    }

    public class BTDispatcher: Singleton<BTDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, IBTHandler> btHandlers = new();

        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (BTHandlerAttribute));

            foreach (Type type in types)
            {
                this.Register(type);
            }
        }

        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);
            if (obj is not IBTHandler ibtHandler)
            {
                throw new Exception($"bt handler not implement IBTHandler: {obj.GetType().FullName}");
            }

            Type nodeType = ibtHandler.GetNodeType();
            if (!this.btHandlers.TryAdd(nodeType, ibtHandler))
            {
                throw new Exception($"duplicate bt handler: {nodeType.FullName}");
            }
        }

        public int Handle(BTNode node, BTEnv env)
        {
            if (node == null)
            {
                throw new Exception("bt node is null");
            }

            if (!this.btHandlers.TryGetValue(node.GetType(), out IBTHandler ibtHandler))
            {
                throw new Exception($"not found bt handler: {node.GetType().FullName}");
            }

            return ibtHandler.Handle(node, env);
        }
    }
}
