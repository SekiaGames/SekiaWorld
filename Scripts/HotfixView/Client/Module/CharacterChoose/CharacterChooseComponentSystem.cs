using System;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(CharacterChooseComponent))]
    [FriendOf(typeof(CharacterChooseComponent))]
    public static partial class CharacterChooseComponentSystem
    {
        public const string ScenePath = "res://Scene/CharacterChoose/SceneCharacterChoose.tscn";

        private const string CameraNodePath = "Main Camera";

        private const string RoleMountNodePath = "RoleMount";

        [EntitySystem]
        private static void Awake(this CharacterChooseComponent self)
        {
        }

        public static void StartLoad(this CharacterChooseComponent self)
        {
            if (self.loadTcs != null)
            {
                return;
            }

            self.loadTcs = ETTask.Create();
            LoadAsync(self).Coroutine();
        }

        public static async ETTask<bool> WaitSceneAsync(this CharacterChooseComponent self)
        {
            EntityRef<CharacterChooseComponent> selfRef = self;

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

        public static void ForgetUnloadedScene(this CharacterChooseComponent self)
        {
            if (self.sceneNode == null || GodotObject.IsInstanceValid(self.sceneNode))
            {
                return;
            }

            self.HideRole();

            self.sceneNode = null;
            self.camera = null;
            self.roleMount = null;
            self.active = false;
            self.loadTcs = null;

            Log.Info("[选角] 场景已被卸载过（进游戏时卸的），缓存状态已清掉，重新加载");
        }

        public static void Activate(this CharacterChooseComponent self)
        {
            if (self.sceneNode == null)
            {
                Log.Error($"[选角] 场景还没加载好（{ScenePath}），没法激活");
                return;
            }

            self.camera ??= self.sceneNode.GetNodeOrNull<Camera3D>(CameraNodePath);
            if (self.camera == null)
            {
                Log.Error($"[选角] 场景里找不到相机节点「{CameraNodePath}」（{ScenePath}），画面会是黑的");
                return;
            }

            self.camera.Current = true;
            self.active = true;

            Log.Info($"[选角] 已激活：相机「{CameraNodePath}」pos={self.camera.Position} "
                    + $"rot={self.camera.RotationDegrees} fov={self.camera.Fov}");
        }

        public static void Deactivate(this CharacterChooseComponent self)
        {
            if (self.camera != null && GodotObject.IsInstanceValid(self.camera))
            {
                self.camera.Current = false;
            }
            self.active = false;
        }

        public static void ShowRole(this CharacterChooseComponent self, RoleParts parts)
        {
            self.HideRole();

            if (parts == null)
            {
                return;
            }

            if (self.roleMount == null || !GodotObject.IsInstanceValid(self.roleMount))
            {
                Log.Error("[选角] 没有挂点，摆不了角色（见 RoleMountNodePath）");
                return;
            }

            RoleComponent role = self.AddChild<RoleComponent, RoleParts>(parts);
            role.AttachTo(self.roleMount, Vector3.Zero, Vector3.Zero);
            role.PlayAction("wait");
            self.role = role;

            Log.Info($"[选角] 已把角色摆到挂点上：{parts}");
        }

        public static void HideRole(this CharacterChooseComponent self)
        {
            if (self.role == null)
            {
                return;
            }

            RoleComponent role = self.role;
            self.role = null;
            role.Dispose();
        }

        private static async ETTask LoadAsync(CharacterChooseComponent self)
        {
            ETTask loadTcs = self.loadTcs;

            try
            {
                Scene root = self.Root();
                EntityRef<CharacterChooseComponent> selfRef = self;

                Node sceneNode = await root.GetComponent<ResourcesLoaderComponent>().LoadSceneAsync(ScenePath);

                self = selfRef;
                if (self == null)
                {
                    return;
                }

                self.sceneNode = sceneNode;

                self.roleMount = sceneNode.GetNodeOrNull<Node3D>(RoleMountNodePath);
                if (self.roleMount == null)
                {
                    Log.Error($"[选角] 场景里找不到角色挂点「{RoleMountNodePath}」（{ScenePath}），屏幕中间不会有角色");
                }

                Log.Info($"[选角] 场景已加载：{ScenePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[选角] 场景加载失败：{ScenePath}\n{e}");
            }
            finally
            {
                loadTcs?.SetResult();
            }
        }
    }
}
