using System;
using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    /// <summary>
    /// Godot 侧资源加载组件（替代 YooAsset）。加载结果缓存到本组件，随组件销毁释放。
    /// </summary>
    [EntitySystemOf(typeof(ResourcesLoaderComponent))]
    [FriendOf(typeof(ResourcesLoaderComponent))]
    public static partial class ResourcesLoaderComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ResourcesLoaderComponent self)
        {
        }

        [EntitySystem]
        private static void Awake(this ResourcesLoaderComponent self, string packageName)
        {
        }

        [EntitySystem]
        private static void Destroy(this ResourcesLoaderComponent self)
        {
            self.handlers.Clear();
            self.scenes.Clear();
        }

        public static async ETTask<T> LoadAssetAsync<T>(this ResourcesLoaderComponent self, string location) where T : Resource
        {
            EntityRef<ResourcesLoaderComponent> selfRef = self;
            using EntityRef<CoroutineLock> coroutineLock = await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());
            self = selfRef;
            if (self == null)
            {
                return null;
            }

            if (self.handlers.TryGetValue(location, out Resource cached))
            {
                return cached as T;
            }

            T asset = ResourceLoader.Load<T>(location);
            if (asset == null)
            {
                throw new Exception($"load asset fail: {location}");
            }

            self.handlers.Add(location, asset);
            return asset;
        }

        /// <summary>
        /// 加载并实例化场景，挂到 Global 节点下（地图、UI 等）。
        /// </summary>
        public static async ETTask<Node> LoadSceneAsync(this ResourcesLoaderComponent self, string location)
        {
            EntityRef<ResourcesLoaderComponent> selfRef = self;
            using EntityRef<CoroutineLock> coroutineLock = await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());
            self = selfRef;
            if (self == null)
            {
                return null;
            }

            if (self.scenes.TryGetValue(location, out Node cached))
            {
                return cached;
            }

            PackedScene packedScene = ResourceLoader.Load<PackedScene>(location);
            if (packedScene == null)
            {
                throw new Exception($"load scene fail: {location}");
            }

            Node node = packedScene.Instantiate();
            // 挂到 Init 场景的 Global 节点下。GlobalComponent 在 AOT 的 Loader 层，
            // 不能直接写 Init.Instance —— Init 在启动工程（本层之上），反过来引会成环。
            GlobalComponent.RootNode.AddChild(node);
            self.scenes.Add(location, node);
            return node;
        }

        public static void UnloadScene(this ResourcesLoaderComponent self, string location)
        {
            if (!self.scenes.Remove(location, out Node node))
            {
                return;
            }

            node.QueueFree();
        }
    }

    [ComponentOf]
    public class ResourcesLoaderComponent: Entity, IAwake, IAwake<string>, IDestroy
    {
        public Dictionary<string, Resource> handlers = new();
        public Dictionary<string, Node> scenes = new();
    }
}
