using Godot;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(MainCameraComponent))]
    [FriendOf(typeof(MainCameraComponent))]
    [FriendOf(typeof(Unit))]
    public static partial class MainCameraComponentSystem
    {
        private const string Tag = "[相机]";

        private const string CameraRootName = "CameraRoot";
        private const string CameraName = "MainCamera3D";

        private const float NearPlane = 0.3f;

        private const float FarPlane = 8000f;

        [EntitySystem]
        private static void Awake(this MainCameraComponent self)
        {
            Node rootNode = GlobalComponent.RootNode;
            self.cameraRoot = rootNode?.GetNodeOrNull<Node3D>(CameraRootName);
            if (self.cameraRoot == null)
            {
                Log.Error($"{Tag} Init.tscn 里没有 {CameraRootName} 节点，主相机建不起来");
                return;
            }

            self.camera = new Camera3D
            {
                Name = CameraName,
                Near = NearPlane,
                Far = FarPlane,
            };
            self.cameraRoot.AddChild(self.camera);

            self.focusOffset = Vector3.Zero;
            self.focusViewPort = new Vector3(0.5f, 0.4f, 30f);
            self.rotation = new Vector3(30f, -45f, 0f);
            self.fieldOfView = 20f;
            self.fieldOfViewMin = 18f;
            self.fieldOfViewMax = 63f;
            self.parkedPosition = new Vector3(0f, -100000f, 0f);

            self.ApplyFieldOfView();
            self.ApplyRotation();
            self.RecalcFollowOffset();
            self.Park();

            Log.Info($"{Tag} 主相机已就绪（常驻 {CameraRootName}/{CameraName}，默认停飞）");
        }

        [EntitySystem]
        private static void Update(this MainCameraComponent self)
        {
            if (self.camera == null)
            {
                return;
            }

            if (!self.active || !self.following)
            {
                return;
            }

            Unit unit = UnitHelper.GetMyUnitFromClientScene(self.Scene());
            if (unit == null)
            {
                return;
            }

            float3 pos = unit.Position;
            Vector3 focus = new Vector3(pos.x, pos.y, pos.z) + self.focusOffset;
            self.camera.GlobalPosition = focus + self.followOffset;

            if (!self.followLogged)
            {
                self.followLogged = true;
                Log.Info($"{Tag} 开始跟随玩家：玩家 pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2})，"
                        + $"相机 pos=({self.camera.GlobalPosition.X:F2}, {self.camera.GlobalPosition.Y:F2}, "
                        + $"{self.camera.GlobalPosition.Z:F2})");
            }
        }

        public static void Activate(this MainCameraComponent self, string sceneName)
        {
            if (self.camera == null)
            {
                Log.Error($"{Tag} 相机没建起来，接管失败（先看 Init.tscn 有没有 {CameraRootName}）");
                return;
            }

            self.ApplyMapCameraInfo(sceneName);
            self.ApplyFieldOfView();
            self.ApplyRotation();
            self.RecalcFollowOffset();

            self.active = true;
            self.following = true;
            self.followLogged = false;
            self.camera.Current = true;

            Log.Info($"{Tag} 主相机已就位：{sceneName} 跟随玩家（fov {self.camera.Fov:F1}, 距离 {self.focusViewPort.Z:F1}, "
                    + $"rot ({self.rotation.X:F0}, {self.rotation.Y:F0}, {self.rotation.Z:F0})）");
        }

        private static void ApplyMapCameraInfo(this MainCameraComponent self, string sceneName)
        {
            CameraInfo cam = MapPointsLoader.MainCamera(MapPointsLoader.Load(MapPointsLoader.MapNameOfScene(sceneName)));
            if (cam != null)
            {
                self.focusViewPort = new Vector3(cam.FocusX, cam.FocusY, cam.FocusZ);
                self.fieldOfView = cam.Fov;
                self.mapRotation = new Quaternion(cam.Qx, cam.Qy, cam.Qz, cam.Qw);
                self.rotationFromData = true;
                return;
            }

            Log.Warning($"[相机] {sceneName} 没有布点相机数据 ⇒ 用 prt_1 那套兜底（先跑一次布点导出）");
            self.focusViewPort = new Vector3(0.5f, 0.4f, 30f);
            self.rotation = new Vector3(30f, -45f, 0f);
            self.fieldOfView = 20f;
            self.rotationFromData = false;
        }

        public static void Park(this MainCameraComponent self)
        {
            if (self.camera == null)
            {
                return;
            }

            bool wasActive = self.active;
            self.active = false;
            self.following = false;
            self.followLogged = false;
            self.camera.Current = false;
            self.camera.GlobalPosition = self.parkedPosition;

            if (wasActive)
            {
                Log.Info($"{Tag} 停飞：无地图 / 加载中 / 不在场景里");
            }
        }

        public static string Describe(this MainCameraComponent self)
        {
            if (self.camera == null)
            {
                return $"{Tag} 状态：相机没建起来";
            }

            Vector3 cameraPos = self.camera.GlobalPosition;
            Unit unit = UnitHelper.GetMyUnitFromClientScene(self.Scene());
            if (unit == null)
            {
                return $"{Tag} 状态：active={self.active} following={self.following} 还没进图（没有玩家 Unit），"
                        + $"相机 pos=({cameraPos.X:F2}, {cameraPos.Y:F2}, {cameraPos.Z:F2})";
            }

            float3 pos = unit.Position;
            Vector3 playerPos = new Vector3(pos.x, pos.y, pos.z);
            Vector3 forward = -self.camera.GlobalTransform.Basis.Z;
            return $"{Tag} 状态：active={self.active} following={self.following} fov={self.camera.Fov:F1} "
                    + $"距离={self.focusViewPort.Z:F2} 高度（相对焦点）={self.followOffset.Y:F2} "
                    + $"水平距离={self.HorizontalDistance():F2} "
                    + $"相机朝向=({forward.X:F2}, {forward.Y:F2}, {forward.Z:F2}) "
                    + $"相机 pos=({cameraPos.X:F2}, {cameraPos.Y:F2}, {cameraPos.Z:F2}) "
                    + $"玩家 pos=({playerPos.X:F2}, {playerPos.Y:F2}, {playerPos.Z:F2}) "
                    + $"玩家在视锥内={self.camera.IsPositionInFrustum(playerPos + new Vector3(0f, 1f, 0f))}";
        }

        private static Vector3 GodotEuler(MainCameraComponent self)
        {
            return new Vector3(
                    Mathf.DegToRad(-self.rotation.X),
                    Mathf.DegToRad(180f - self.rotation.Y),
                    Mathf.DegToRad(-self.rotation.Z));
        }

        private static void ApplyFieldOfView(this MainCameraComponent self)
        {
            if (self.camera == null)
            {
                return;
            }

            self.camera.Fov = self.fieldOfView;
        }

        private static void ApplyRotation(this MainCameraComponent self)
        {
            if (self.camera == null)
            {
                return;
            }

            if (self.rotationFromData)
            {
                self.camera.Basis = new Basis(self.mapRotation);
                return;
            }

            self.camera.RotationDegrees = new Vector3(
                    -self.rotation.X,
                    180f - self.rotation.Y,
                    -self.rotation.Z);
        }

        private static void RecalcFollowOffset(this MainCameraComponent self)
        {
            float z = self.focusViewPort.Z;
            float halfFovRad = Mathf.DegToRad(self.fieldOfView * 0.5f);
            float dy = (self.focusViewPort.Y - 0.5f) * 2f * Mathf.Tan(halfFovRad) * z;

            Basis basis = self.rotationFromData && self.camera != null
                    ? self.camera.Basis
                    : Basis.FromEuler(GodotEuler(self));

            self.followOffset = -(basis * new Vector3(0f, dy, -z));
        }

        private static float HorizontalDistance(this MainCameraComponent self)
        {
            return new Vector2(self.followOffset.X, self.followOffset.Z).Length();
        }
    }
}
