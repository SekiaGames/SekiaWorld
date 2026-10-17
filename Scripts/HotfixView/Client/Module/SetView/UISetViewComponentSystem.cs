using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(UISetViewComponent))]
    [FriendOf(typeof(UISetViewComponent))]
    public static partial class UISetViewComponentSystem
    {
        private const string NodeClose = "CloseButton";
        private const string NodeToggles = "toggles";
        private const string NodeActivatedPanel = "MainBoard/ActivatedPanel";
        private const string NodeSwitchRoleBtn = "MainBoard/MainPage/BackLoginBtn";
        private const string NodeQuitBtn = "MainBoard/MainPage/SaveBtn";

        private const string NodeMark = "Checkmark";

        private const string DefaultTab = "SystemSetting";

        [EntitySystem]
        private static void Awake(this UISetViewComponent self)
        {
            Node panel = self.GetParent<UI>().GameObject;

            self.closeButton = panel.GetNodeOrNull<Button>(NodeClose);
            if (self.closeButton != null)
            {
                self.closeButton.Pressed += () => self.OnClickClose();
            }

            Node toggles = panel.GetNodeOrNull<Node>(NodeToggles);
            Node pagesRoot = panel.GetNodeOrNull<Node>(NodeActivatedPanel);
            if (toggles != null && pagesRoot != null)
            {
                foreach (Node child in toggles.GetChildren())
                {
                    if (child is not Button tab)
                    {
                        continue;
                    }

                    string tabName = child.Name.ToString();
                    Control page = pagesRoot.GetNodeOrNull<Control>(PageNameOf(tabName));
                    if (page == null)
                    {
                        Log.Warning($"[设置] tab「{tabName}」找不到对应分页「{PageNameOf(tabName)}」—— 场景改动的话看这里");
                        continue;
                    }

                    self.tabs[tabName] = tab;
                    self.pages[tabName] = page;
                    self.tabMarks[tabName] = tab.GetNodeOrNull<Control>(NodeMark);

                    string key = tabName;
                    tab.Pressed += () => self.SwitchTab(key);
                }
            }

            self.switchRoleButton = panel.GetNodeOrNull<Button>(NodeSwitchRoleBtn);
            if (self.switchRoleButton != null)
            {
                self.switchRoleButton.Pressed += () => self.OnClickSwitchRole();
            }

            self.quitButton = panel.GetNodeOrNull<Button>(NodeQuitBtn);
            if (self.quitButton != null)
            {
                self.quitButton.Pressed += () => self.OnClickQuit();
            }

            StubRestButtons(self, panel);

            if (self.pages.Count > 0)
            {
                self.SwitchTab(DefaultTab);
            }

            Log.Info($"[设置] 界面已打开：分页 {self.pages.Count} 个（当前「{self.currentTab}」），"
                    + $"关闭按钮 {(self.closeButton != null ? "有" : "无")}，打桩按钮 {self.stubButtonCount} 个");
        }

        public static void SwitchTab(this UISetViewComponent self, string tab)
        {
            if (!self.pages.TryGetValue(tab, out Control active))
            {
                Log.Warning($"[设置] 没有分页「{tab}」");
                return;
            }

            foreach (KeyValuePair<string, Control> kv in self.pages)
            {
                kv.Value.Visible = kv.Key == tab;
            }

            foreach (KeyValuePair<string, Control> kv in self.tabMarks)
            {
                if (kv.Value != null)
                {
                    kv.Value.Visible = kv.Key == tab;
                }
            }

            self.currentTab = tab;
            Log.Info($"[设置] 切到分页：{tab}");
        }

        private static string PageNameOf(string tab)
        {
            const string suffix = "Setting";
            return (tab.EndsWith(suffix) ? tab[..^suffix.Length] : tab) + "Page";
        }

        public static void OnClickClose(this UISetViewComponent self)
        {
            Log.Info("[设置] 关闭");
            UIHelper.Remove(self.Root(), UIType.SetView).Coroutine();
        }

        public static void OnClickSwitchRole(this UISetViewComponent self)
        {
            Log.Info("[设置] 点了「切换角色」→ 退回角色选择界面");
            CharacterChooseHelper.ReturnAsync(self.Root()).Coroutine();
        }

        public static void OnClickQuit(this UISetViewComponent self)
        {
            Log.Info("[设置] 点了「退出游戏」—— 打桩（退出行为以后再定）");
        }

        public static void OnClickStub(this UISetViewComponent self, string name)
        {
            Log.Info($"[设置] 点了「{name}」—— 打桩（客户：先只做打开/关闭）");
        }

        private static void StubRestButtons(this UISetViewComponent self, Node root)
        {
            foreach (Node child in root.GetChildren())
            {
                if (child is Button button && !IsHandled(self, button))
                {
                    string name = button.Name.ToString();
                    button.Pressed += () => self.OnClickStub(name);
                    self.stubButtonCount++;
                }

                StubRestButtons(self, child);
            }
        }

        private static bool IsHandled(this UISetViewComponent self, Button button)
        {
            if (button == self.closeButton || button == self.switchRoleButton || button == self.quitButton)
            {
                return true;
            }

            foreach (Button tab in self.tabs.Values)
            {
                if (tab == button)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
