using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIMainViewComponent))]
    [FriendOf(typeof(UIMainViewComponent))]
    public static partial class UIMainViewComponentSystem
    {
        private const string LabelLevel = "Level";
        private const string LabelSysTime = "SysTime";

        private const string NodeMoreButton = "MoreButton";

        private const string NodeMoreBord = "MoreBord";

        private const string NodeMoreClose = "CloseMore";

        private const string NodeMoreGrid = "Grid";

        private const string NodeCellLabel = "Label";

        private const string MoreCellSetting = "设置";

        private static (string Node, string Label)[] ButtonList()
        {
            return new (string, string)[]
            {
                ("CommunityBtn", "社群"),
                ("TeamButton", "队伍"),
                ("BagButton", "背包"),
                ("MoreButton", "更多（切更多面板）"),
                ("CloseBtn", "关小地图"),
                ("Skill1", "技能 1"),
                ("Skill2", "技能 2"),
                ("AddButton", "快捷键加号"),
                ("AutoBattleButton", "自动战斗"),
            };
        }

        [EntitySystem]
        private static void Awake(this UIMainViewComponent self)
        {
            Node panel = self.GetParent<UI>().GameObject;

            self.level = Find<Label>(panel, LabelLevel);
            self.sysTime = Find<Label>(panel, LabelSysTime);

            (string Node, string Label)[] buttons = ButtonList();
            foreach ((string node, string label) in buttons)
            {
                Button button = Find<Button>(panel, node);
                if (button == null)
                {
                    Log.Warning($"[主界面] MainView 里没找到按钮 {node}（{label}）—— 场景改动的话看这里");
                    continue;
                }

                if (node == NodeMoreButton)
                {
                    button.Pressed += () => self.ToggleMoreBord();
                }
                else
                {
                    string text = label;
                    button.Pressed += () => self.OnClickButton(node, text);
                }
                self.buttons[node] = button;
            }

            self.expectedButtonCount = buttons.Length;
            Log.Info($"[主界面] MainView 已接入：按钮 {self.buttons.Count}/{buttons.Length}，"
                    + $"等级节点 {(self.level != null ? "有" : "无")}，时间节点 {(self.sysTime != null ? "有" : "无")}");

            InitMoreBord(self, panel);
        }

        private static void InitMoreBord(this UIMainViewComponent self, Node panel)
        {
            Control moreBord = Find<Control>(panel, NodeMoreBord);
            if (moreBord == null)
            {
                Log.Warning("[主界面] MainView 里没找到 MoreBord（更多面板）—— 场景改动的话看这里");
                return;
            }

            self.moreBord = moreBord;
            moreBord.Visible = false;

            Button close = moreBord.GetNodeOrNull<Button>(NodeMoreClose);
            if (close != null)
            {
                close.Pressed += () => self.HideMoreBord();
            }
            else
            {
                Log.Warning($"[主界面] 更多面板里没找到关闭按钮 {NodeMoreClose}（面板只能靠「更多」按钮收起）");
            }

            Node grid = moreBord.GetNodeOrNull<Node>(NodeMoreGrid);
            if (grid != null)
            {
                foreach (Node child in grid.GetChildren())
                {
                    if (child is not Button cell)
                    {
                        continue;
                    }

                    Label label = cell.GetNodeOrNull<Label>(NodeCellLabel);
                    string name = label != null ? label.Text : child.Name.ToString();
                    self.moreCells[name] = cell;
                    cell.Pressed += () => self.OnClickMoreCell(name);
                }
            }

            Log.Info($"[主界面] 更多面板已接入：菜单格 {self.moreCells.Count} 个，"
                    + $"关闭按钮 {(close != null ? "有" : "无")}（默认收起）");
        }

        public static void ToggleMoreBord(this UIMainViewComponent self)
        {
            SetMoreBordVisible(self, self.moreBord != null && !self.moreBord.Visible);
        }

        public static void HideMoreBord(this UIMainViewComponent self)
        {
            SetMoreBordVisible(self, false);
        }

        private static void SetMoreBordVisible(this UIMainViewComponent self, bool visible)
        {
            if (self.moreBord == null)
            {
                return;
            }

            self.moreBord.Visible = visible;
            Log.Info(visible ? "[主界面] 更多面板：展开" : "[主界面] 更多面板：收起");
        }

        public static void OnClickMoreCell(this UIMainViewComponent self, string name)
        {
            if (name == MoreCellSetting)
            {
                Log.Info("[主界面] 更多面板点了「设置」→ 打开设置界面");
                self.HideMoreBord();
                OpenSetView(self);
                return;
            }

            Log.Info($"[主界面] 更多面板点了「{name}」—— 一期未实现（等对应界面接入）");
            self.HideMoreBord();
        }

        private static void OpenSetView(this UIMainViewComponent self)
        {
            Scene scene = self.Root();
            UIComponent uiComponent = scene?.GetComponent<UIComponent>();
            if (uiComponent == null)
            {
                Log.Error("[主界面] 没有 UIComponent，打不开设置界面");
                return;
            }

            if (uiComponent.Get(UIType.SetView) != null)
            {
                Log.Info("[主界面] 设置界面已经开着，不重复开");
                return;
            }

            UIHelper.Create(scene, UIType.SetView, UILayer.High).Coroutine();
        }

        public static void OnClickButton(this UIMainViewComponent self, string node, string label)
        {
            Log.Info($"[主界面] 点了「{label}」（{node}）—— 一期未实现");
        }

        public static void OnClickReturnToSelect(this UIMainViewComponent self)
        {
            Log.Info("[主界面] 返回角色选择界面");
            CharacterChooseHelper.ReturnAsync(self.Root()).Coroutine();
        }

        private static T Find<T>(Node root, string name) where T : class
        {
            if (root == null)
            {
                return null;
            }
            return root.FindChild(name, true, false) as T;
        }
    }
}
