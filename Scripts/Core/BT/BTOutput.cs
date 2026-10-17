using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BTOutput: Attribute
    {
        public Type Type { get; }

        public BTOutput(Type type)
        {
            this.Type = type;
        }
    }
}
