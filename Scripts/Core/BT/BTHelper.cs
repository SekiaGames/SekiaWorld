namespace ET
{
    public static class BTHelper
    {
        public static int RunTree(BTNode node, BTEnv env)
        {
            int ret = BTDispatcher.Instance.Handle(node, env);

            if (BTDebug.Enabled)
            {
                Scene scene = env.Scene;
                if (scene != null)
                {
                    EventSystem.Instance.Publish(scene, new BTRunTreeEvent() { Root = node, Env = env });
                }
            }

            return ret;
        }
    }
}
