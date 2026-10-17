using Godot;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(OperaComponent))]
    [FriendOf(typeof(OperaComponent))]
    public static partial class OperaComponentSystem
    {
        [EntitySystem]
        private static void Awake(this OperaComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this OperaComponent self)
        {
            bool pressed = Input.IsMouseButtonPressed(MouseButton.Right) || Input.IsMouseButtonPressed(MouseButton.Left);
            if (!pressed)
            {
                self.mouseDown = false;
                return;
            }

            if (self.mouseDown)
            {
                return;
            }

            if (Input.IsMouseButtonPressed(MouseButton.Left)
                    && GlobalComponent.RootNode.GetViewport().GuiGetHoveredControl() != null)
            {
                return;
            }

            self.mouseDown = true;
            self.RaycastMove().Coroutine();
        }

        private static ETTask RaycastMove(this OperaComponent self)
        {
            return self.RaycastMoveAt(GlobalComponent.RootNode.GetViewport().GetMousePosition());
        }

        public static void RaycastAtScreenCenter(this OperaComponent self)
        {
            Vector2 screen = GlobalComponent.RootNode.GetViewport().GetVisibleRect().Size;
            self.RaycastMoveAt(new Vector2(screen.X * 0.5f, screen.Y * 0.5f - 150f)).Coroutine();
        }

        public static async ETTask RaycastMoveAt(this OperaComponent self, Vector2 screenPosition)
        {
            Camera3D camera = GlobalComponent.RootNode.GetViewport().GetCamera3D();
            if (camera == null)
            {
                return;
            }

            Vector3 from = camera.ProjectRayOrigin(screenPosition);
            Vector3 to = from + camera.ProjectRayNormal(screenPosition) * 1000f;

            PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to);
            Godot.Collections.Dictionary result = camera.GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (result.Count == 0)
            {
                Log.Warning("[寻路] 点地射线没打到任何东西（地面没有碰撞体？见 MapGroundCollision）");
                return;
            }

            Vector3 position = (Vector3) result["position"];
            self.ClickPoint = position;

            Log.Info($"[寻路] 点地：世界坐标 ({position.X:F2}, {position.Y:F2}, {position.Z:F2}) → 发 C2M_PathfindingResult");

            C2M_PathfindingResult c2MPathfindingResult = C2M_PathfindingResult.Create();
            c2MPathfindingResult.Position = new float3(position.X, position.Y, position.Z);
            self.Root().GetComponent<ClientSenderComponent>().Send(c2MPathfindingResult);

            await ETTask.CompletedTask;
        }

        public static async ETTask MoveToWorld(this OperaComponent self, Vector3 position)
        {
            self.ClickPoint = position;
            Log.Info($"[寻路] 直接寻路：世界坐标 ({position.X:F2}, {position.Y:F2}, {position.Z:F2}) → 发 C2M_PathfindingResult");

            C2M_PathfindingResult c2MPathfindingResult = C2M_PathfindingResult.Create();
            c2MPathfindingResult.Position = new float3(position.X, position.Y, position.Z);
            self.Root().GetComponent<ClientSenderComponent>().Send(c2MPathfindingResult);

            await ETTask.CompletedTask;
        }
    }
}
