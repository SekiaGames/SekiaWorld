using System;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Flags]
    public enum EntityStatus: byte
    {
        None = 0,
        IsFromPool = 1,
        IsRegister = 1 << 1,
        IsComponent = 1 << 2,
        IsCreated = 1 << 3,
        IsSerializeWithParent = 1 << 4,
    }

    [MemoryPackable(GenerateType.NoGenerate)]
    public abstract partial class Entity: DisposeObject, IPool
    {
        [MemoryPackIgnore]
        [BsonIgnore]
        public long InstanceId { get; protected set; }

        protected Entity()
        {
        }

        [BsonIgnore]
        private EntityStatus status = EntityStatus.None;

        [MemoryPackIgnore]
        [BsonIgnore]
        public bool IsFromPool
        {
            get => (this.status & EntityStatus.IsFromPool) == EntityStatus.IsFromPool;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsFromPool;
                }
                else
                {
                    this.status &= ~EntityStatus.IsFromPool;
                }
            }
        }

        [BsonIgnore]
        protected bool IsRegister
        {
            get => (this.status & EntityStatus.IsRegister) == EntityStatus.IsRegister;
            set
            {
                if (this.IsRegister == value)
                {
                    return;
                }

                if (value)
                {
                    this.status |= EntityStatus.IsRegister;
                }
                else
                {
                    this.status &= ~EntityStatus.IsRegister;
                }

                if (value)
                {
                    this.RegisterSystem();
                }
            }
        }

        protected virtual void RegisterSystem()
        {
            this.iScene.Fiber.EntitySystem.RegisterSystem(this);
        }

        [BsonIgnore]
        protected bool IsComponent
        {
            get => (this.status & EntityStatus.IsComponent) == EntityStatus.IsComponent;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsComponent;
                }
                else
                {
                    this.status &= ~EntityStatus.IsComponent;
                }
            }
        }

        [BsonIgnore]
        protected bool IsCreated
        {
            get => (this.status & EntityStatus.IsCreated) == EntityStatus.IsCreated;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsCreated;
                }
                else
                {
                    this.status &= ~EntityStatus.IsCreated;
                }
            }
        }

        [BsonIgnore]
        public bool IsSerializeWithParent
        {
            get => (this.status & EntityStatus.IsSerializeWithParent) == EntityStatus.IsSerializeWithParent;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsSerializeWithParent;
                }
                else
                {
                    this.status &= ~EntityStatus.IsSerializeWithParent;
                }
            }
        }

        [MemoryPackIgnore]
        [BsonIgnore]
        public bool IsDisposed => this.InstanceId == 0;
        
        [BsonIgnore]
        private Entity parent;

        // 可以改变parent，但是不能设置为null
        [MemoryPackIgnore]
        [BsonIgnore]
        public Entity Parent
        {
            get => this.parent;
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有domain,也就是说parent必须在数据树上面
                if (value.IScene == null)
                {
                    throw new Exception($"cant set parent because parent domain is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveFromChildren(this);
                }

                this.parent = value;
                this.IsComponent = false;
                this.parent.AddToChildren(this);

                if (this is IScene scene)
                {
                    scene.Fiber = this.parent.iScene.Fiber;
                    this.IScene = scene;
                }
                else
                {
                    this.IScene = this.parent.iScene;
                }
            }
        }

        // 该方法只能在AddComponent中调用，其他人不允许调用
        [BsonIgnore]
        private Entity ComponentParent
        {
            set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有domain,也就是说parent必须在数据树上面
                if (value.IScene == null)
                {
                    throw new Exception($"cant set parent because parent domain is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveFromComponents(this);
                }

                this.parent = value;
                this.IsComponent = true;
                this.parent.AddToComponents(this);
                
                if (this is IScene scene)
                {
                    scene.Fiber = this.parent.iScene.Fiber;
                    this.IScene = scene;
                }
                else
                {
                    this.IScene = this.parent.iScene;
                }
            }
        }

        public T GetParent<T>() where T : Entity
        {
            return this.Parent as T;
        }

        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        [BsonElement]
        [BsonId]
        public long Id { get; protected set; }

        [BsonIgnore]
        protected IScene iScene;

        [MemoryPackIgnore]
        [BsonIgnore]
        public IScene IScene
        {
            get
            {
                return this.iScene;
            }
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"domain cant set null: {this.GetType().FullName}");
                }

                if (this.iScene == value)
                {
                    return;
                }

                IScene preScene = this.iScene;
                this.iScene = value;

                if (preScene == null)
                {
                    if (this.InstanceId == 0)
                    {
                        this.InstanceId = IdGenerater.Instance.GenerateInstanceId();
                    }

                    this.IsRegister = true;

                    if (this is ISerializeToEntity)
                    {
                        this.IsSerializeWithParent = true;
                    }

                    if (this.components != null)
                    {
                        foreach ((long _, Entity component) in this.components)
                        {
                            component.IsComponent = true;
                            component.parent = this;
                        }
                    }

                    if (this.children != null)
                    {
                        foreach ((long _, Entity child) in this.children)
                        {
                            child.IsComponent = false;
                            child.parent = this;
                        }
                    }
                }

                // 递归设置孩子的Domain
                if (this.children != null)
                {
                    foreach (Entity entity in this.children.Values)
                    {
                        entity.IScene = this.iScene;
                    }
                }

                if (this.components != null)
                {
                    foreach (Entity component in this.components.Values)
                    {
                        component.IScene = this.iScene;
                    }
                }

                if (!this.IsCreated)
                {
                    this.IsCreated = true;
                    EntitySystemSingleton.Instance.Deserialize(this);
                }
            }
        }

        [MemoryPackInclude]
        [BsonElement("Children")]
        [BsonIgnoreIfNull]
        protected ChildrenCollection children;

        [MemoryPackIgnore]
        [BsonIgnore]
        public ChildrenCollection Children
        {
            get
            {
                return this.children ??= ChildrenCollection.Create(true);
            }
        }

        private void AddToChildren(Entity entity)
        {
            this.Children.Add(entity.Id, entity);
        }

        private void RemoveFromChildren(Entity entity)
        {
            if (this.children == null)
            {
                return;
            }

            this.children.Remove(entity.Id);

            if (this.children.Count == 0)
            {
                this.children.Dispose();
                this.children = null;
            }
        }

        [MemoryPackInclude]
        [BsonElement("C")]
        [BsonIgnoreIfNull]
        protected ComponentsCollection components;

        [MemoryPackIgnore]
        [BsonIgnore]
        public ComponentsCollection Components
        {
            get
            {
                return this.components ??= ComponentsCollection.Create(true);
            }
        }

        public int ComponentsCount()
        {
            if (this.components == null)
            {
                return 0;
            }
            return this.components.Count;
        }
        
        public int ChildrenCount()
        {
            if (this.children == null)
            {
                return 0;
            }
            return this.children.Count;
        }

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsRegister = false;
            this.InstanceId = 0;

            if (this.children != null)
            {
                foreach (Entity child in this.children.Values)
                {
                    child.Dispose();
                }

                this.children.Dispose();
                this.children = null;
            }

            if (this.components != null)
            {
                foreach (var kv in this.components)
                {
                    kv.Value.Dispose();
                }

                this.components.Dispose();
                this.components = null;
            }

            // 触发Destroy事件
            if (this is IDestroy)
            {
                EntitySystemSingleton.Instance.Destroy(this);
            }

            this.iScene = null;

            if (this.parent != null && !this.parent.IsDisposed)
            {
                if (this.IsComponent)
                {
                    this.parent.RemoveComponent(this);
                }
                else
                {
                    this.parent.RemoveFromChildren(this);
                }
            }

            this.parent = null;

            base.Dispose();
            
            // 把status字段其它的status标记都还原
            bool isFromPool = this.IsFromPool;
            this.status = EntityStatus.None;
            this.IsFromPool = isFromPool;
            
            ObjectPool.Instance.Recycle(this);
        }

        private void AddToComponents(Entity component)
        {
            this.Components.Add(this.GetLongHashCode(component.GetType()), component);
        }

        private void RemoveFromComponents(Entity component)
        {
            if (this.components == null)
            {
                return;
            }

            this.components.Remove(this.GetLongHashCode(component.GetType()));

            if (this.components.Count == 0)
            {
                this.components.Dispose();
                this.components = null;
            }
        }

        public K GetChild<K>(long id) where K : Entity
        {
            if (this.children == null)
            {
                return null;
            }

            this.children.TryGetValue(id, out Entity child);
            return child as K;
        }

        public void RemoveChild(long id)
        {
            if (this.children == null)
            {
                return;
            }

            if (!this.children.TryGetValue(id, out Entity child))
            {
                return;
            }

            this.children.Remove(id);
            child.Dispose();
        }

        public void RemoveComponent<K>() where K : Entity
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.components == null)
            {
                return;
            }

            Type type = typeof (K);
            
            Entity c;
            if (!this.components.TryGetValue(this.GetLongHashCode(type), out c))
            {
                return;
            }

            this.RemoveFromComponents(c);
            c.Dispose();
        }

        private void RemoveComponent(Entity component)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.components == null)
            {
                return;
            }

            Entity c;
            if (!this.components.TryGetValue(this.GetLongHashCode(component.GetType()), out c))
            {
                return;
            }

            if (c.InstanceId != component.InstanceId)
            {
                return;
            }

            this.RemoveFromComponents(c);
            c.Dispose();
        }

        public void RemoveComponent(Type type)
        {
            if (this.IsDisposed)
            {
                return;
            }

            Entity c;
            if (!this.components.TryGetValue(this.GetLongHashCode(type), out c))
            {
                return;
            }

            RemoveFromComponents(c);
            c.Dispose();
        }

        public K GetComponent<K>() where K : Entity
        {
            if (this.components == null)
            {
                return null;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            if (this is IGetComponentSys)
            {
                EntitySystemSingleton.Instance.GetComponentSys(this, typeof(K));
            }
            
            Entity component;
            if (!this.components.TryGetValue(this.GetLongHashCode(typeof (K)), out component))
            {
                return default;
            }

            return (K) component;
        }

        public Entity GetComponent(Type type)
        {
            if (this.components == null)
            {
                return null;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            // 这个要在tryget之前调用，因为有可能components没有，但是执行GetComponentSystem后又有了
            if (this is IGetComponentSys)
            {
                EntitySystemSingleton.Instance.GetComponentSys(this, type);
            }
            
            Entity component;
            if (!this.components.TryGetValue(this.GetLongHashCode(type), out component))
            {
                return null;
            }

            return component;
        }

        private static Entity Create(Type type, bool isFromPool)
        {
            Entity component;
            if (isFromPool)
            {
                component = (Entity) ObjectPool.Instance.Fetch(type);
            }
            else
            {
                component = Activator.CreateInstance(type) as Entity;
            }

            component.IsFromPool = isFromPool;
            component.IsCreated = true;
            component.Id = 0;
            return component;
        }

        public Entity AddComponent(Entity component)
        {
            Type type = component.GetType();
            if (this.components != null && this.components.ContainsKey(this.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            component.ComponentParent = this;

            return component;
        }

        public Entity AddComponent(Type type, bool isFromPool = false)
        {
            if (this.components != null && this.components.ContainsKey(this.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = this.Id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component);

            return component;
        }

        public K AddComponentWithId<K>(long id, bool isFromPool = false) where K : Entity, IAwake, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component);

            return component as K;
        }

        public K AddComponentWithId<K, P1>(long id, P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component, p1);

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2>(long id, P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component, p1, p2);

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(this.GetLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component, p1, p2, p3);

            return component as K;
        }

        public K AddComponent<K>(bool isFromPool = false) where K : Entity, IAwake, new()
        {
            return this.AddComponentWithId<K>(this.Id, isFromPool);
        }

        public K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            return this.AddComponentWithId<K, P1>(this.Id, p1, isFromPool);
        }

        public K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            return this.AddComponentWithId<K, P1, P2>(this.Id, p1, p2, isFromPool);
        }

        public K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return this.AddComponentWithId<K, P1, P2, P3>(this.Id, p1, p2, p3, isFromPool);
        }

        public Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
        }

        public T AddChild<T>(bool isFromPool = false) where T : Entity, IAwake
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        public T AddChild<T, A>(A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        public T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        public T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity, IAwake
        {
            Type type = typeof (T);
            T component = Entity.Create(type, isFromPool) as T;
            component.Id = id;
            component.Parent = this;
            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        public T AddChildWithId<T, A>(long id, A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        public T AddChildWithId<T, A, B>(long id, A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        public long GetLongHashCode()
        {
            return this.GetLongHashCode(this.GetType());
        }

        protected virtual long GetLongHashCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }

        public override void BeginInit()
        {
            if (this.iScene == null)
            {
                return;
            }

            EntitySystemSingleton.Instance.Serialize(this);

            if (this.components is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.components)
                {
                    if (entity.IsSerializeWithParent)
                    {
                        entity.BeginInit();
                    }
                }
            }

            if (this.children is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.children)
                {
                    if (entity.IsSerializeWithParent)
                    {
                        entity.BeginInit();
                    }
                }
            }
        }
    }
}