using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 角色数据在两侧的转换：<see cref="AccountRole"/>（RealM 的 Mongo 文档）↔ <c>RoleInfo</c>（协议/客户端）。
    ///
    /// <para>
    /// 只做字段搬运，不放业务判断（业务在 RealM 侧的处理里）。
    /// 字段含义与 Parts 数组顺序见 <see cref="AccountRole"/> 与 proto 里的 <c>RoleInfo</c> 注释。
    /// </para>
    /// </summary>
    public static class RoleListHelper
    {
        /// <summary>库里的列表 → 协议列表（RealM 回给 Gate 用）</summary>
        public static List<RoleInfo> ToInfoList(List<AccountRole> roles)
        {
            List<RoleInfo> list = new();
            if (roles == null)
            {
                return list;
            }

            foreach (AccountRole role in roles)
            {
                list.Add(ToInfo(role));
            }
            return list;
        }

        public static RoleInfo ToInfo(AccountRole role)
        {
            RoleInfo info = RoleInfo.Create();
            info.Sequence = role.Sequence;
            info.Name = role.Name;
            info.Gender = role.Gender;
            info.DestClassId = role.DestClassId;
            // ⚠️ 老文档可能没有 parts 字段（读出来是 null），别直接 AddRange
            if (role.Parts != null)
            {
                info.Parts.AddRange(role.Parts);
            }
            info.PosX = role.PosX;
            info.PosY = role.PosY;
            info.PosZ = role.PosZ;
            info.RotY = role.RotY;
            // ⚠️ 老文档没有 mapId 字段（5.0 之前建的号）⇒ 读出来是 0，兜底成默认图，老号行为不变
            info.MapId = GateMapConst.Normalize(role.MapId);
            return info;
        }

        /// <summary>创角请求（Gate 转给 RealM 的）→ 要写进库的记录</summary>
        public static AccountRole FromCreateRequest(G2R_CreateRole request)
        {
            AccountRole role = new()
            {
                Sequence = request.Sequence,
                Name = request.Name,
                Gender = request.Gender,
                DestClassId = request.DestClassId,
                Parts = new List<int>(request.Parts),
                // 出生点（Gate 按当前地图填的），建出来就写进库
                PosX = request.PosX,
                PosY = request.PosY,
                PosZ = request.PosZ,
                // 建出来时在哪张图（Gate 填的默认图）
                MapId = GateMapConst.Normalize(request.MapId),
            };
            return role;
        }
    }
}
