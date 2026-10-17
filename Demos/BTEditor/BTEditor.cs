using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Godot;

namespace ET
{
    public partial class BTEditor: Control
    {
        private const string JsonRelativePath = ".FrameWork/Config/Excel_TestJsonExport/c/BTConfig.txt";

        private static readonly List<Color> TreeColors = new()
        {
            new Color(0.24f, 0.35f, 0.5f), new Color(0.35f, 0.3f, 0.45f), new Color(0.27f, 0.4f, 0.33f),
        };

        private readonly List<BTNodeData> allRows = new();
        private readonly Dictionary<int, BTNode> trees = new();
        private readonly Dictionary<int, ColorRect> nodeViews = new();
        private readonly Dictionary<int, int> nodeDepths = new();

        private BTNode selectedNode;
        private int selectedTreeId;
        private BTNode selectedTree;

        private OptionButton treeSelector;
        private OptionButton nodeTypeSelector;
        private VBoxContainer paramBox;
        private Label statusLabel;
        private LineDrawer lineDrawer;

        public override void _Ready()
        {
            EnsureHotfixAssemblies();
            this.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            this.BuildUi();
            this.Reload();
        }

        private static void EnsureHotfixAssemblies()
        {
            if (CodeTypes.Instance != null)
            {
                return;
            }

            World.Instance.AddSingleton<Logger>().Log = new GodotLogger();
            World.Instance.AddSingleton<CodeLoader>().LoadHotfixAssemblies();
        }

        private void BuildUi()
        {
            HSplitContainer root = new();
            root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            this.AddChild(root);

            VBoxContainer left = new();
            left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            root.AddChild(left);

            HBoxContainer toolbar = new();
            left.AddChild(toolbar);

            this.treeSelector = new OptionButton();
            this.treeSelector.ItemSelected += index => this.OpenTree((int)index);
            toolbar.AddChild(this.treeSelector);

            Button reload = new() { Text = "重新加载" };
            reload.Pressed += this.Reload;
            toolbar.AddChild(reload);

            this.nodeTypeSelector = new OptionButton();
            foreach (Type type in BTNodeMeta.GetNodeTypes())
            {
                this.nodeTypeSelector.AddItem(type.Name);
            }

            toolbar.AddChild(this.nodeTypeSelector);

            Button addChild = new() { Text = "给选中节点加子节点" };
            addChild.Pressed += this.AddChildNode;
            toolbar.AddChild(addChild);

            Button deleteNode = new() { Text = "删除选中节点" };
            deleteNode.Pressed += this.DeleteSelected;
            toolbar.AddChild(deleteNode);

            Button validate = new() { Text = "校验" };
            validate.Pressed += this.ValidateAll;
            toolbar.AddChild(validate);

            Button save = new() { Text = "保存 JSON" };
            save.Pressed += this.Save;
            toolbar.AddChild(save);

            this.statusLabel = new Label { Text = "" };
            toolbar.AddChild(this.statusLabel);

            this.lineDrawer = new LineDrawer { SizeFlagsVertical = SizeFlags.ExpandFill };
            left.AddChild(this.lineDrawer);

            PanelContainer rightPanel = new();
            rightPanel.CustomMinimumSize = new Vector2(260, 0);
            root.AddChild(rightPanel);

            ScrollContainer scroll = new();
            rightPanel.AddChild(scroll);

            this.paramBox = new VBoxContainer();
            scroll.AddChild(this.paramBox);
        }

        private string JsonPath => ProjectSettings.GlobalizePath(Path.Combine(ProjectSettings.GlobalizePath("res://"), JsonRelativePath));

        private void Reload()
        {
            this.allRows.Clear();
            this.trees.Clear();
            this.treeSelector.Clear();

            string path = Path.GetFullPath(this.JsonPath);
            if (!File.Exists(path))
            {
                this.SetStatus($"找不到 JSON: {path}(先跑一次 Launch01_ExportConfig.bat)");
                return;
            }

            JsonDocumentOptions options = new() { AllowTrailingCommas = true };
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path), options);
            HashSet<int> treeIds = new();
            foreach (JsonElement item in document.RootElement.GetProperty("dict").EnumerateArray())
            {
                JsonElement fields = item[1];
                BTNodeData row = new()
                {
                    TreeId = GetInt(fields, "TreeId"),
                    NodeId = GetInt(fields, "NodeId"),
                    ParentId = GetInt(fields, "ParentId"),
                    Order = GetInt(fields, "Order"),
                    NodeType = GetString(fields, "NodeType"),
                    Params = GetString(fields, "Params"),
                };
                this.allRows.Add(row);
                treeIds.Add(row.TreeId);
            }

