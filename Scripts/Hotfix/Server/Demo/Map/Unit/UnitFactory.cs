using System;
using Unity.Mathematics;

namespace ET.Server
{
    public static partial class UnitFactory
    {
        /// <summary>
        /// 玩家用的 <c>UnitConfig</c> id。ET demo 的占位配置（外观其实来自角色存档，
        /// 见客户端 <c>AfterUnitCreate_CreateUnitView</c>）—— 只用来让 <c>Unit.Type()</c> 认出「这是玩家」。
        /// </summary>
        public const int PlayerConfigId = 1001;

        /// <summary>
        /// 建一个 Unit。
        /// <paramref name="spawnPos"/> 是**出生位置** —— 玩家用「存档里那份位置」（RealM 的 Mongo，
        /// 见 <c>C2G_EnterMapHandler</c>），NPC / 怪物用布点数据里的点位。位置由调用方给，工厂不再自己写死。
        /// <paramref name="mapId"/> 是**在哪张图**（<c>MapConfig.Id</c>）：决定用哪份导航数据（寻路）、
        /// 以及回给客户端的场景名。
        ///
        /// <para>
        /// <paramref name="configId"/> 是**配表 id**：玩家与 ET demo 单位是 <c>UnitConfig</c> 的 id（1001…），
        /// 地图上的 NPC / 怪物是 <c>Table_Npc</c> / <c>Table_Monster</c> 的 id（如 10001 波利）。
        /// 「算哪种类型」由 <see cref="ET.UnitSystem.Type"/> 按它判定 ⇒ **调用方不再传 UnitType**
        /// （原来传的那个只用来选 1001/1002 两个 demo 配置，而 <c>UnitConfig</c> 里两行的 Type 都是 1，
        /// 怪物会被当成玩家）。
        /// </para>
        /// </summary>
        public static Unit Create(Scene scene, long id, int configId, float3 spawnPos, int mapId,
                float3 forward = default)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            Unit unit = unitComponent.AddChildWithId<Unit, int>(id, configId);
            unit.MapId = mapId;
            unit.AddComponent<MoveComponent>();
            unit.Position = spawnPos;

            // ⚠️ 朝向必须在**加入 AOI 之前**设好：AOI 一加入就可能给视野内的玩家发 M2C_CreateUnits，
            //    那一刻 UnitInfo.Forward 就是客户端定朝向用的值（晚设的话客户端拿到的是默认 +Z）。
            if (math.lengthsq(forward) > 0f)
            {
                unit.Forward = forward;
            }

            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            // 玩家跑得快一点（6 米每秒）；NPC / 怪物 3 米每秒
            numericComponent.Set(NumericType.Speed, unit.Type() == UnitType.Player? 6f : 3f);
            numericComponent.Set(NumericType.AOI, 15000); // 视野 15 米

            // Gate 兼任 Map：Unit 直接建在本 Gate 场景里，创建时就把原先由传送流程补上的组件补齐
            unit.AddComponent<PathfindingComponent, string>(GateMapConst.SceneNameOf(mapId));
            unit.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);

            unitComponent.Add(unit);
            // 加入 aoi
            unit.AddComponent<AOIEntity, int, float3>(9 * 1000, unit.Position);
            return unit;
        }
    }
}