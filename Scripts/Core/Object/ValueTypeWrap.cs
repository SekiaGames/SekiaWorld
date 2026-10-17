namespace ET
{
    public abstract class IValue<T>: DisposeObject where T: struct
    {
        public T Value
        {
            get;
            set;
        }

        public override void Dispose()
        {
            ObjectPool.Instance.Recycle(this);
        }
    }

    public class ValueTypeWrap<T>: IValue<T>, IPool where T: struct
    {
        public static ValueTypeWrap<T> Create(T value, bool isFromPool = true)
        {
            ValueTypeWrap<T> vw = (ValueTypeWrap<T>)ObjectPool.Instance.Fetch(typeof (ValueTypeWrap<T>), isFromPool);
            vw.Value = value;
            return vw;
        }

        public bool IsFromPool { get; set; }

        void IPool.Clear()
        {
            this.Value = default;
        }
    }
}
