using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class BTNodeTitleColorAttribute: Attribute
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public BTNodeTitleColorAttribute(float r, float g, float b, float a = 1f)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }
    }
}
