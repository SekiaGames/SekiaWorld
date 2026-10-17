using System.Collections.Generic;
using System.IO;
using Godot;

namespace ET.Client
{
    public static class MapWarp
    {
        public static List<WarpPoint> GetWarps(string sceneName)
        {
            List<WarpPoint> warps = new();
            MapPointsData data = MapPointsLoader.Load(MapPointsLoader.MapNameOfScene(sceneName));
            if (data == null)
            {
                return warps;
            }

            foreach (ExitPointInfo exit in data.ExitPoints)
            {
                if (!IsTargetMapUsable(exit.TargetMap))
                {
                    continue;
                }

                warps.Add(new WarpPoint
                {
                    TargetMapId = exit.TargetMap,
                    TargetBornPoint = exit.TargetBornPoint,
                    Position = new Vector3(exit.X, exit.Y, exit.Z),
                    Radius = exit.Radius,
                });
            }

            return warps;
        }

        private static bool IsTargetMapUsable(int mapId)
        {
            if (!MapConfigCategory.Instance.Contain(mapId))
            {
                return false;
            }

            string targetScene = "Scene" + MapConfigCategory.Instance.Get(mapId).NameEn;
            if (!ResourceLoader.Exists(MapSceneHelper.Resolve(targetScene)))
            {
                return false;
            }

            return !OS.HasFeature("editor")
                    || File.Exists(Path.Combine(".FrameWork/Config/RecastOutput", targetScene));
        }

        public const string WarpScenePath = "res://Resources/Public/Effect/Common/16TeleportPoint.tscn";

        public static int Setup(Node mapNode, string sceneName)
        {
            int count = 0;
            foreach (WarpPoint warp in GetWarps(sceneName))
            {
                Node3D visual = CreateVisual(warp);
                if (visual == null)
                {
                    continue;
                }

                mapNode.AddChild(visual);
                count++;
            }

            return count;
        }

        private static Node3D CreateVisual(WarpPoint warp)
        {
            PackedScene packed = ResourceLoader.Load<PackedScene>(WarpScenePath);
            if (packed == null)
            {
                Log.Error($"[传送阵] 载不到 {WarpScenePath} —— 文件不在？"
                        + "挪到美术工程后忘了加进 export_presets.cfg 的 export_files？");
                return null;
            }

            Node3D root = packed.Instantiate<Node3D>();
            root.Name = $"Warp_To{warp.TargetMapId}";
            root.Position = warp.Position;
            root.Scale = new Vector3(warp.Radius, 1f, warp.Radius);
            return root;
        }
    }
}
