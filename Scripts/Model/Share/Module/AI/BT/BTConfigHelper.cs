using System.Collections.Generic;

namespace ET
{
    public static class BTConfigHelper
    {
        public static bool HasTree(int treeId)
        {
            foreach (BTConfig config in BTConfigCategory.Instance.GetAll().Values)
            {
                if (config.TreeId == treeId)
                {
                    return true;
                }
            }

            return false;
        }

        public static BTNode Build(int treeId)
        {
            List<BTNodeData> rows = new();
            foreach (BTConfig config in BTConfigCategory.Instance.GetAll().Values)
            {
                if (config.TreeId != treeId)
                {
                    continue;
                }

                rows.Add(new BTNodeData
                {
                    TreeId = config.TreeId,
                    NodeId = config.NodeId,
                    ParentId = config.ParentId,
                    Order = config.Order,
                    NodeType = config.NodeType,
                    Params = config.Params,
                });
            }

            return BTBuilder.Build(rows, treeId);
        }
    }
}
