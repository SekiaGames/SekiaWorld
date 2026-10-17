using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ET
{
    public static class BTBuilder
    {
        public static BTNode Build(IReadOnlyList<BTNodeData> rows, int treeId)
        {
            Dictionary<int, BTNode> nodes = new();
            BTNode root = null;

            foreach (BTNodeData row in rows)
            {
                if (row.TreeId != treeId)
                {
                    continue;
                }

                Type type = GetNodeType(row.NodeType);
                BTNode node = (BTNode)Activator.CreateInstance(type);
                node.Id = row.NodeId;

                if (!nodes.TryAdd(row.NodeId, node))
                {
                    throw new Exception($"bt config duplicate nodeId: tree {treeId} node {row.NodeId}");
                }

                if (row.ParentId == 0)
                {
                    if (root != null)
                    {
                        throw new Exception($"bt config has multiple roots: tree {treeId} node {row.NodeId}");
                    }

                    root = node;
                }
            }

            if (root == null)
            {
                throw new Exception($"bt config root not found: tree {treeId}");
            }

            List<BTNodeData> sorted = new(rows);
            sorted.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (BTNodeData row in sorted)
            {
                if (row.TreeId != treeId || row.ParentId == 0)
                {
                    continue;
                }

                if (!nodes.TryGetValue(row.ParentId, out BTNode parent))
                {
                    throw new Exception($"bt config parent not found: tree {treeId} node {row.NodeId} parent {row.ParentId}");
                }

                BTNode node = nodes[row.NodeId];
                ApplyParams(node, row.Params);
                parent.Children.Add(node);
            }

            ApplyParams(root, FindRootParams(rows, treeId));
            return root;
        }

        private static string FindRootParams(IReadOnlyList<BTNodeData> rows, int treeId)
        {
            foreach (BTNodeData row in rows)
            {
                if (row.TreeId == treeId && row.ParentId == 0)
                {
                    return row.Params;
                }
            }

            return null;
        }

        public static void ApplyParams(BTNode node, string paramsText)
        {
            if (string.IsNullOrWhiteSpace(paramsText))
            {
                return;
            }

            Type type = node.GetType();
            string[] items = paramsText.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {
                string[] kv = item.Split('=', 2);
                if (kv.Length != 2)
                {
                    throw new Exception($"bt params format error: {type.Name} {item}, expect key=value");
                }

                string key = kv[0].Trim();
                string value = kv[1].Trim();

                FieldInfo field = type.GetField(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field == null)
                {
                    throw new Exception($"bt params field not found: {type.FullName} {key}");
                }

                field.SetValue(node, ConvertValue(type, field.FieldType, value));
            }
        }

        private static object ConvertValue(Type nodeType, Type fieldType, string value)
        {
            try
            {
                if (fieldType == typeof (int))
                {
                    return int.Parse(value, CultureInfo.InvariantCulture);
                }

                if (fieldType == typeof (float))
                {
                    return float.Parse(value, CultureInfo.InvariantCulture);
                }

                if (fieldType == typeof (long))
                {
                    return long.Parse(value, CultureInfo.InvariantCulture);
                }

                if (fieldType == typeof (double))
                {
                    return double.Parse(value, CultureInfo.InvariantCulture);
                }

                if (fieldType == typeof (bool))
                {
                    return bool.Parse(value);
                }

                if (fieldType == typeof (string))
                {
                    return value;
                }
            }
            catch (FormatException e)
            {
                throw new Exception($"bt params convert fail: {nodeType.FullName} {fieldType.Name} = {value}", e);
            }

            throw new Exception($"bt params type not supported: {nodeType.FullName} {fieldType.Name}");
        }

        public static Type GetNodeType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new Exception("bt node type is empty");
            }

            Dictionary<string, Type> types = BTNodeMeta.GetTypes();
            if (types.TryGetValue(typeName, out Type type))
            {
                return CheckNodeType(typeName, type);
            }

            Type match = null;
            foreach (Type t in types.Values)
            {
                if (t.Name != typeName)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new Exception($"bt node type ambiguous: {typeName}, use full name");
                }

                match = t;
            }

            if (match == null)
            {
                throw new Exception($"bt node type not found: {typeName}");
            }

            return CheckNodeType(typeName, match);
        }

        private static Type CheckNodeType(string typeName, Type type)
        {
            if (!typeof (BTNode).IsAssignableFrom(type) || type.IsAbstract)
            {
                throw new Exception($"bt node type invalid: {typeName} {type.FullName}");
            }

            return type;
        }
    }
}
