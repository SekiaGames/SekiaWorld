using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class AOIEntity: Entity, IAwake<int, float3>, IDestroy
    {
        public Unit Unit => this.GetParent<Unit>();

        public int ViewDistance;

        /// <summary>
        /// 这个 AOI 属于哪张图（= <c>Unit.MapId</c>，见 <see cref="Unit"/>）。
        /// <para>
        /// 一个 Gate 场景里可以同时存在多张图，而 AOI 的单元格键只由平面坐标算
        /// ⇒ 不带上地图，两张图坐标靠近的单位会在同一个格子里**互相看见**（收到对方的 <c>M2C_CreateUnits</c>）。
        /// 这个值在建 AOI 时从 Unit 抄过来，之后**不再变**（换图会重建整个 Unit）。
        /// </para>
        /// </summary>
        public int MapId { get; set; }

        private EntityRef<Cell> cell;

        public Cell Cell
        {
            get
            {
                return this.cell;
            }
            set
            {
                this.cell = value;
            }
        }

        // 观察进入视野的Cell
        public HashSet<long> SubEnterCells = new HashSet<long>();

        // 观察离开视野的Cell
        public HashSet<long> SubLeaveCells = new HashSet<long>();
        
        // 观察进入视野的Cell
        public HashSet<long> enterHashSet = new HashSet<long>();

        // 观察离开视野的Cell
        public HashSet<long> leaveHashSet = new HashSet<long>();

        // 我看的见的Unit
        public Dictionary<long, EntityRef<AOIEntity>> SeeUnits = new Dictionary<long, EntityRef<AOIEntity>>();
        
        // 看见我的Unit
        public Dictionary<long, EntityRef<AOIEntity>> BeSeeUnits = new Dictionary<long, EntityRef<AOIEntity>>();
        
        // 我看的见的Player
        public Dictionary<long, EntityRef<AOIEntity>> SeePlayers = new Dictionary<long, EntityRef<AOIEntity>>();

        // 看见我的Player单独放一个Dict，用于广播
        public Dictionary<long, EntityRef<AOIEntity>> BeSeePlayers = new Dictionary<long, EntityRef<AOIEntity>>();
    }
}