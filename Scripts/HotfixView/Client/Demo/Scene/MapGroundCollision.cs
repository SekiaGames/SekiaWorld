using Godot;

namespace ET.Client
{
    public static class MapGroundCollision
    {
        private const string WalkableGroup = "Nav_Walkable";

        private const string WalkablePath = "static/ground";

        public static int Ensure(Node mapRoot)
        {
            if (mapRoot == null)
            {
                return 0;
            }

            return Collect(mapRoot, string.Empty, false);
        }

        private static int Collect(Node node, string relPath, bool insideWalkable)
        {
            string path = relPath.Length == 0? node.Name : $"{relPath}/{node.Name}";

            bool walkable = insideWalkable
                    || node.IsInGroup(WalkableGroup)
                    || path == WalkablePath
                    || path.EndsWith($"/{WalkablePath}");

            int added = 0;
            if (walkable && node is MeshInstance3D mesh && mesh.Mesh != null && !HasCollision(mesh))
            {
                mesh.CreateTrimeshCollision();
                added++;
            }

            foreach (Node child in node.GetChildren())
            {
                added += Collect(child, path, walkable);
            }

            return added;
        }

        private static bool HasCollision(MeshInstance3D mesh)
        {
            foreach (Node child in mesh.GetChildren())
            {
                if (child is StaticBody3D)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
