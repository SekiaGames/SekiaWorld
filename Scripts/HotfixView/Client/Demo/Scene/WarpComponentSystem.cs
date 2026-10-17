using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(WarpComponent))]
    [FriendOf(typeof(WarpComponent))]
    public static partial class WarpComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WarpComponent self)
        {
            self.Warps = MapWarp.GetWarps(self.Scene().Name);

            self.Armed = false;
            self.Firing = false;

            Log.Info($"[传送阵] {self.Scene().Name}：{self.Warps.Count} 个（走进半径内即传送）");
        }

        [EntitySystem]
        private static void Update(this WarpComponent self)
        {
            if (self.Firing || self.Warps.Count == 0)
            {
                return;
            }

            Unit my = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (my == null)
            {
                return;
            }

            float3 pos = my.Position;
            foreach (WarpPoint warp in self.Warps)
            {
                float dx = pos.x - warp.Position.X;
                float dz = pos.z - warp.Position.Z;
                if (dx * dx + dz * dz > warp.Radius * warp.Radius)
                {
                    self.Armed = true;
                    continue;
                }

                if (!self.Armed)
                {
                    continue;
                }

                self.Firing = true;
                Log.Info($"[传送阵] 走进传送阵：{self.Scene().Name} → 地图 {warp.TargetMapId}"
                        + $"（目标出生点 {warp.TargetBornPoint}，阵心 {warp.Position.X:F2}, {warp.Position.Y:F2}, "
                        + $"{warp.Position.Z:F2} 半径 {warp.Radius:F1}）");
                ChangeMapHelper.ChangeMapAsync(self.Root(), warp.TargetMapId, warp.TargetBornPoint).Coroutine();
                return;
            }
        }
    }
}
