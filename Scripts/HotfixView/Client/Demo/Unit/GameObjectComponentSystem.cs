namespace ET.Client
{
    [EntitySystemOf(typeof(GameObjectComponent))]
    public static partial class GameObjectComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this GameObjectComponent self)
        {
            self.GameObject?.QueueFree();
        }

        [EntitySystem]
        private static void Awake(this GameObjectComponent self)
        {
        }
    }
}
