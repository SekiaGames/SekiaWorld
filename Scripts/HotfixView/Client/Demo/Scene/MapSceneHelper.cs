using Godot;

namespace ET.Client
{
    public static class MapSceneHelper
    {
        public static string Resolve(string sceneName)
        {
            string mapDir = sceneName.StartsWith("Scene") ? sceneName["Scene".Length..] : sceneName;
            string mapPath = $"res://Scene/{mapDir}/{sceneName}.tscn";
            if (!ResourceLoader.Exists(mapPath))
            {
                mapPath = $"res://Bundles/Scenes/{sceneName}.tscn";
            }

            return mapPath;
        }
    }
}
