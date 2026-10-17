using System.Collections.Generic;
using System.Text;

namespace ET.Server
{
    [EntitySystemOf(typeof(Cell))]
    [FriendOf(typeof(Cell))]
    public static partial class CellSystem
    {
        [EntitySystem]
        private static void Awake(this ET.Server.Cell self)
        {

        }
        
        [EntitySystem]
        private static void Destroy(this Cell self)
        {
            self.AOIUnits.Clear();

            self.SubsEnterEntities.Clear();

            self.SubsLeaveEntities.Clear();
        }

        public static void Add(this Cell self, AOIEntity aoiEntity)
        {
            self.AOIUnits.Add(aoiEntity.Id, aoiEntity);
        }

        public static void Remove(this Cell self, AOIEntity aoiEntity)
        {
            self.AOIUnits.Remove(aoiEntity.Id);
        }

        /// <summary>把单元格键解成 "地图:格子X:格子Y"（只给日志/排查用，布局见 <see cref="AOIHelper.CreateCellId"/>）</summary>
        public static string CellIdToString(this long cellId)
        {
            (int mapId, int x, int y) = AOIHelper.DecodeCellId(cellId);
            return $"{mapId}:{x}:{y}";
        }

        public static string CellIdToString(this HashSet<long> cellIds)
        {
            StringBuilder sb = new StringBuilder();
            foreach (long cellId in cellIds)
            {
                sb.Append(cellId.CellIdToString());
                sb.Append(",");
            }

            return sb.ToString();
        }

    }
}