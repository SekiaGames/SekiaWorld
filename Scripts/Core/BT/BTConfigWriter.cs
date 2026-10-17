using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ET
{
    public static class BTConfigWriter
    {
        public static List<BTNodeData> Write(BTNode root, int treeId)
        {
            HashSet<int> used = new();
            int maxId = 0;
            CollectIds(root, used, ref maxId);
            int nextId = maxId + 1;

            List<BTNodeData> rows = new();
            Walk(root, 0, 1, rows, used, ref nextId, treeId);
            return rows;
        }

        private static void Walk(BTNode node, int parentId, int order, List<BTNodeData> rows,
                HashSet<int> used, ref int nextId, int treeId)
        {
            int nodeId = node.Id;
            if (nodeId <= 0)
            {
                while (used.Contains(nextId))
                {
                    ++nextId;
                }

                nodeId = nextId++;
                node.Id = nodeId;
            }

            rows.Add(new BTNodeData
            {
                TreeId = treeId,
                NodeId = nodeId,
                ParentId = parentId,
                Order = order,
                NodeType = node.GetType().FullName,
                Params = SerializeParams(node),
            });

            if (node.Children == null)
            {
                return;
            }

            int childOrder = 1;
            foreach (BTNode child in node.Children)
            {
                Walk(child, nodeId, childOrder, rows, used, ref nextId, treeId);
                ++childOrder;
            }
        }

        private static void CollectIds(BTNode node, HashSet<int> used, ref int maxId)
        {
            if (node.Id > 0)
            {
                used.Add(node.Id);
                if (node.Id > maxId)
                {
                    maxId = node.Id;
                }
            }

            if (node.Children == null)
            {
                return;
            }

            foreach (BTNode child in node.Children)
            {
                CollectIds(child, used, ref maxId);
            }
        }

        public static string SerializeParams(BTNode node)
        {
            List<string> items = new();
            Type type = node.GetType();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                object value = field.GetValue(node);
                object defaultValue = field.FieldType.IsValueType? Activator.CreateInstance(field.FieldType) : null;
                if (Equals(value, defaultValue))
                {
                    continue;
                }

                string text = FormatValue(type, value);
                if (text == null)
                {
                    continue;
                }

                items.Add($"{field.Name}={text}");
            }

            return string.Join(";", items);
        }

        private static string FormatValue(Type nodeType, object value)
        {
            if (value is int intValue)
            {
                return intValue.ToString(CultureInfo.InvariantCulture);
            }

            if (value is long longValue)
            {
                return longValue.ToString(CultureInfo.InvariantCulture);
            }

            if (value is float floatValue)
            {
                return floatValue.ToString(CultureInfo.InvariantCulture);
            }

            if (value is double doubleValue)
            {
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }

            if (value is bool boolValue)
            {
                return boolValue? "true" : "false";
            }

            if (value is string stringValue)
            {
                return stringValue;
            }

            return null;
        }
    }
}
