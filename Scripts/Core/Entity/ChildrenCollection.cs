using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ET
{
    internal sealed class ChildrenCollectionDebugView
    {
        private readonly ChildrenCollection collection;

        public ChildrenCollectionDebugView(ChildrenCollection collection)
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

    [DebuggerTypeProxy(typeof(ChildrenCollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public class ChildrenCollection: SortedDictionary<long, Entity>, IPool
    {
        public static ChildrenCollection Create(bool isFromPool = false)
        {
            return (ChildrenCollection)ObjectPool.Instance.Fetch(typeof (ChildrenCollection), isFromPool);
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
