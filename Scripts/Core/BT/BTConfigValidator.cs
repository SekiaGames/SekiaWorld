using System;
using System.Collections.Generic;

namespace ET
{
    public sealed class BTConfigError
    {
        public int TreeId;

        public int NodeId;

        public string Message;

        public override string ToString()
        {
            return this.NodeId == 0? $"树{this.TreeId}: {this.Message}" : $"树{this.TreeId} 节点{this.NodeId}: {this.Message}";
        }
    }

    public static class BTConfigValidator
    {
        public static List<BTConfigError> Validate(IReadOnlyList<BTNodeData> rows)
        {
            List<BTConfigError> errors = new();

            Dictionary<int, List<BTNodeData>> byTree = new();
            foreach (BTNodeData row in rows)
            {
                if (!byTree.TryGetValue(row.TreeId, out List<BTNodeData> treeRows))
                {
                    treeRows = new List<BTNodeData>();
                    byTree.Add(row.TreeId, treeRows);
                }

                treeRows.Add(row);
            }

            foreach ((int treeId, List<BTNodeData> treeRows) in byTree)
            {
                ValidateTree(treeId, treeRows, errors);
            }

            if (byTree.Count == 0)
            {
                errors.Add(new BTConfigError { Message = "表里没有任何节点" });
            }

            return errors;
        }

        private static void ValidateTree(int treeId, List<BTNodeData> rows, List<BTConfigError> errors)
        {
            Dictionary<int, BTNodeData> byNode = new();
            List<BTNodeData> roots = new();
            foreach (BTNodeData row in rows)
            {
                if (row.NodeId == 0)
                {
                    errors.Add(new BTConfigError { TreeId = treeId, Message = "节点Id 不能为 0" });
                    continue;
                }

                if (!byNode.TryAdd(row.NodeId, row))
                {
                    errors.Add(new BTConfigError { TreeId = treeId, NodeId = row.NodeId, Message = "节点Id 重复" });
                    continue;
                }

                if (row.ParentId == 0)
                {
                    roots.Add(row);
                }
            }

            if (roots.Count == 0)
            {
                errors.Add(new BTConfigError { TreeId = treeId, Message = "缺少根节点(父节点Id=0)" });
            }
            else if (roots.Count > 1)
            {
                foreach (BTNodeData root in roots)
                {
                    errors.Add(new BTConfigError { TreeId = treeId, NodeId = root.NodeId, Message = "出现多个根节点" });
                }
            }

            Dictionary<int, HashSet<int>> siblingOrders = new();
            foreach (BTNodeData row in rows)
            {
                if (row.ParentId != 0 && !byNode.ContainsKey(row.ParentId))
                {
                    errors.Add(new BTConfigError { TreeId = treeId, NodeId = row.NodeId, Message = $"父节点 {row.ParentId} 不存在" });
                }

                if (!siblingOrders.TryGetValue(row.ParentId, out HashSet<int> orders))
                {
                    orders = new HashSet<int>();
                    siblingOrders.Add(row.ParentId, orders);
                }

                if (!orders.Add(row.Order))
                {
                    errors.Add(new BTConfigError { TreeId = treeId, NodeId = row.NodeId, Message = $"同级顺序 {row.Order} 重复(父节点 {row.ParentId})" });
                }
            }

            foreach (BTNodeData row in rows)
            {
                try
                {
                    Type type = BTBuilder.GetNodeType(row.NodeType);
                    BTNode node = (BTNode)Activator.CreateInstance(type);
                    BTBuilder.ApplyParams(node, row.Params);
                }
                catch (Exception e)
                {
                    errors.Add(new BTConfigError { TreeId = treeId, NodeId = row.NodeId, Message = e.Message });
                }
            }

            if (roots.Count == 1)
            {
                Dictionary<int, List<int>> children = new();
                foreach (BTNodeData row in rows)
                {
                    if (row.ParentId == 0)
                    {
                        continue;
                    }

                    if (!children.TryGetValue(row.ParentId, out List<int> list))
                    {
                        list = new List<int>();
                        children.Add(row.ParentId, list);
                    }

                    list.Add(row.NodeId);
                }

                HashSet<int> visited = new();
                Stack<int> stack = new();
                stack.Push(roots[0].NodeId);
                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (!visited.Add(nodeId))
                    {
                        continue;
                    }

                    if (children.TryGetValue(nodeId, out List<int> list))
                    {
                        foreach (int child in list)
                        {
                            stack.Push(child);
                        }
                    }
                }

                foreach (BTNodeData row in rows)
                {
                    if (!visited.Contains(row.NodeId))
                    {
                        errors.Add(new BTConfigError { TreeId = treeId, NodeId = row.NodeId, Message = "节点未连到根(可能成环或父链断开)" });
                    }
                }
            }
        }
    }
}
