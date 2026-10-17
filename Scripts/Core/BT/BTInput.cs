using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BTInput: Attribute
    {
        public Type Type { get; }

        public BTInput()
        {
            this.Type = typeof (void);
        }

        public BTInput(Type type)
        {
            this.Type = type;
        }
    }
}
