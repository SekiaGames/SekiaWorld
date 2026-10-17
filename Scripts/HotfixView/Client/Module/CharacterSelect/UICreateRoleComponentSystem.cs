using System;
using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(UICreateRoleComponent))]
    [FriendOf(typeof(UICreateRoleComponent))]
    [FriendOf(typeof(CreatedRoleComponent))]
    public static partial class UICreateRoleComponentSystem
    {
        private const string ReturnBtn = "returnBtn";
        private const string Title = "Title";
        private const string LeftPanel = "Left";
        private const string RightPanel = "Right";
        private const string SexBtnMale = "Left/SexBtn_1";
        private const string SexBtnFemale = "Left/SexBtn_2";
        private const string SexFemaleCover = "Left/SexBtn_2/Cover";
        private const string HairSlotFormat = "Left/HairSlot{0}";
        private const string HairSlotFrame = "Frame";
        private const string SlotIcon = "Icon";

        private const string SlotSelected = "Selected";
        private const string HairLeftBtn = "Left/HairLeftBtn";
        private const string HairRightBtn = "Left/HairRightBtn";
        private const string HairColorLab = "Left/HairColorLab";
        private const string HeadSlotFormat = "Left/HeadSlot{0}";
        private const string HeadSlotFrame = "Frame";
        private const string NameInput = "NameInput";
        private const string ProfLabel = "Right/ProfLabel";
        private const string ProfLabelEng = "Right/ProfLabelEng";
        private const string ProfFlag = "Right/ProfFlag";
        private const string ProfIcon = "Right/ProfessionIcon";
        private const string SymbolDisc = "Right/Symbol";
        private const string AttrBg2 = "AttrBg_2";
        private const string EnterBtn = "Right/EnterBtn";

        private const string AttrPolygonName = "AttrPolygon";

        private const string IconDir = "res://Resources/GUI/pic/UI/";

        private const int NoviceClassId = 1;

        private const int NoviceMaleEye = 1;

        private const int NoviceFemaleEye = 2;
        private const int NoviceDefaultWeapon = 40301;

        private const string AttrNodeCsv = "Str,Vit,Luk,Int,Dex,Agi";

        private const float AttrCenterX = 1090f;
        private const float AttrCenterY = 460f;
        private const float AttrRadius = 112f;

        private const float AttrPolygonAlpha = 0.8f;

        private static int[] AttrValues(ClassConfig classRow)
        {
            if (classRow == null)
            {
                return null;
            }

            int[] values = { classRow.AttrStr, classRow.AttrVit, classRow.AttrLuk,
                             classRow.AttrInt, classRow.AttrDex, classRow.AttrAgi };
            foreach (int v in values)
            {
                if (v != 0)
                {
                    return values;
                }
            }

            return null;
        }

        private static string[] AttrNodeNames()
        {
            return AttrNodeCsv.Split(',');
        }

        private static Color CareerFlagColor(int type)
        {
            return type switch
            {
                1 => new Color(255 / 255f, 84 / 255f, 70 / 255f),
                2 => new Color(57 / 255f, 114 / 255f, 215 / 255f),
                3 => new Color(151 / 255f, 69 / 255f, 171 / 255f),
                4 => new Color(255 / 255f, 198 / 255f, 48 / 255f),
                5 => new Color(24 / 255f, 210 / 255f, 126 / 255f),
                6 => new Color(249 / 255f, 132 / 255f, 50 / 255f),
                _ => Colors.White,
            };
        }

        private static Color CareerIconBgColor(int type)
        {
            return type switch
            {
                1 => new Color(185 / 255f, 37 / 255f, 40 / 255f),
                2 => new Color(50 / 255f, 77 / 255f, 167 / 255f),
                3 => new Color(109 / 255f, 52 / 255f, 136 / 255f),
                4 => new Color(255 / 255f, 192 / 255f, 0f),
                5 => new Color(30 / 255f, 136 / 255f, 93 / 255f),
                6 => new Color(233 / 255f, 115 / 255f, 17 / 255f),
                _ => Colors.White,
            };
        }

        private static (Color inside, Color outside) AttrPolygonColors(int classIndex)
        {
            return classIndex switch
            {
                1 => (Color.FromHtml("FFA200"), Color.FromHtml("FDFF51")),
                2 => (Color.FromHtml("00A463"), Color.FromHtml("4EFF90")),
                3 => (Color.FromHtml("FF4200"), Color.FromHtml("FFA800")),
                4 => (Color.FromHtml("0740CE"), Color.FromHtml("359CFF")),
                5 => (Color.FromHtml("F5242B"), Color.FromHtml("FF5B3C")),
                6 => (Color.FromHtml("871FF1"), Color.FromHtml("E76DFF")),
                _ => (Colors.White, Colors.White),
            };
        }

        private static Color SelectedTint()
        {
            return new Color(1.0f, 0.80f, 0.32f);
        }
        private static Color NormalTint()
        {
            return Colors.White;
        }

        [EntitySystem]
        private static void Awake(this UICreateRoleComponent self)
        {
            Node panel = self.GetParent<UI>().GameObject;

            self.returnBtn = panel.GetNodeOrNull<Button>(ReturnBtn);
            self.titleBanner = panel.GetNodeOrNull<TextureRect>(Title);
            self.leftPanel = panel.GetNodeOrNull<Control>(LeftPanel);
            self.rightPanel = panel.GetNodeOrNull<Control>(RightPanel);
            self.sexBtnMale = panel.GetNodeOrNull<Button>(SexBtnMale);
            self.sexBtnFemale = panel.GetNodeOrNull<Button>(SexBtnFemale);
            self.femaleCover = panel.GetNodeOrNull<Panel>(SexFemaleCover);
            self.hairLeftBtn = panel.GetNodeOrNull<Button>(HairLeftBtn);
            self.hairRightBtn = panel.GetNodeOrNull<Button>(HairRightBtn);
            self.hairColorLab = panel.GetNodeOrNull<Label>(HairColorLab);
            self.nameInput = panel.GetNodeOrNull<LineEdit>(NameInput);
            self.profLabel = panel.GetNodeOrNull<Label>(ProfLabel);
            self.profLabelEng = panel.GetNodeOrNull<Label>(ProfLabelEng);
            self.profFlag = panel.GetNodeOrNull<TextureRect>(ProfFlag);
            self.profIcon = panel.GetNodeOrNull<TextureRect>(ProfIcon);
            self.symbolDisc = panel.GetNodeOrNull<TextureRect>(SymbolDisc);
            self.enterBtn = panel.GetNodeOrNull<Button>(EnterBtn);

            Node right = panel.GetNodeOrNull<Node>("Right");
            if (right != null)
            {
                Node2D container = new() { Name = AttrPolygonName };
                right.AddChild(container);
                Node attrBg2 = right.GetNodeOrNull<Node>(AttrBg2);
                int at = attrBg2 != null ? attrBg2.GetIndex() + 1 : right.GetChildCount() - 1;
                right.MoveChild(container, at);
                self.attrPolygon = container;
            }

            for (int i = 1; i <= 4; ++i)
            {
                Button slot = panel.GetNodeOrNull<Button>(string.Format(HairSlotFormat, i));
                if (slot == null)
                {
                    continue;
                }
                int index = i - 1;
                self.hairSlots.Add(slot);
                self.hairFrames.Add(slot.GetNodeOrNull<Control>(HairSlotFrame));
                self.hairIcons.Add(slot.GetNodeOrNull<TextureRect>(SlotIcon));
                self.hairSelects.Add(slot.GetNodeOrNull<Control>(SlotSelected));
                slot.Pressed += () => self.OnClickHairSlot(index);
            }
            for (int i = 1; i <= 3; ++i)
            {
                Button slot = panel.GetNodeOrNull<Button>(string.Format(HeadSlotFormat, i));
                if (slot == null)
                {
                    continue;
                }
                int index = i - 1;
                self.headSlots.Add(slot);
                self.headFrames.Add(slot.GetNodeOrNull<Control>(HeadSlotFrame));
                self.headIcons.Add(slot.GetNodeOrNull<TextureRect>(SlotIcon));
                slot.Pressed += () => self.OnClickHeadSlot(index);
            }

            string[] attrNodeNames = AttrNodeNames();
            foreach (string nodeName in attrNodeNames)
            {
                self.attrLabels.Add(panel.GetNodeOrNull<Label>($"Right/Attr{nodeName}"));
            }

            if (self.returnBtn != null)
            {
                self.returnBtn.Pressed += () => self.OnClickReturn();
            }
            if (self.sexBtnMale != null)
            {
                self.sexBtnMale.Pressed += () => self.SetGender(1);
            }
            if (self.sexBtnFemale != null)
            {
                self.sexBtnFemale.Pressed += () => self.SetGender(2);
            }
            if (self.hairLeftBtn != null)
            {
                self.hairLeftBtn.Pressed += () => self.StepHairColor(-1);
            }
            if (self.hairRightBtn != null)
            {
                self.hairRightBtn.Pressed += () => self.StepHairColor(1);
            }
            if (self.enterBtn != null)
            {
                self.enterBtn.Pressed += () => self.OnClickEnter();
            }

            self.InitData();
            self.Refresh();
            self.SyncToScene();

            self.HideAll();

            Log.Info($"[创角界面] 已打开：职业序号 {self.classIndex} 性别 {self.gender} "
                    + $"发型 {self.hairSlots.Count} 格 / 头饰 {self.headSlots.Count} 格");
            Log.Info($"[创角界面] 格子图标：发型 {CountIcons(self.hairIcons)}/{self.hairIcons.Count}，"
                    + $"头饰 {CountIcons(self.headIcons)}/{self.headIcons.Count}");
        }

        public static void InitData(this UICreateRoleComponent self)
        {
            CharacterSelectList role = RoleConfigHelper.MakeRole(self.classIndex);
            if (role == null)
            {
                Log.Error($"[创角界面] 配表里没有职业序号 {self.classIndex}");
                return;
            }

            self.gender = role.DefaultGender;
            self.ResetAppearanceForGender(role);

            if (self.nameInput != null && string.IsNullOrEmpty(self.nameInput.Text))
            {
                self.nameInput.PlaceholderText = "点击输入名字";
            }
        }

        private static void ResetAppearanceForGender(this UICreateRoleComponent self, CharacterSelectList role)
        {
            self.hairIndex = 0;
            NpcConfig npc = RoleConfigHelper.Npc(self.gender == 2 ? role.FemaleNpcId : role.MaleNpcId);
            List<int> hairList = self.CurrentHairList();
            if (npc != null)
            {
                int at = hairList.IndexOf(npc.Hair);
                if (at >= 0)
                {
                    self.hairIndex = at;
                }
            }

            self.hairColorIndex = 0;
            List<int> colors = self.CurrentHairColorList();
            int defColor = self.CurrentHairStyle()?.DefaultColor ?? 0;
            if (defColor > 0)
            {
                int at = colors.IndexOf(defColor);
                if (at >= 0)
                {
                    self.hairColorIndex = at;
                }
            }

            self.headIndex = 0;
        }

        private static List<int> CurrentHairList(this UICreateRoleComponent self)
        {
            CharacterGenderConfig gender = RoleConfigHelper.Gender(self.gender);
            return gender?.HairList != null ? new List<int>(gender.HairList) : new List<int>();
        }

        private static List<int> CurrentHairColorList(this UICreateRoleComponent self)
        {
            List<int> colors = new(RoleConfigHelper.Preview.HairColorList);
            int defColor = self.CurrentHairStyle()?.DefaultColor ?? 0;
            if (defColor > 0 && !colors.Contains(defColor))
            {
                colors.Add(defColor);
            }
            return colors;
        }

        private static HairStyleConfig CurrentHairStyle(this UICreateRoleComponent self)
        {
            List<int> hairList = self.CurrentHairList();
            if (hairList.Count == 0)
            {
                return null;
            }
            int index = Mathf.Clamp(self.hairIndex, 0, hairList.Count - 1);
            return RoleConfigHelper.HairStyle(hairList[index]);
        }

        private static HairColorConfig CurrentHairColor(this UICreateRoleComponent self)
        {
            List<int> colors = self.CurrentHairColorList();
            if (colors.Count == 0)
            {
                return null;
            }
            int index = Mathf.Clamp(self.hairColorIndex, 0, colors.Count - 1);
            return RoleConfigHelper.HairColor(colors[index]);
        }

        public static RoleParts BuildParts(this UICreateRoleComponent self)
        {
            CharacterSelectList role = RoleConfigHelper.MakeRole(self.classIndex);
            if (role == null)
            {
                return null;
            }

            NpcConfig npc = RoleConfigHelper.Npc(self.gender == 2 ? role.FemaleNpcId : role.MaleNpcId);
            RoleParts parts = RoleParts.FromNpc(npc);
            if (parts == null)
            {
                return null;
            }

            List<int> hairList = self.CurrentHairList();
            if (hairList.Count > 0)
            {
                parts.Hair = hairList[Mathf.Clamp(self.hairIndex, 0, hairList.Count - 1)];
            }

            List<int> colors = self.CurrentHairColorList();
            if (colors.Count > 0)
            {
                parts.HairColorIndex = colors[Mathf.Clamp(self.hairColorIndex, 0, colors.Count - 1)];
            }

            List<int> accessories = new(RoleConfigHelper.Preview.Accessories);
            if (self.headIndex >= 0 && self.headIndex < accessories.Count)
            {
                parts.Head = accessories[self.headIndex];
            }

            parts.Gender = self.gender;
            return parts;
        }

        public static RoleParts BuildCreatedRoleParts(this UICreateRoleComponent self)
        {
            bool female = self.gender == 2;

            ClassConfig novice = RoleConfigHelper.Class(NoviceClassId);
            RoleParts parts = new()
            {
                Body = female ? novice.FemaleBody : novice.MaleBody,
                Eye = female ? NoviceFemaleEye : NoviceMaleEye,
                RightWeapon = NoviceDefaultWeapon,
                Gender = self.gender,
            };

            List<int> hairList = self.CurrentHairList();
            if (hairList.Count > 0)
            {
                parts.Hair = hairList[Mathf.Clamp(self.hairIndex, 0, hairList.Count - 1)];
            }

            List<int> colors = self.CurrentHairColorList();
            if (colors.Count > 0)
            {
                parts.HairColorIndex = colors[Mathf.Clamp(self.hairColorIndex, 0, colors.Count - 1)];
            }

            return parts;
        }

        [EntitySystem]
        private static void Update(this UICreateRoleComponent self)
        {
            if (self.busy)
            {
                return;
            }

            if (!self.introRevealed)
            {
                CharacterSelectComponent intro = self.Root().GetComponent<CharacterSelectComponent>();
                if (intro != null && !intro.IsIntroDone())
                {
                    return;
                }

                self.introRevealed = true;
                self.ShowTitle();
                Log.Info("[创角界面] 进场镜头过场结束，显示 返回 + Banner");
                return;
            }

            CharacterSelectComponent select = self.Root().GetComponent<CharacterSelectComponent>();

            if (self.awaitingReveal)
            {
                if (select == null || select.IsSelectPhaseIdle())
                {
                    self.awaitingReveal = false;
                    self.ShowOccupationDetail();
                    Log.Info("[创角界面] 镜头推进结束，展开创角界面");
                }
                return;
            }

            if (self.awaitingCollapse)
            {
                if (select == null || select.IsSelectPhaseIdle())
                {
                    self.awaitingCollapse = false;
                    self.selectedClass = 0;
                    self.ShowTitle();
                    Log.Info("[创角界面] 镜头回退结束，收回创角界面");
                }
                return;
            }

            if (self.selectedClass != 0)
            {
                return;
            }

            bool down = Input.IsMouseButtonPressed(MouseButton.Left);
            if (down && !self.mouseDown)
            {
                self.mouseDown = true;

                int classIndex = select?.PickClassAtMouse() ?? -1;
                if (classIndex > 0)
                {
                    self.SelectClass(classIndex);
                }
            }
            else if (!down)
            {
                self.mouseDown = false;
            }
        }

        private static void HideAll(this UICreateRoleComponent self)
        {
            SetVisible(self.returnBtn, false);
            SetVisible(self.titleBanner, false);
            SetVisible(self.leftPanel, false);
            SetVisible(self.rightPanel, false);
            SetVisible(self.nameInput, false);
        }

        private static void ShowTitle(this UICreateRoleComponent self)
        {
            SetVisible(self.returnBtn, true);
            SetVisible(self.titleBanner, true);
            SetVisible(self.leftPanel, false);
            SetVisible(self.rightPanel, false);
            SetVisible(self.nameInput, false);
        }

        private static void ShowOccupationDetail(this UICreateRoleComponent self)
        {
            SetVisible(self.returnBtn, true);
            SetVisible(self.titleBanner, false);
            SetVisible(self.leftPanel, true);
            SetVisible(self.rightPanel, true);
            SetVisible(self.nameInput, true);
        }

        private static void SetVisible(CanvasItem node, bool visible)
        {
            if (node != null && GodotObject.IsInstanceValid(node))
            {
                node.Visible = visible;
            }
        }

        private static int CountIcons(List<TextureRect> icons)
        {
            int count = 0;
            foreach (TextureRect icon in icons)
            {
                if (icon != null && GodotObject.IsInstanceValid(icon) && icon.Texture != null)
                {
                    ++count;
                }
            }
            return count;
        }

        private static void SetSlotIcon(List<TextureRect> icons, int index, string fileName)
        {
            if (index < 0 || index >= icons.Count)
            {
                return;
            }

            TextureRect icon = icons[index];
            if (icon == null || !GodotObject.IsInstanceValid(icon))
            {
                return;
            }

            Texture2D texture = null;
            if (!string.IsNullOrEmpty(fileName))
            {
                string path = $"{IconDir}{fileName}";
                if (ResourceLoader.Exists(path))
                {
                    texture = ResourceLoader.Load<Texture2D>(path);
                }
            }
            icon.Texture = texture;
        }

        private static void SetVisibleAt(List<Control> nodes, int index, bool visible)
        {
            if (index < 0 || index >= nodes.Count)
            {
                return;
            }

            Control node = nodes[index];
            if (node != null && GodotObject.IsInstanceValid(node))
            {
                node.Visible = visible;
            }
        }

        public static void SyncToScene(this UICreateRoleComponent self)
        {
            Scene scene = self.Root();
            scene.GetComponent<CharacterSelectComponent>()?.ApplyAppearance(self.BuildParts(), self.classIndex, self.gender);
        }

        public static void SelectClass(this UICreateRoleComponent self, int classIndex)
        {
            if (self.selectedClass != 0 || self.awaitingReveal)
            {
                return;
            }

            self.SetClassIndex(classIndex);
            self.selectedClass = classIndex;
            self.awaitingReveal = true;

            CharacterSelectComponent select = self.Root().GetComponent<CharacterSelectComponent>();
            select?.EnterSelect(classIndex, self.gender);
        }

        public static void SetClassIndex(this UICreateRoleComponent self, int classIndex)
        {
            if (self.classIndex == classIndex)
            {
                return;
            }

            self.classIndex = classIndex;
            self.InitData();
            self.Refresh();
            self.SyncToScene();

            Log.Info($"[创角界面] 选中职业 {classIndex}（{RoleConfigHelper.MakeRole(classIndex)?.Name}），"
                    + $"性别 {self.gender}");
        }

        public static void SetGender(this UICreateRoleComponent self, int gender)
        {
            if (self.gender == gender)
            {
                return;
            }

            self.gender = gender;

            CharacterSelectList role = RoleConfigHelper.MakeRole(self.classIndex);
            if (role != null)
            {
                self.ResetAppearanceForGender(role);
            }

            self.Refresh();
            self.SyncToScene();

            Log.Info($"[创角界面] 切性别：职业 {self.classIndex} → {(gender == 2 ? "女" : "男")}（只切这一组，其余 NPC 保持各自默认）");
        }

        public static void StepHair(this UICreateRoleComponent self, int delta)
        {
            List<int> hairList = self.CurrentHairList();
            if (hairList.Count == 0)
            {
                return;
            }

            self.hairIndex = (self.hairIndex + delta + hairList.Count) % hairList.Count;
            self.Refresh();
            self.SyncToScene();
        }

        public static void OnClickHairSlot(this UICreateRoleComponent self, int index)
        {
            if (index < 0 || index >= self.CurrentHairList().Count)
            {
                return;
            }
            self.hairIndex = index;
            self.Refresh();
            self.SyncToScene();
        }

        public static void OnClickHeadSlot(this UICreateRoleComponent self, int index)
        {
            if (index < 0 || index >= RoleConfigHelper.Preview.Accessories.Length)
            {
                return;
            }
            self.headIndex = index;
            self.Refresh();
            self.SyncToScene();
        }

        public static void StepHairColor(this UICreateRoleComponent self, int delta)
        {
            List<int> colors = self.CurrentHairColorList();
            if (colors.Count == 0)
            {
                return;
            }

            self.hairColorIndex = (self.hairColorIndex + delta + colors.Count) % colors.Count;
            self.Refresh();
            self.SyncToScene();
        }

        public static void OnClickReturn(this UICreateRoleComponent self)
        {
            if (self.busy || self.awaitingReveal || self.awaitingCollapse)
            {
                return;
            }

            if (self.selectedClass != 0)
            {
                CharacterSelectComponent select = self.Root().GetComponent<CharacterSelectComponent>();
                select?.ExitSelect();
                self.awaitingCollapse = true;
                Log.Info("[创角界面] 已点「返回」：先收回创角界面（镜头回退）");
                return;
            }

            self.busy = true;
            LeaveAsync(self).Coroutine();
        }

        private static async ETTask LeaveAsync(UICreateRoleComponent self)
        {
            EntityRef<UICreateRoleComponent> selfRef = self;
            Scene scene = self.Root();

            try
            {
                await CharacterSelectHelper.LeaveAsync(scene);
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

        public static void OnClickEnter(this UICreateRoleComponent self)
        {
            if (self.busy)
            {
                return;
            }

            string name = self.nameInput?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                Log.Info("[创角界面] 名字为空，先点名字框输入再进游戏");
                return;
            }

            int nameBytes = System.Text.Encoding.UTF8.GetByteCount(name);
            if (nameBytes < RoleConfigHelper.System.NameLenMin || nameBytes > RoleConfigHelper.System.NameLenMax)
            {
                Log.Info($"[创角界面] 名字「{name}」{nameBytes} 字节，不在 "
                        + $"{RoleConfigHelper.System.NameLenMin}..{RoleConfigHelper.System.NameLenMax} 之间，不允许进入");
                return;
            }

            self.busy = true;
            EnterGameAsync(self).Coroutine();
        }

        private static async ETTask EnterGameAsync(UICreateRoleComponent self)
        {
            EntityRef<Scene> sceneRef = self.Root();

            RoleParts parts = self.BuildCreatedRoleParts();
            string name = self.nameInput?.Text ?? string.Empty;
            int gender = self.gender;
            int classIndex = self.classIndex;
            int destClassId = RoleConfigHelper.MakeRole(classIndex)?.ClassId ?? 0;

            try
            {
                Log.Info($"[创角界面] 进入游戏：名字「{name}」目标职业序号 {classIndex} 性别 {gender} 外观 [{parts}]");

                Scene scene = sceneRef;
                if (scene == null)
                {
                    return;
                }

                ClientRoleListComponent roleList = scene.GetComponent<ClientRoleListComponent>();
                int sequence = roleList != null ? roleList.FirstEmptySequence(ClientRoleHelper.SlotCount) : -1;
                if (sequence < 0)
                {
                    Log.Error("[创角界面] 拿不到角色列表 / 存档位都占满了，不建角色也不进图");
                    return;
                }

                RoleOpResult createResult = await ClientRoleHelper.CreateRoleAsync(sceneRef, sequence, name, gender, destClassId, parts);
                if (!createResult.IsSuccess)
                {
                    Log.Error($"[创角界面] 创角失败（没进图）：Error={createResult.Error} {createResult.Message}");
                    return;
                }

                scene = sceneRef;
                if (scene == null)
                {
                    return;
                }

                scene.GetComponent<CharacterSelectComponent>()?.PlayHappyAnim(gender);

                CreatedRoleComponent created = scene.GetComponent<CreatedRoleComponent>();
                if (created == null)
                {
                    created = scene.AddComponent<CreatedRoleComponent>();
                }
                created.RoleName = name;
                created.Parts = parts;

                await UIHelper.Remove(scene, UIType.CreateRoleViewV2);

                scene = sceneRef;
                if (scene == null)
                {
                    return;
                }

                scene.GetComponent<CharacterSelectComponent>()?.Deactivate();

                ResourcesLoaderComponent loader = scene.GetComponent<ResourcesLoaderComponent>();
                if (loader != null)
                {
                    loader.UnloadScene(CharacterSelectComponentSystem.ScenePath);
                    loader.UnloadScene(CharacterChooseComponentSystem.ScenePath);
                }

                Log.Info("[创角界面] 开始进首图（C2G_EnterMap）");
                await EnterMapHelper.EnterMapAsync(scene, sequence);
                Log.Info("[创角界面] 已进入首图");
            }
            catch (Exception e)
            {
                Log.Error($"[创角界面] 进入游戏失败：{e}");
            }
        }

        public static void Refresh(this UICreateRoleComponent self)
        {
            CharacterSelectList role = RoleConfigHelper.MakeRole(self.classIndex);
            ClassConfig classRow = role != null ? RoleConfigHelper.Class(role.ClassId) : null;

            if (self.femaleCover != null)
            {
                self.femaleCover.Visible = self.gender == 1;
            }

            List<int> hairList = self.CurrentHairList();
            for (int i = 0; i < self.hairFrames.Count; ++i)
            {
                if (self.hairFrames[i] == null)
                {
                    continue;
                }
                bool usable = i < hairList.Count;
                bool selected = usable && i == self.hairIndex;
                self.hairSlots[i].Visible = usable;
                self.hairFrames[i].Modulate = selected ? SelectedTint() : NormalTint();
                SetVisibleAt(self.hairSelects, i, selected);
                SetSlotIcon(self.hairIcons, i, usable ? $"hairicon_{hairList[i]}.png" : null);
            }

            HairColorConfig color = self.CurrentHairColor();
            if (self.hairColorLab != null)
            {
                self.hairColorLab.Text = color?.NameZh ?? "--";
            }

            int accCount = RoleConfigHelper.Preview.Accessories.Length;
            for (int i = 0; i < self.headFrames.Count; ++i)
            {
                if (self.headFrames[i] == null)
                {
                    continue;
                }
                bool usable = i < accCount;
                self.headSlots[i].Visible = usable;
                self.headFrames[i].Modulate = (usable && i == self.headIndex) ? SelectedTint() : NormalTint();
                SetSlotIcon(self.headIcons, i, usable
                        ? $"headicon_{RoleConfigHelper.Preview.Accessories[i]}.png"
                        : null);
            }

            if (classRow != null)
            {
                if (self.profLabel != null)
                {
                    self.profLabel.Text = classRow.NameZh;
                }
                if (self.profLabelEng != null)
                {
                    self.profLabelEng.Text = classRow.NameEn;
                }
                if (self.profFlag != null)
                {
                    self.profFlag.Modulate = CareerFlagColor(classRow.Type);
                }
                if (self.symbolDisc != null)
                {
                    self.symbolDisc.Modulate = CareerIconBgColor(classRow.Type);
                }
                if (self.profIcon != null && !string.IsNullOrEmpty(classRow.Icon))
                {
                    string iconPath = $"{IconDir}{classRow.Icon}.png";
                    if (ResourceLoader.Exists(iconPath))
                    {
                        self.profIcon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
                    }
                }
            }

            self.DrawAttrPolygon(classRow);
        }

        private static void DrawAttrPolygon(this UICreateRoleComponent self, ClassConfig classRow)
        {
            Node2D container = self.attrPolygon;
            if (container == null || !GodotObject.IsInstanceValid(container))
            {
                return;
            }

            foreach (Node child in container.GetChildren())
            {
                container.RemoveChild(child);
                child.QueueFree();
            }

            int[] attrValues = AttrValues(classRow);
            if (attrValues == null)
            {
                return;
            }

            (Color inside, Color outside) = AttrPolygonColors(self.classIndex);
            inside.A = AttrPolygonAlpha;
            outside.A = AttrPolygonAlpha;

            string[] keys = AttrNodeNames();
            Vector2 center = new(AttrCenterX, AttrCenterY);
            Vector2[] verts = new Vector2[keys.Length];
            for (int i = 0; i < keys.Length; ++i)
            {
                int value = attrValues[i];
                float len = AttrRadius * (value / 100f);
                float rad = Mathf.DegToRad(90f - 60f * i);
                verts[i] = new Vector2(center.X + len * Mathf.Cos(rad), center.Y - len * Mathf.Sin(rad));
            }

            Polygon2D fill = new() { Name = "Fill", Antialiased = true };
            Vector2[] polygon = new Vector2[verts.Length + 1];
            Color[] colors = new Color[verts.Length + 1];
            polygon[0] = center;
            colors[0] = inside;
            for (int i = 0; i < verts.Length; ++i)
            {
                polygon[i + 1] = verts[i];
                colors[i + 1] = outside;
            }
            fill.Polygon = polygon;
            fill.VertexColors = colors;
            container.AddChild(fill);

            Line2D outline = new()
            {
                Name = "Outline",
                Points = verts,
                Closed = true,
                Width = 2f,
                DefaultColor = new Color(1f, 1f, 1f, 0.478f),
                Antialiased = true,
            };
            container.AddChild(outline);

            for (int i = 0; i < verts.Length; ++i)
            {
                Polygon2D dot = new()
                {
                    Name = $"P{i + 1}",
                    Color = Colors.White,
                    Polygon = CirclePoints(verts[i], 6f, 12),
                };
                container.AddChild(dot);
            }
        }

        private static Vector2[] CirclePoints(Vector2 center, float radius, int segments)
        {
            Vector2[] pts = new Vector2[segments];
            for (int i = 0; i < segments; ++i)
            {
                float a = Mathf.Tau * i / segments;
                pts[i] = new Vector2(center.X + radius * Mathf.Cos(a), center.Y + radius * Mathf.Sin(a));
            }
            return pts;
        }
    }
}
