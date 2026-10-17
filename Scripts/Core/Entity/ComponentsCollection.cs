using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ET
{
    internal sealed class ComponentsCollectionDebugView
    {
        private readonly ComponentsCollection collection;

        public ComponentsCollectionDebugView(ComponentsCollection collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            this.collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Entity[] Items
        {
            get
            {
                Entity[] items = new Entity[this.collection.Count];
                this.collection.Values.CopyTo(items, 0);
                return items;
            }
        }
    }

    [DebuggerTypeProxy(typeof(ComponentsCollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public class ComponentsCollection: SortedDictionary<long, Entity>, IPool
    {
        public static ComponentsCollection Create(bool isFromPool = false)
        {
            return (ComponentsCollection)ObjectPool.Instance.Fetch(typeof (ComponentsCollection), isFromPool);
        }

        public bool IsFromPool { get; set; }

        void IPool.Clear()
        {
            this.Clear();
        }

        public void Dispose()
        {
            ObjectPool.Instance.Recycle(this);
        }
    }
}
