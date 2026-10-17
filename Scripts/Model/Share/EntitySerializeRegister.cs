using MemoryPack;

namespace ET
{
    public static partial class EntitySerializeRegister
    {
        static partial void Register();

        public static void Init()
        {
            MemoryPackFormatterProvider.Register(new MemoryPackChildrenCollectionFormatter());
            MemoryPackFormatterProvider.Register(new MemoryPackComponentsCollectionFormatter());
            Register();
        }
    }
}

