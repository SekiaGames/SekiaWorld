using System;
using System.Collections.Generic;

namespace ET
{
    [Serializable]
    public abstract class BTNode: Object
    {
        public int Id;

        public List<BTNode> Children = new();

        public string Desc;
    }
}
