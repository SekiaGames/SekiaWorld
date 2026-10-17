using MongoDB.Bson.Serialization.Attributes;
using Unity.Mathematics;

namespace ET
{
    [ChildOf(typeof(UnitComponent))]
    public partial class Unit: Entity, IAwake<int>
    {
        public int ConfigId { get; set; } //配置表id

        /// <summary>
        /// 这个 Unit 在**哪张图**（<c>MapConfig.Id</c>）。寻路（<see cref="ET.Server.PathfindingComponent"/>）与
        /// 下发客户端的场景名都按它来。玩家取自角色存档（<c>RoleInfo.MapId</c>），怪按其所属图。
        /// </summary>
        public int MapId { get; set; }

        [BsonElement]
        private float3 position; //坐标

        [BsonIgnore]
        public float3 Position
        {
            get => this.position;
            set
            {
                float3 oldPos = this.position;
                this.position = value;
                EventSystem.Instance.Publish(this.Scene(), new ChangePosition() { Unit = this, OldPos = oldPos });
            }
        }

        [BsonIgnore]
        public float3 Forward
        {
            get => math.mul(this.Rotation, math.forward());
            set => this.Rotation = quaternion.LookRotation(value, math.up());
        }
        
        // 默认朝世界 +Z
        [BsonElement]
        private quaternion rotation = quaternion.identity;
        
        [BsonIgnore]
        public quaternion Rotation
        {
            get => this.rotation;
            set
            {
                this.rotation = value;
                EventSystem.Instance.Publish(this.Scene(), new ChangeRotation() { Unit = this });
            }
        }
    }
}