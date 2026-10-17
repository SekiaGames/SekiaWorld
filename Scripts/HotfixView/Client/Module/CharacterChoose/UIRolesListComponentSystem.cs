using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIRolesListComponent))]
    [FriendOf(typeof(UIRolesListComponent))]
    public static partial class UIRolesListComponentSystem
    {
        private const string BackButton = "ButtonBack";
        private const string RolesList = "RolesList";
        private const string CellFormat = "RolesList/RoleCell{0}";
        private const string CellFrame = "Frame";
        private const string CellIcon = "Icon";
        private const string StartButton = "ButtonStartGame";
        private const string StartButtonTitle = "ButtonStartGame/Title";

        private const string DeleteButton = "ButtonDelete";

        private const int CellCount = 3;

        private const int LockedFrom = 3;

        private const string TitleCreate = "创建角色";
        private const string TitleEnter = "进入游戏";

        private static Color SelectedTint()
        {
            return new Color(1.0f, 0.80f, 0.32f);
        }

        private static Color NormalTint()
        {
            return Colors.White;
        }

        [EntitySystem]
        private static void Awake(this UIRolesListComponent self)
        {
            Node panel = self.GetParent<UI>().GameObject;

            self.backButton = panel.GetNodeOrNull<Button>(BackButton);
            self.startButton = panel.GetNodeOrNull<Button>(StartButton);
            self.startButtonTitle = panel.GetNodeOrNull<Label>(StartButtonTitle);
            self.deleteButton = panel.GetNodeOrNull<Button>(DeleteButton);

            for (int i = 1; i <= CellCount; ++i)
            {
                Button cell = panel.GetNodeOrNull<Button>(string.Format(CellFormat, i));
                if (cell == null)
                {
                    Log.Error($"[选角界面] 场景里找不到角色槽 {string.Format(CellFormat, i)}");
                    continue;
                }

                int index = i - 1;
                self.cells.Add(cell);
                self.cellFrames.Add(cell.GetNodeOrNull<TextureRect>(CellFrame));
                self.cellIcons.Add(cell.GetNodeOrNull<TextureRect>(CellIcon));
                self.cellCreatable.Add(i < LockedFrom);
                self.cellRoles.Add(null);

                cell.Pressed += () => self.OnClickCell(index);
            }

            if (self.backButton != null)
            {
                self.backButton.Pressed += () => self.OnClickBack();
            }
            if (self.startButton != null)
            {
                self.startButton.Pressed += () => self.OnClickStart();
            }
            if (self.deleteButton != null)
            {
                self.deleteButton.Pressed += () => self.OnClickDelete();
            }
            else
            {
                Log.Error("[选角界面] 场景里找不到删除按钮 ButtonDelete —— 改了美术场景要重打 SekiaArt_UI 的 pck");
            }

            self.ReloadRoles("已打开");
        }

        private static void ReloadRoles(this UIRolesListComponent self, string reason)
        {
            ClientRoleListComponent roleList = self.Root().GetComponent<ClientRoleListComponent>();

            for (int i = 0; i < self.cellRoles.Count; ++i)
            {
                RoleInfo role = roleList?.Get(i);
                self.cellRoles[i] = role;
                self.cellCreatable[i] = role == null && i + 1 < LockedFrom;
            }

            if (self.SelectedRole() == null)
            {
                self.selectedIndex = -1;
                for (int i = 0; i < self.cellRoles.Count; ++i)
                {
                    if (self.cellRoles[i] != null)
                    {
                        self.selectedIndex = i;
                        break;
                    }
                }
            }

            self.Refresh();
            self.SyncMountRole();

            Log.Info($"[选角界面] {reason}：槽 {(roleList != null ? roleList.Describe(CellCount, LockedFrom) : "拿不到角色列表")}");
        }

        private static void SyncMountRole(this UIRolesListComponent self)
        {
            CharacterChooseComponent choose = self.Root().GetComponent<CharacterChooseComponent>();
            if (choose == null)
            {
                return;
            }

            RoleInfo selected = self.SelectedRole();
            choose.ShowRole(selected != null ? ClientRoleHelper.FromArray(selected.Parts) : null);
        }

        public static void OnClickDelete(this UIRolesListComponent self)
        {
            RoleInfo selected = self.SelectedRole();
            if (selected == null)
            {
                Log.Info("[选角界面] 没有选中已有角色（选的是空位/没选），没什么可删的");
                return;
            }

            if (self.busy)
            {
                return;
            }

            self.busy = true;
            DeleteRoleAsync(self, selected.Sequence, selected.Name).Coroutine();
        }

        private static async ETTask DeleteRoleAsync(UIRolesListComponent self, int sequence, string name)
        {
            EntityRef<UIRolesListComponent> selfRef = self;
            EntityRef<Scene> sceneRef = self.Root();

            try
            {
                RoleOpResult result = await ClientRoleHelper.DeleteRoleAsync(sceneRef, sequence);

                self = selfRef;
                if (self == null)
                {
                    return;
                }

                if (!result.IsSuccess)
                {
                    Log.Warning($"[选角界面] 删角失败：槽 {sequence + 1}「{name}」→ {result.Message}");
                }
                self.ReloadRoles(result.IsSuccess ? $"删除后（删了槽 {sequence + 1} 的「{name}」）" : "删角失败后");
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.busy = false;
                }
            }
        }

        public static void OnClickCell(this UIRolesListComponent self, int index)
        {
            if (index < 0 || index >= self.cells.Count)
            {
                return;
            }

            if (index + 1 >= LockedFrom)
            {
                Log.Info($"[选角界面] 槽 {index + 1} 未开放（锁）");
                return;
            }

            self.selectedIndex = self.selectedIndex == index ? -1 : index;
            self.Refresh();
            self.SyncMountRole();
        }

        public static void OnClickBack(this UIRolesListComponent self)
        {
            Log.Info("[选角界面] 点了返回（一期未实现回登录，忽略）");
        }

        public static void OnClickStart(this UIRolesListComponent self)
        {
            if (self.busy)
            {
                return;
            }

            RoleInfo selected = self.SelectedRole();
            if (selected != null)
            {
                self.busy = true;
                EnterGameAsync(self, selected).Coroutine();
                return;
            }

            if (self.selectedIndex < 0)
            {
                self.selectedIndex = 0;
                self.Refresh();
            }

            self.busy = true;
            EnterCharacterSelect(self).Coroutine();
        }

        public static RoleInfo SelectedRole(this UIRolesListComponent self)
        {
            if (self.selectedIndex < 0 || self.selectedIndex >= self.cellRoles.Count)
            {
                return null;
            }
            return self.cellRoles[self.selectedIndex];
        }

        private static async ETTask EnterGameAsync(UIRolesListComponent self, RoleInfo role)
        {
            EntityRef<Scene> sceneRef = self.Root();

            try
            {
                Log.Info($"[选角界面] 进入游戏：槽 {role.Sequence + 1} 名字「{role.Name}」");

                Scene scene = sceneRef;
                if (scene == null)
                {
                    return;
                }

                CreatedRoleComponent created = scene.GetComponent<CreatedRoleComponent>();
                if (created == null)
                {
                    created = scene.AddComponent<CreatedRoleComponent>();
                }
                created.RoleName = role.Name;
                created.Parts = ClientRoleHelper.FromArray(role.Parts);

                await UIHelper.Remove(scene, UIType.UIViewRolesList);

                scene = sceneRef;
                if (scene == null)
                {
                    return;
                }

                scene.GetComponent<CharacterChooseComponent>()?.Deactivate();

                ResourcesLoaderComponent loader = scene.GetComponent<ResourcesLoaderComponent>();
                if (loader != null)
                {
                    loader.UnloadScene(CharacterChooseComponentSystem.ScenePath);
                }

                Log.Info("[选角界面] 开始进首图（C2G_EnterMap）");
                await EnterMapHelper.EnterMapAsync(scene, role.Sequence);
                Log.Info("[选角界面] 已进入首图");
            }
            catch (System.Exception e)
            {
                Log.Error($"[选角界面] 进入游戏失败：{e}");
            }
        }

        private static async ETTask EnterCharacterSelect(UIRolesListComponent self)
        {
            EntityRef<UIRolesListComponent> selfRef = self;
            EntityRef<Scene> sceneRef = self.Root();

            try
            {
                await CharacterSelectHelper.EnterAsync(sceneRef);
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.busy = false;
                }
            }
        }

        public static void Refresh(this UIRolesListComponent self)
        {
            for (int i = 0; i < self.cellFrames.Count; ++i)
            {
                TextureRect frame = self.cellFrames[i];
                if (frame == null)
                {
                    continue;
                }
                frame.Modulate = i == self.selectedIndex ? SelectedTint() : NormalTint();

                if (i < self.cellIcons.Count && self.cellIcons[i] != null && GodotObject.IsInstanceValid(self.cellIcons[i]))
                {
                    bool hasRole = i < self.cellRoles.Count && self.cellRoles[i] != null;
                    if (hasRole)
                    {
                        self.cellIcons[i].Visible = false;
                    }
                }
            }

            if (self.startButtonTitle != null)
            {
                self.startButtonTitle.Text = self.SelectedRole() != null ? TitleEnter : TitleCreate;
            }

            if (self.deleteButton != null && GodotObject.IsInstanceValid(self.deleteButton))
            {
                self.deleteButton.Visible = self.SelectedRole() != null;
            }
        }

        public static bool IsEmptySlot(this UIRolesListComponent self, int index)
        {
            return index >= 0 && index < self.cellCreatable.Count && self.cellCreatable[index];
        }
    }
}
