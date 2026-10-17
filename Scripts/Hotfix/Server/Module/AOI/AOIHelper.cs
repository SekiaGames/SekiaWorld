using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(AOIEntity))]
    public static partial class AOIHelper
    {
        /// <summary>单元格键里地图占的位数（22 位 ⇒ 最多 4194303 张图，`MapConfig.Id` 里最大的 80000 富余得很）</summary>
        private const int MapIdBits = 22;

        /// <summary>单元格键里每个坐标轴占的位数（21 位有符号 ⇒ ±1048575 个格；格边长 10m ⇒ ±1 万公里）</summary>
        private const int CellBits = 21;

        private const long CellMask = (1L << CellBits) - 1;

        /// <summary>
        /// 单元格键：**地图 + 平面格坐标**。
        ///
        /// <para>
        /// 一个 Gate 场景里可以同时有多张图，而这张图原来是
        /// <c>((long)x &lt;&lt; 32) | y</c>（只有坐标）⇒ 两张图坐标靠近的单位会落进同一格、互相看见。
        /// 现在把 <paramref name="mapId"/> 塞进高位：不同地图的格子**天然不撞**，AOI 就被隔离了。
        /// </para>
        /// <para>
        /// ⚠️ 键只是**不透明标识**（没有任何业务逻辑反解出坐标来用），唯一反解的地方是
        /// <c>CellSystem.CellIdToString</c>（日志用）—— 它跟着本布局一起改，别在别处再拼/拆这个键。
        /// </para>
        /// </summary>
        public static long CreateCellId(int mapId, int cellX, int cellY)
        {
            return ((long) (mapId & ((1 << MapIdBits) - 1)) << (CellBits * 2))
                    | ((long) (cellX & CellMask) << CellBits)
                    | (long) (cellY & CellMask);
        }

        /// <summary>反解单元格键（只给日志/排查用）</summary>
        public static (int MapId, int CellX, int CellY) DecodeCellId(long cellId)
        {
            int mapId = (int) ((ulong) cellId >> (CellBits * 2));
            return (mapId, SignExtend(cellId >> CellBits, CellBits), SignExtend(cellId, CellBits));
        }

        private static int SignExtend(long value, int bits)
        {
            long v = value & ((1L << bits) - 1);
            return (int) (v << (64 - bits) >> (64 - bits));
        }

        public static void CalcEnterAndLeaveCell(AOIEntity aoiEntity, int cellX, int cellY, HashSet<long> enterCell, HashSet<long> leaveCell)
        {
            enterCell.Clear();
            leaveCell.Clear();
            int r = (aoiEntity.ViewDistance - 1) / AOIManagerComponent.CellSize + 1;
            int leaveR = r;
            if (aoiEntity.Unit.Type() == UnitType.Player)
            {
                leaveR += 1;
            }

            // 同图内的格子范围：地图维度由 CreateCellId 统一带上（换图 = 换 Unit = 换 AOI，这里不会跨图）
            int mapId = aoiEntity.MapId;
            for (int i = cellX - leaveR; i <= cellX + leaveR; ++i)
            {
                for (int j = cellY - leaveR; j <= cellY + leaveR; ++j)
                {
                    long cellId = CreateCellId(mapId, i, j);
                    leaveCell.Add(cellId);

                    if (i > cellX + r || i < cellX - r || j > cellY + r || j < cellY - r)
                    {
                        continue;
                    }

                    enterCell.Add(cellId);
                }
            }
        }
    }
}
