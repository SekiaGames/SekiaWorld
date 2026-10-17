using System;
using System.Collections.Generic;
using System.Text;

namespace ET
{
    public static class BTTreePrinter
    {
        public static string Print(BTNode root)
        {
            StringBuilder sb = new();
            Walk(root, 0, sb);
            return sb.ToString();
        }

        public static string Print(IReadOnlyList<BTNodeData> rows, int treeId)
        {
            return Print(BTBuilder.Build(rows, treeId));
        }

        private static void Walk(BTNode node, int depth, StringBuilder sb)
        {
            sb.Append(new string(' ', depth * 2));
            sb.Append(node.GetType().Name);
            sb.Append(" #");
            sb.Append(node.Id);

            string parameters = BTConfigWriter.SerializeParams(node);
            if (!string.IsNullOrEmpty(parameters))
            {
                sb.Append(" (");
                sb.Append(parameters);
                sb.Append(')');
            }

            sb.AppendLine();

            if (node.Children == null)
            {
                return;
            }

            foreach (BTNode child in node.Children)
            {
                Walk(child, depth + 1, sb);
            }
        }
    }
}
