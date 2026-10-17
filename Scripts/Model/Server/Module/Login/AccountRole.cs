using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 一个存档位上的角色，存在 <see cref="Account.Roles"/> 数组里（MongoDB 的 Account 文档）。
    ///
    /// <para>
    /// ⚠️ **只有 RealM 读写它**：Gate 只转发，客户端只显示。字段与协议里的 <c>RoleInfo</c> 一一对应，
    /// 转换见 <see cref="RoleListHelper"/>。
    /// </para>
    /// <para>
    /// Parts 与 Unity <c>Asset_Role</c> 的 parts 数组**同序**：
    /// [1]Body [2]Hair [3]LeftWeapon [4]RightWeapon [5]Head … [12]Gender [13]HairColorIndex [14]EyeColorIndex [16]BodyColorIndex。
    /// </para>
    /// </summary>
    [EnableClass]
    public sealed class AccountRole
    {
        /// <summary>存档位 0..2（对应选角界面 RoleCell1..3）。一个账号里唯一 —— 由写库时的过滤条件保证</summary>
        [BsonElement("seq")]
        public int Sequence { get; set; }

        /// <summary>角色名</summary>
        [BsonElement("name")]
        public string Name { get; set; }

        /// <summary>1 男 / 2 女</summary>
        [BsonElement("gender")]
        public int Gender { get; set; }

        /// <summary>创角时选的**目标职业** classID（真正建出来的角色是初心者，见创角流程）</summary>
        [BsonElement("destClassId")]
        public int DestClassId { get; set; }

        /// <summary>外观部件表（初心者 body/eye + 客户端选的头发/发色 + 初心者默认武器）</summary>
        [BsonElement("parts")]
        public List<int> Parts { get; set; } = new();

        /// <summary>玩家掉线（心跳超时）时由 Gate 回写：停在哪</summary>
        [BsonElement("posX")]
        public float PosX { get; set; }

        [BsonElement("posY")]
        public float PosY { get; set; }

        [BsonElement("posZ")]
        public float PosZ { get; set; }

        /// <summary>朝向：绕 Y 的欧拉角（度）</summary>
        [BsonElement("rotY")]
        public float RotY { get; set; }

        /// <summary>
        /// 这个角色**当前在哪张图**（MapConfig.Id，如 1=普隆德拉 / 2=普隆德拉南门 prt_1）。
        /// 与 <see cref="PosX"/> 等位置字段是一对，一起回写；进图时按它决定出生图。
        /// ⚠️ 老文档没有这个字段 ⇒ 读出来是 0，读端兜底成默认图（<c>GateMapConst.DefaultMapId</c>）。
        /// </summary>
        [BsonElement("mapId")]
        public int MapId { get; set; }
    }
}