            List<int> sorted = new(treeIds);
            sorted.Sort();
            foreach (int treeId in sorted)
            {
                this.treeSelector.AddItem($"树 {treeId}");
            }

            if (sorted.Count == 0)
            {
                this.SetStatus("表里没有树");
                return;
            }

            this.treeSelector.Selected = 0;
            this.OpenTree(0);
            this.SetStatus($"已加载 {sorted.Count} 棵树,{this.allRows.Count} 行");
        }

        private void OpenTree(int index)
        {
            if (index < 0 || index >= this.treeSelector.ItemCount)
            {
                return;
            }

            this.selectedTreeId = this.treeSelector.GetItemText(index).Replace("树 ", "").ToInt();
            this.selectedNode = null;

            if (!this.trees.TryGetValue(this.selectedTreeId, out this.selectedTree))
            {
                this.selectedTree = BTBuilder.Build(this.allRows, this.selectedTreeId);
                this.trees.Add(this.selectedTreeId, this.selectedTree);
            }

            this.RefreshCanvas();
        }

        private void RefreshCanvas()
        {
            this.nodeViews.Clear();
            this.nodeDepths.Clear();
            foreach (Node child in this.lineDrawer.GetChildren())
            {
                child.QueueFree();
            }

            if (this.selectedTree == null)
            {
                return;
            }

            int nextColumn = 0;
            Layout(this.selectedTree, 0, ref nextColumn);

            foreach ((int nodeId, ColorRect view) in this.nodeViews)
            {
                view.GuiInput += @event => this.OnNodeInput(@event, nodeId);
            }

            this.RefreshParamPanel();
            this.lineDrawer.Refresh(this.nodeViews, this.selectedTree, this.nodeDepths);
        }

        private int Layout(BTNode node, int depth, ref int nextColumn)
        {
            this.nodeDepths[node.Id] = depth;

            int column;
            if (node.Children == null || node.Children.Count == 0)
            {
                column = nextColumn++;
            }
            else
            {
                int first = int.MaxValue;
                int last = int.MinValue;
                foreach (BTNode child in node.Children)
                {
                    int childColumn = Layout(child, depth + 1, ref nextColumn);
                    first = Math.Min(first, childColumn);
                    last = Math.Max(last, childColumn);
                }

                column = (first + last) / 2;
            }

            ColorRect view = new()
            {
                Color = this.GetNodeColor(node),
                Size = new Vector2(160, 44),
                Position = new Vector2(column * 190 + 20, depth * 90 + 20),
            };

            Label label = new()
            {
                Text = this.GetNodeText(node),
                Position = new Vector2(4, 2),
                Size = new Vector2(152, 40),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            view.AddChild(label);
            this.lineDrawer.AddChild(view);
            this.nodeViews[node.Id] = view;
            return column;
        }

        private Color GetNodeColor(BTNode node)
        {
            float[] color = BTNodeMeta.GetTitleColor(node.GetType());
            if (color != null)
            {
                return new Color(color[0], color[1], color[2], color[3]);
            }

            int index = Math.Abs(node.GetType().Name.GetHashCode()) % TreeColors.Count;
            return TreeColors[index];
        }

        private string GetNodeText(BTNode node)
        {
            string parameters = BTConfigWriter.SerializeParams(node);
            string text = $"{node.GetType().Name} #{node.Id}";
            if (!string.IsNullOrEmpty(parameters))
            {
                text += $"\n{parameters}";
            }

            return text;
        }

        private void OnNodeInput(InputEvent @event, int nodeId)
        {
            if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Left)
            {
                return;
            }

            this.selectedNode = this.FindNode(this.selectedTree, nodeId);
            if (this.nodeViews.TryGetValue(nodeId, out ColorRect view))
            {
                view.Modulate = new Color(1.4f, 1.4f, 1.4f);
            }

            foreach ((int id, ColorRect other) in this.nodeViews)
            {
                if (id != nodeId)
                {
                    other.Modulate = Colors.White;
                }
            }

            this.RefreshParamPanel();
        }

        private BTNode FindNode(BTNode node, int nodeId)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Id == nodeId)
            {
                return node;
            }

            if (node.Children == null)
            {
                return null;
            }

            foreach (BTNode child in node.Children)
            {
                BTNode found = this.FindNode(child, nodeId);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void RefreshParamPanel()
        {
            foreach (Node child in this.paramBox.GetChildren())
            {
                child.QueueFree();
            }

            if (this.selectedNode == null)
            {
                this.paramBox.AddChild(new Label { Text = "点一个方块来编辑参数" });
                return;
            }

            this.paramBox.AddChild(new Label { Text = $"{this.selectedNode.GetType().Name} #{this.selectedNode.Id}" });
            foreach (BTNodeFieldMeta field in BTNodeMeta.GetFields(this.selectedNode.GetType()))
            {
                HBoxContainer line = new();
                line.AddChild(new Label { Text = field.Name, CustomMinimumSize = new Vector2(110, 0) });
                LineEdit edit = new()
                {
                    Text = FormatField(this.selectedNode, field.Name),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                line.AddChild(edit);
                this.paramBox.AddChild(line);

                edit.TextSubmitted += text =>
                {
                    if (this.TryApplyField(field.Name, text))
                    {
                        this.RefreshCanvas();
                    }
                };
            }

            this.paramBox.AddChild(new Label { Text = "(输完回车生效)" });
        }

        private static string FormatField(BTNode node, string fieldName)
        {
            FieldInfo field = node.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                return "";
            }

            object value = field.GetValue(node);
            return value?.ToString() ?? "";
        }

        private bool TryApplyField(string fieldName, string text)
        {
            try
            {
                BTBuilder.ApplyParams(this.selectedNode, $"{fieldName}={text}");
                this.SetStatus($"{fieldName} = {text}");
                return true;
            }
            catch (Exception e)
            {
                this.SetStatus($"参数错误: {e.Message}");
                return false;
            }
        }

        private void AddChildNode()
        {
            if (this.selectedNode == null)
            {
                this.SetStatus("先选中一个节点");
                return;
            }

            List<Type> types = BTNodeMeta.GetNodeTypes();
            int index = this.nodeTypeSelector.Selected;
            if (index < 0 || index >= types.Count)
            {
                return;
            }

            BTNode child = (BTNode)Activator.CreateInstance(types[index]);
            child.Id = this.NextNodeId();
            this.selectedNode.Children ??= new List<BTNode>();
            this.selectedNode.Children.Add(child);
            this.RefreshCanvas();
            this.SetStatus($"已加子节点 {child.GetType().Name} #{child.Id}");
        }

        private void DeleteSelected()
        {
            if (this.selectedNode == null || this.selectedNode == this.selectedTree)
            {
                this.SetStatus("根节点不能删(选中子节点再删)");
                return;
            }

            if (!this.RemoveNode(this.selectedTree, this.selectedNode.Id))
            {
                return;
            }

            this.selectedNode = null;
            this.RefreshCanvas();
            this.SetStatus("已删除");
        }

        private bool RemoveNode(BTNode parent, int nodeId)
        {
            if (parent.Children == null)
            {
                return false;
            }

            for (int i = 0; i < parent.Children.Count; ++i)
            {
                if (parent.Children[i].Id == nodeId)
                {
                    parent.Children.RemoveAt(i);
                    return true;
                }

                if (this.RemoveNode(parent.Children[i], nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        private int NextNodeId()
        {
            int max = 0;
            foreach (BTNodeData row in this.allRows)
            {
                if (row.TreeId == this.selectedTreeId && row.NodeId > max)
                {
                    max = row.NodeId;
                }
            }

            return max + 1;
        }

        private void ValidateAll()
        {
            List<BTNodeData> rows = new();
            List<int> treeIds = this.CollectTreeIds();

            foreach (int treeId in treeIds)
            {
                if (this.trees.TryGetValue(treeId, out BTNode tree))
                {
                    rows.AddRange(BTConfigWriter.Write(tree, treeId));
                }
                else
                {
                    foreach (BTNodeData row in this.allRows)
                    {
                        if (row.TreeId == treeId)
                        {
                            rows.Add(row);
                        }
                    }
                }
            }

            List<BTConfigError> errors = BTConfigValidator.Validate(rows);
            if (errors.Count == 0)
            {
                this.SetStatus("校验通过");
                return;
            }

            StringBuilder sb = new();
            foreach (BTConfigError error in errors)
            {
                sb.Append(error).Append("  ");
            }

            this.SetStatus(sb.ToString());
        }

        private List<int> CollectTreeIds()
        {
            List<int> ids = new();
            foreach (BTNodeData row in this.allRows)
            {
                if (!ids.Contains(row.TreeId))
                {
                    ids.Add(row.TreeId);
                }
            }

            ids.Sort();
            return ids;
        }

        private void Save()
        {
            List<int> treeIds = this.CollectTreeIds();
            List<BTNodeData> output = new();
            foreach (int treeId in treeIds)
            {
                if (this.trees.TryGetValue(treeId, out BTNode tree))
                {
                    output.AddRange(BTConfigWriter.Write(tree, treeId));
                }
                else
                {
                    foreach (BTNodeData row in this.allRows)
                    {
                        if (row.TreeId == treeId)
                        {
                            output.Add(row);
                        }
                    }
                }
            }

            List<BTConfigError> errors = BTConfigValidator.Validate(output);
            if (errors.Count > 0)
            {
                this.SetStatus($"有 {errors.Count} 个错误,先修再存:{errors[0]}");
                return;
            }

            StringBuilder sb = new();
            sb.Append("{\"dict\": [\n");
            int rowId = 1;
            foreach (BTNodeData row in output)
            {
                sb.Append($"[{rowId}, {{\"_t\":\"BTConfig\",\"_id\":{rowId},\"TreeId\":{row.TreeId},\"NodeId\":{row.NodeId},")
                        .Append($"\"ParentId\":{row.ParentId},\"Order\":{row.Order},\"NodeType\":\"{row.NodeType}\",\"Params\":\"{Escape(row.Params)}\"}}],\n");
                ++rowId;
            }

            sb.Append("]}\n");

            string path = Path.GetFullPath(this.JsonPath);
            File.WriteAllText(path, sb.ToString());
            this.SetStatus($"已保存 {output.Count} 行:{path}(接着跑 Launch01_ExportConfig.bat)");
        }

        private static string Escape(string text)
        {
            return (text ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static int GetInt(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number? value.GetInt32() : 0;
        }

        private static string GetString(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            {
                return "";
            }

            return value.GetString() ?? "";
        }

        private void SetStatus(string text)
        {
            if (this.statusLabel != null)
            {
                this.statusLabel.Text = text;
            }

            GD.Print($"[BTEditor] {text}");
        }

        private partial class LineDrawer: Control
        {
            private readonly List<(Vector2 From, Vector2 To)> lines = new();

            public void Refresh(Dictionary<int, ColorRect> views, BTNode root, Dictionary<int, int> depths)
            {
                this.lines.Clear();
                this.Collect(root, views);
                this.QueueRedraw();
            }

            private void Collect(BTNode node, Dictionary<int, ColorRect> views)
            {
                if (node?.Children == null)
                {
                    return;
                }

                if (views.TryGetValue(node.Id, out ColorRect parentView))
                {
                    foreach (BTNode child in node.Children)
                    {
                        if (views.TryGetValue(child.Id, out ColorRect childView))
                        {
                            this.lines.Add((
                                    parentView.Position + new Vector2(parentView.Size.X / 2, parentView.Size.Y),
                                    childView.Position + new Vector2(childView.Size.X / 2, 0)));
                        }

                        this.Collect(child, views);
                    }
                }
            }

            public override void _Draw()
            {
                foreach ((Vector2 from, Vector2 to) in this.lines)
                {
                    this.DrawLine(from, to, new Color(0.8f, 0.8f, 0.8f), 2);
                }
            }
        }
    }
}
