using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class ClientRoleListComponent: Entity, IAwake
    {
        public List<RoleInfo> roles = new();
    }
}
