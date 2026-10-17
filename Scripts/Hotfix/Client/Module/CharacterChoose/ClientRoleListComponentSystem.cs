using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(ClientRoleListComponent))]
    [FriendOf(typeof(ClientRoleListComponent))]
    public static partial class ClientRoleListComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientRoleListComponent self)
        {
        }

        public static void Set(this ClientRoleListComponent self, List<RoleInfo> roles)
        {
            self.roles.Clear();
            if (roles != null)
            {
                self.roles.AddRange(roles);
            }
            self.roles.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
        }

        public static RoleInfo Get(this ClientRoleListComponent self, int sequence)
        {
            foreach (RoleInfo role in self.roles)
            {
                if (role.Sequence == sequence)
                {
                    return role;
                }
            }
            return null;
        }

        public static int FirstEmptySequence(this ClientRoleListComponent self, int slotCount)
        {
            for (int i = 0; i < slotCount; ++i)
            {
                if (self.Get(i) == null)
                {
                    return i;
                }
            }
            return -1;
        }

        public static string Describe(this ClientRoleListComponent self, int slotCount, int lockedFrom)
        {
            System.Text.StringBuilder summary = new();
            for (int i = 0; i < slotCount; ++i)
            {
                RoleInfo role = self.Get(i);
                summary.Append($"{i + 1}:{(role != null ? role.Name : i + 1 < lockedFrom ? "空位" : "锁")} ");
            }
            return summary.ToString().TrimEnd();
        }
    }
}
