using System;
using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(CharacterSelectComponent))]
    [FriendOf(typeof(CharacterSelectComponent))]
    public static partial class CharacterSelectComponentSystem
    {
        public const string ScenePath = "res://Scene/CharacterSelect/SceneCharacterSelect.tscn";

        private const string CameraNodePath = "CameraRoot/Root/Main Camera";

        private const string CameraAnimNodePath = "CameraRoot/Root/CameraAnim";

        private const string SelectedCrossAction = "choose_turn";

        private const string UnselectedCrossAction = "choose_turn2";

        private const string AltSelectionIdle = "choose_wait2";

        private const string HappyAction = "choose_show";

        private const string IntroAnim = "OP";

        private const string IdleAnim = "wait";

        private const string SelectAnimPrefix = "wait";

        private const string RolesRootPath = "CameraRoot/Root/Characters";

        private const string RootNodePath = "CameraRoot/Root";

        private const int ClassCount = 6;

        private const uint PickLayer = 1 << 1;

        private const int PhaseIdle = 0;

        private const int PhaseSelecting = 1;

        private const int PhaseUnselecting = 2;

        [EntitySystem]
        private static void Awake(this CharacterSelectComponent self)
        {
        }

        public static void StartLoad(this CharacterSelectComponent self)
        {
            if (self.loadTcs != null)
            {
                return;
            }

            self.loadTcs = ETTask.Create();
            LoadAsync(self).Coroutine();
        }

        public static async ETTask<bool> WaitSceneAsync(this CharacterSelectComponent self)
        {
            EntityRef<CharacterSelectComponent> selfRef = self;

            self.ForgetUnloadedScene();
            self.StartLoad();

            ETTask loadTcs = self.loadTcs;
            if (loadTcs != null)
            {
                await loadTcs;
            }

            self = selfRef;
            if (self == null)
            {
                return false;
            }

            return self.sceneNode != null;
        }

        public static void ForgetUnloadedScene(this CharacterSelectComponent self)
        {
            if (self.sceneNode == null || GodotObject.IsInstanceValid(self.sceneNode))
            {
                return;
            }

            foreach (RoleGroup group in self.groups)
            {
                DisposeRole(group.MaleRole);
                DisposeRole(group.FemaleRole);
                DisposeRole(group.PetRole);
            }

            self.groups.Clear();
            self.sceneNode = null;
            self.camera = null;
            self.cameraAnim = null;
            self.rolesRoot = null;
            self.rolesReady = false;
            self.active = false;
            self.introPlaying = false;
            self.introDone = false;
            self.selectAnim = null;
            self.selectPhase = PhaseIdle;
            self.stashedMale = null;
            self.stashedFemale = null;
            self.loadTcs = null;

            Log.Info("[创角] 场景已被卸载过（进游戏时卸的），缓存状态已清掉，重新加载");
        }

        private static void DisposeRole(EntityRef<RoleComponent> roleRef)
        {
            RoleComponent role = roleRef;
            if (role != null)
            {
                role.Dispose();
            }
        }

        public static void Activate(this CharacterSelectComponent self)
        {
            if (self.sceneNode == null)
            {
                Log.Error($"[创角] 场景还没加载好（{ScenePath}），没法激活");
                return;
            }

            self.camera ??= self.sceneNode.GetNodeOrNull<Camera3D>(CameraNodePath);
            if (self.camera == null)
            {
                Log.Error($"[创角] 场景里找不到相机节点「{CameraNodePath}」（{ScenePath}），画面会是黑的");
                return;
            }

            self.camera.Current = true;
            self.active = true;

            self.SetupRoles();
            self.PlayIntro();

            Log.Info($"[创角] 已激活：相机 pos={self.camera.Position} fov={self.camera.Fov}，"
                    + $"角色 {self.groups.Count} 组");
        }

        public static void PlayIntro(this CharacterSelectComponent self)
        {
            self.introDone = false;
            self.introPlaying = false;

            if (self.sceneNode == null)
            {
                self.introDone = true;
                return;
            }

            self.cameraAnim = self.sceneNode.GetNodeOrNull<AnimationPlayer>(CameraAnimNodePath);
            if (self.cameraAnim == null || !self.cameraAnim.HasAnimation(IntroAnim))
            {
                Log.Warning($"[创角] 场景里没有过场动画「{CameraAnimNodePath}/{IntroAnim}」，跳过进场过场");
                self.introDone = true;
                return;
            }

            self.cameraAnim.Play(IntroAnim);
            self.introPlaying = true;
            Log.Info("[创角] 进场镜头过场开始（OP）");
        }

        public static bool IsIntroDone(this CharacterSelectComponent self)
        {
            return self.introDone;
        }

        [EntitySystem]
        private static void Update(this CharacterSelectComponent self)
        {
            if (self.introPlaying)
            {
                UpdateIntro(self);
                return;
            }

            if (self.selectPhase != PhaseIdle)
            {
                UpdateSelectCamera(self);
            }
        }

        private static void UpdateIntro(this CharacterSelectComponent self)
        {
            if (self.cameraAnim == null || !GodotObject.IsInstanceValid(self.cameraAnim))
            {
                self.introPlaying = false;
                self.introDone = true;
                return;
            }

            if (self.cameraAnim.IsPlaying())
            {
                return;
            }

            self.introPlaying = false;
            self.introDone = true;
            if (self.cameraAnim.HasAnimation(IdleAnim))
            {
                self.cameraAnim.Play(IdleAnim);
            }
            Log.Info("[创角] 进场镜头过场结束，切到静止位（wait）");
        }

        private static void UpdateSelectCamera(this CharacterSelectComponent self)
        {
            if (self.cameraAnim != null && GodotObject.IsInstanceValid(self.cameraAnim)
                && self.cameraAnim.IsPlaying())
            {
                return;
            }

            if (self.selectPhase == PhaseSelecting)
            {
                self.selectPhase = PhaseIdle;
                StashSelected(self);
                Log.Info($"[创角] 镜头推进完成（{self.selectAnim}）");
            }
            else if (self.selectPhase == PhaseUnselecting)
            {
                self.selectPhase = PhaseIdle;
                self.selectAnim = null;
                ShowAllRoles(self);
                PlayUnselectActions(self);
                Log.Info("[创角] 镜头回退完成，恢复其余角色");
            }
        }

        public static void Deactivate(this CharacterSelectComponent self)
        {
            if (self.camera != null && GodotObject.IsInstanceValid(self.camera))
            {
                self.camera.Current = false;
            }
            self.active = false;
        }

        public static void SetupRoles(this CharacterSelectComponent self)
        {
            if (self.rolesReady || self.sceneNode == null)
            {
                return;
            }

            if (RoleConfigHelper.MakeRole(1) == null)
            {
                Log.Error("[创角] 配表没加载，装不了角色");
                return;
            }

            self.rolesRoot = self.sceneNode.GetNodeOrNull<Node3D>(RolesRootPath);
            if (self.rolesRoot == null)
            {
                Log.Error($"[创角] 场景里找不到角色父节点「{RolesRootPath}」（{ScenePath}）");
                return;
            }

            for (int i = 1; i <= ClassCount; ++i)
            {
                CharacterSelectList rc = RoleConfigHelper.MakeRole(i);
                if (rc == null)
                {
                    Log.Warning($"[创角] 配表里没有职业序号 {i}");
                    continue;
                }

                Node3D groupNode = self.rolesRoot.GetNodeOrNull<Node3D>(i.ToString());
                if (groupNode == null)
                {
                    Log.Warning($"[创角] 场景里没有 Characters/{i} 节点");
                    continue;
                }

                RoleGroup group = new()
                {
                    Node = groupNode,
                    MaleNode = groupNode.GetNodeOrNull<Node3D>($"NPC_{rc.MaleNpcId}_0"),
                    FemaleNode = groupNode.GetNodeOrNull<Node3D>($"NPC_{rc.FemaleNpcId}_0"),
                    DefaultGender = rc.DefaultGender,
                    MaleIdle = IdleActionOf(rc.MaleNpcId),
                    FemaleIdle = IdleActionOf(rc.FemaleNpcId),
                };

                group.MaleRole = CreateRole(self, rc.MaleNpcId, 1, group.MaleNode, group.MaleIdle);
                group.FemaleRole = CreateRole(self, rc.FemaleNpcId, 2, group.FemaleNode, group.FemaleIdle);
                group.SetGenderVisible(rc.DefaultGender);
                self.groups.Add(group);

                if (rc.PetNpcId > 0)
                {
                    group.PetNode = self.sceneNode.GetNodeOrNull<Node3D>($"{RootNodePath}/NPC_{rc.PetNpcId}_0");
                    group.PetIdle = IdleActionOf(rc.PetNpcId);
                    group.PetRole = CreateRole(self, rc.PetNpcId, 0, group.PetNode, group.PetIdle);
                }
            }

            self.rolesReady = true;
            self.activeClassIndex = 1;
            self.activeGender = RoleConfigHelper.MakeRole(1)?.DefaultGender ?? 1;

            int pets = 0;
            foreach (RoleGroup g in self.groups)
            {
                if (g.PetNode != null)
                {
                    ++pets;
                }
            }
            Log.Info($"[创角] 角色装配完成：{self.groups.Count} 组（每组男女各一），宠物 {pets} 只");

            System.Text.StringBuilder summary = new();
            for (int i = 0; i < self.groups.Count; ++i)
            {
                RoleGroup g = self.groups[i];
                string male = g.Role(1)?.CurrentAction ?? g.MaleIdle;
                string female = g.Role(2)?.CurrentAction ?? g.FemaleIdle;
                bool isFemale = g.DefaultGender == 2;
                summary.Append($"{i + 1}:默认{(isFemale ? "女" : "男")}={(isFemale ? female : male)}"
                        + $"（男={male}，女={female}");
                if (g.PetNode != null)
                {
                    RoleComponent pet = g.PetRole;
                    summary.Append($"，宠物={pet?.CurrentAction ?? g.PetIdle}");
                }
                summary.Append("）； ");
            }
            Log.Info($"[创角] 各组默认性别/待机：{summary}".TrimEnd());
        }

        private static EntityRef<RoleComponent> CreateRole(CharacterSelectComponent self,
                int npcId, int gender, Node3D parent, string idleAction)
        {
            if (parent == null)
            {
                Log.Warning($"[创角] 找不到挂载节点（npc {npcId}）");
                return default;
            }

            NpcConfig npc = RoleConfigHelper.Npc(npcId);
            RoleParts parts = RoleParts.FromNpc(npc);
            if (parts == null)
            {
                Log.Warning($"[创角] Table_Npc 里没有 npc {npcId}");
                return default;
            }

            parts.Gender = gender;
            RoleComponent role = self.AddChild<RoleComponent, RoleParts>(parts);
            role.AttachTo(parent, Vector3.Zero, Vector3.Zero);
            if (!string.IsNullOrEmpty(idleAction))
            {
                role.PlayAction(ResolveIdleAction(role, idleAction));
            }
            return role;
        }

        private static string ResolveIdleAction(RoleComponent role, string wanted)
        {
            if (HasAction(role, wanted))
            {
                return wanted;
            }

            if (wanted != AltSelectionIdle && HasAction(role, AltSelectionIdle))
            {
                Log.Info($"[创角] 模型没有待机动作「{wanted}」（Body {role.Parts?.Body}），退回 {AltSelectionIdle}");
                return AltSelectionIdle;
            }

            Log.Info($"[创角] 模型没有待机动作「{wanted}」（Body {role.Parts?.Body}），退回 {IdleAnim}");
            return HasAction(role, IdleAnim) ? IdleAnim : wanted;
        }

        private static bool HasAction(RoleComponent role, string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                return false;
            }

            if (role.AnimTree != null && GodotObject.IsInstanceValid(role.AnimTree)
                    && role.AnimTree.TreeRoot is AnimationNodeStateMachine sm && sm.HasNode(action))
            {
                return true;
            }

            return role.AnimPlayer != null && GodotObject.IsInstanceValid(role.AnimPlayer)
                    && role.AnimPlayer.HasAnimation(action);
        }

        public static int PickClassAtMouse(this CharacterSelectComponent self)
        {
            Viewport viewport = GlobalComponent.RootNode.GetViewport();

            if (viewport.GuiGetHoveredControl() != null)
            {
                return -1;
            }

            return PickClassAtScreen(self, viewport.GetMousePosition());
        }

        public static int PickClassAtScreen(this CharacterSelectComponent self, Vector2 screen)
        {
            if (!self.active || !self.rolesReady || self.selectPhase != PhaseIdle)
            {
                return -1;
            }

            Camera3D camera = GlobalComponent.RootNode.GetViewport().GetCamera3D();
            if (camera == null)
            {
                return -1;
            }

            Vector3 from = camera.ProjectRayOrigin(screen);
            Vector3 to = from + camera.ProjectRayNormal(screen) * 1000f;

            PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to, PickLayer);
            Godot.Collections.Dictionary hit = camera.GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (hit.Count == 0 || hit["collider"].As<Node>() is not Node collider)
            {
                return -1;
            }

            return ClassIndexOfCollider(self, collider);
        }

        public static string SelfCheckPicking(this CharacterSelectComponent self)
        {
            if (!self.rolesReady || self.groups.Count == 0)
            {
                return "自检跳过：角色还没装配";
            }

            Viewport viewport = GlobalComponent.RootNode.GetViewport();
            Camera3D camera = viewport.GetCamera3D();
            if (camera == null)
            {
                return "自检跳过：没有相机";
            }

            Rect2 view = new(Vector2.Zero, viewport.GetVisibleRect().Size);
            int checkedCount = 0;
            System.Text.StringBuilder bad = new();

            for (int i = 0; i < self.groups.Count; ++i)
            {
                Node3D node = self.groups[i].Node;
                if (node == null || !GodotObject.IsInstanceValid(node))
                {
                    continue;
                }

                Vector3 aim = node.GlobalPosition + Vector3.Up * 0.9f;
                if (camera.IsPositionBehind(aim))
                {
                    continue;
                }

                Vector2 screen = camera.UnprojectPosition(aim);
                if (!view.HasPoint(screen))
                {
                    continue;
                }

                ++checkedCount;
                int got = PickClassAtScreen(self, screen);
                if (got != i + 1)
                {
                    bad.Append($"{i + 1}→{(got < 0 ? "-" : got.ToString())} ");
                }
            }

            if (checkedCount == 0)
            {
                return "自检跳过：没有 NPC 在画面里";
            }

            return bad.Length == 0 ? $"全部命中（{checkedCount}/{self.groups.Count}）" : $"有误判：{bad}".TrimEnd();
        }

        private static int ClassIndexOfCollider(CharacterSelectComponent self, Node collider)
        {
            Node node = collider;
            while (node != null)
            {
                Node parent = node.GetParent();
                if (parent == self.rolesRoot)
                {
                    return int.TryParse(node.Name.ToString(), out int index) ? index : -1;
                }
                node = parent;
            }
            return -1;
        }

        public static bool IsSelectPhaseIdle(this CharacterSelectComponent self)
        {
            return self.selectPhase == PhaseIdle;
        }

        public static void EnterSelect(this CharacterSelectComponent self, int classIndex, int gender)
        {
            if (!self.rolesReady || classIndex < 1 || classIndex > self.groups.Count)
            {
                return;
            }
            if (self.selectPhase != PhaseIdle)
            {
                return;
            }

            self.activeClassIndex = classIndex;
            self.activeGender = gender;

            RoleGroup group = self.groups[classIndex - 1];
            PlayActionSafe(group.Role(1), SelectedCrossAction, group.MaleIdle);
            PlayActionSafe(group.Role(2), SelectedCrossAction, group.FemaleIdle);
            if (group.PetNode != null)
            {
                RoleComponent pet = group.PetRole;
                PlayActionSafe(pet, AltSelectionIdle, group.PetIdle ?? IdleAnim);
            }
            HideOtherRoles(self, classIndex);

            string anim = SelectAnimPrefix + classIndex;
            if (!PlayCamera(self, anim, 1f, false))
            {
                Log.Warning($"[创角] 场景里没有选中镜头动画「{CameraAnimNodePath}/{anim}」，跳过镜头推进");
                return;
            }

            self.selectAnim = anim;
            self.selectPhase = PhaseSelecting;
            Log.Info($"[创角] 点选职业 {classIndex}：镜头推进（{anim}）");
        }

        public static void ExitSelect(this CharacterSelectComponent self)
        {
            if (string.IsNullOrEmpty(self.selectAnim) || self.selectPhase == PhaseUnselecting)
            {
                return;
            }

            ApplySelected(self);

            if (!PlayCamera(self, self.selectAnim, -1f, true))
            {
                self.selectPhase = PhaseIdle;
                self.selectAnim = null;
                ShowAllRoles(self);
                return;
            }

            self.selectPhase = PhaseUnselecting;
            Log.Info($"[创角] 反选：镜头回退（{self.selectAnim}）");
        }

        private static void StashSelected(CharacterSelectComponent self)
        {
            int index = self.activeClassIndex - 1;
            if (index < 0 || index >= self.groups.Count)
            {
                return;
            }

            RoleGroup group = self.groups[index];
            self.stashedMale = group.Role(1)?.Parts?.Clone();
            self.stashedFemale = group.Role(2)?.Parts?.Clone();
        }

        private static void ApplySelected(CharacterSelectComponent self)
        {
            int index = self.activeClassIndex - 1;
            if (index < 0 || index >= self.groups.Count)
            {
                return;
            }

            RoleGroup group = self.groups[index];

            group.SetGenderVisible(group.DefaultGender);
            self.activeGender = group.DefaultGender;

            RestoreStash(group.Role(1), self.stashedMale);
            RestoreStash(group.Role(2), self.stashedFemale);

            PlayActionSafe(group.Role(1), AltSelectionIdle, group.MaleIdle);
            PlayActionSafe(group.Role(2), AltSelectionIdle, group.FemaleIdle);
            if (group.PetNode != null)
            {
                RoleComponent pet = group.PetRole;
                PlayActionSafe(pet, AltSelectionIdle, group.PetIdle ?? IdleAnim);
            }

            Log.Info($"[创角] 反选还原：职业 {self.activeClassIndex} 回到默认性别 {group.DefaultGender}，外观回暂存");
        }

        private static void RestoreStash(RoleComponent role, RoleParts stashed)
        {
            if (role == null || stashed == null)
            {
                return;
            }

            role.Parts = stashed.Clone();
            role.Rebuild();
        }

        private static bool PlayCamera(CharacterSelectComponent self, string name, float speed, bool fromEnd)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            if (self.cameraAnim == null || !GodotObject.IsInstanceValid(self.cameraAnim)
                || !self.cameraAnim.HasAnimation(name))
            {
                return false;
            }

            self.cameraAnim.Play(name, -1, speed, fromEnd);
            return true;
        }

        private static void HideOtherRoles(CharacterSelectComponent self, int classIndex)
        {
            int hiddenPets = 0;
            for (int i = 0; i < self.groups.Count; ++i)
            {
                bool keep = i == classIndex - 1;
                RoleGroup group = self.groups[i];
                SetNodeVisible(group.Node, keep);
                SetNodeVisible(group.PetNode, keep);
                if (!keep && group.PetNode != null)
                {
                    ++hiddenPets;
                }
            }
            Log.Info($"[创角] 隐藏其余角色：{self.groups.Count - 1} 组 + {hiddenPets} 只宠物");
        }

        private static void ShowAllRoles(CharacterSelectComponent self)
        {
            foreach (RoleGroup group in self.groups)
            {
                SetNodeVisible(group.Node, true);
                SetNodeVisible(group.PetNode, true);
            }
        }

        private static void SetNodeVisible(Node3D node, bool visible)
        {
            if (node != null && GodotObject.IsInstanceValid(node))
            {
                node.Visible = visible;
            }
        }

        public static void PlayHappyAnim(this CharacterSelectComponent self, int gender)
        {
            int index = self.activeClassIndex - 1;
            if (index < 0 || index >= self.groups.Count)
            {
                return;
            }

            RoleGroup group = self.groups[index];
            PlayActionSafe(group.Role(gender), HappyAction, group.IdleAction(gender));
        }

        private static void PlayUnselectActions(CharacterSelectComponent self)
        {
            int index = self.activeClassIndex - 1;
            if (index < 0 || index >= self.groups.Count)
            {
                return;
            }

            RoleGroup group = self.groups[index];
            PlayActionSafe(group.Role(1), UnselectedCrossAction, group.MaleIdle);
            PlayActionSafe(group.Role(2), UnselectedCrossAction, group.FemaleIdle);
            if (group.PetNode != null)
            {
                RoleComponent pet = group.PetRole;
                string petIdle = group.PetIdle ?? IdleAnim;
                PlayActionSafe(pet, petIdle, petIdle);
            }
        }

        private static void PlayActionSafe(RoleComponent role, string action, string wantedIdle)
        {
            if (role == null)
            {
                return;
            }

            if (HasAction(role, action))
            {
                role.PlayAction(action);
                Log.Info($"[创角] 切换动作：{action}（Body {role.Parts?.Body}）");
                return;
            }

            string idle = ResolveIdleAction(role, wantedIdle);
            Log.Info($"[创角] 模型没有动作「{action}」（Body {role.Parts?.Body}），退回「{idle}」");
            role.PlayAction(idle);
        }

        private static string IdleActionOf(int npcId)
        {
            return npcId == 1173 ? "choose_wait2" : "choose_wait";
        }

        public static void ApplyGender(this CharacterSelectComponent self, int classIndex, int gender)
        {
            self.activeGender = gender;

            int index = classIndex - 1;
            if (index < 0 || index >= self.groups.Count)
            {
                return;
            }

            self.groups[index].SetGenderVisible(gender);
        }

        public static void ApplyAppearance(this CharacterSelectComponent self, RoleParts parts, int classIndex, int gender)
        {
            if (!self.rolesReady || parts == null)
            {
                return;
            }

            if (classIndex >= 1 && classIndex <= self.groups.Count)
            {
                self.activeClassIndex = classIndex;
            }

            self.ApplyGender(self.activeClassIndex, gender);

            int index = self.activeClassIndex - 1;
            if (index < 0 || index >= self.groups.Count)
            {
                return;
            }

            RoleComponent role = self.groups[index].Role(gender);
            if (role == null)
            {
                return;
            }

            role.Parts = parts;
            role.Rebuild();

            Log.Info($"[创角] 外观已套用：职业 {self.activeClassIndex} 性别 {gender} [{parts}]");
        }

        private static async ETTask LoadAsync(CharacterSelectComponent self)
        {
            ETTask loadTcs = self.loadTcs;

            try
            {
                Scene root = self.Root();
                EntityRef<CharacterSelectComponent> selfRef = self;

                Node sceneNode = await root.GetComponent<ResourcesLoaderComponent>().LoadSceneAsync(ScenePath);

                self = selfRef;
                if (self == null)
                {
                    return;
                }

                self.sceneNode = sceneNode;
                Log.Info($"[创角] 场景已加载：{ScenePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[创角] 场景加载失败：{ScenePath}\n{e}");
            }
            finally
            {
                loadTcs?.SetResult();
            }
        }
    }
}
