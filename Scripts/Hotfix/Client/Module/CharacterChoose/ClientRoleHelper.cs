using System.Collections.Generic;

namespace ET.Client
{
    public static class ClientRoleHelper
    {
        public const int SlotCount = 3;

        public static async ETTask<RoleOpResult> CreateRoleAsync(Scene root, int sequence, string name, int gender,
                int destClassId, RoleParts parts)
        {
            EntityRef<Scene> rootRef = root;

            C2G_CreateRole request = C2G_CreateRole.Create();
            request.Sequence = sequence;
            request.Name = name;
            request.Gender = gender;
            request.DestClassId = destClassId;
            request.Parts.AddRange(ToArray(parts));

            G2C_CreateRole response = await root.GetComponent<ClientSenderComponent>().Call(request) as G2C_CreateRole;
            root = rootRef;
            if (response == null)
            {
                return RoleOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "服务器没有响应");
            }

            using (response)
            {
                if (response.Error == ErrorCode.ERR_Success && root != null)
                {
                    Apply(root, response.Roles);
                    Log.Info($"[角色存档] 创角成功（RealM 已写库）：槽 {sequence + 1} 名字「{name}」"
                            + $"→ 账号现在 {response.Roles.Count} 个角色");
                }
                return new RoleOpResult { Error = response.Error, Message = response.Message };
            }
        }

        public static async ETTask<RoleOpResult> DeleteRoleAsync(Scene root, int sequence)
        {
            EntityRef<Scene> rootRef = root;

            C2G_DeleteRole request = C2G_DeleteRole.Create();
            request.Sequence = sequence;

            G2C_DeleteRole response = await root.GetComponent<ClientSenderComponent>().Call(request) as G2C_DeleteRole;
            root = rootRef;
            if (response == null)
            {
                return RoleOpResult.Fail(ErrorCode.ERR_ConnectServerFail, "服务器没有响应");
            }

            using (response)
            {
                if (response.Error == ErrorCode.ERR_Success && root != null)
                {
                    Apply(root, response.Roles);
                    Log.Info($"[角色存档] 删角成功（RealM 已写库）：槽 {sequence + 1} → 账号现在 {response.Roles.Count} 个角色");
                }
                return new RoleOpResult { Error = response.Error, Message = response.Message };
            }
        }

        private static void Apply(Scene root, List<RoleInfo> roles)
        {
            ClientRoleListComponent list = root.GetComponent<ClientRoleListComponent>();
            if (list == null)
            {
                return;
            }
            list.Set(new List<RoleInfo>(roles));
        }

        public static List<int> ToArray(RoleParts parts)
        {
            List<int> array = new();
            for (int i = 0; i < PartIndex.Count + 1 + 5; ++i)
            {
                array.Add(0);
            }

            if (parts == null)
            {
                return array;
            }

            for (int i = 1; i <= PartIndex.Count; ++i)
            {
                array[i] = parts[i];
            }
            array[PartIndex.Gender] = parts.Gender;
            array[PartIndex.HairColorIndex] = parts.HairColorIndex;
            array[PartIndex.EyeColorIndex] = parts.EyeColorIndex;
            array[PartIndex.BodyColorIndex] = parts.BodyColorIndex;
            return array;
        }

        public static RoleParts FromArray(List<int> array)
        {
            RoleParts parts = new();
            if (array == null)
            {
                return parts;
            }

            for (int i = 1; i <= PartIndex.Count && i < array.Count; ++i)
            {
                parts[i] = array[i];
            }
            parts.Gender = At(array, PartIndex.Gender);
            parts.HairColorIndex = At(array, PartIndex.HairColorIndex);
            parts.EyeColorIndex = At(array, PartIndex.EyeColorIndex);
            parts.BodyColorIndex = At(array, PartIndex.BodyColorIndex);
            return parts;
        }

        private static int At(List<int> array, int index)
        {
            return index >= 0 && index < array.Count ? array[index] : 0;
        }
    }
}
