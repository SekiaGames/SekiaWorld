using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class BTIntDropdownAttribute: Attribute
    {
        public BTIntDropdownAttribute(Type enumSingletonType)
        {
            this.EnumSingletonType = enumSingletonType;
        }

        public Type EnumSingletonType { get; }
    }
}
