
using System.Collections.Generic;

namespace ET.Server
{
    public static partial class MapMessageHelper
    {
        public static void NoticeUnitAdd(Unit unit, Unit sendUnit)
        {
            M2C_CreateUnits createUnits = M2C_CreateUnits.Create();
            createUnits.Units.Add(UnitHelper.CreateUnitInfo(sendUnit));
            MapMessageHelper.SendToClient(unit, createUnits);
        }

        /// <summary>
        /// 一次下发多个单位（比逐个 <c>NoticeUnitAdd</c> 少发很多包 —— 一张图上百个 NPC/怪物时差别很大）。
        /// 列表为空时**不发消息**。
        /// </summary>
        public static void NoticeUnitAddBatch(Unit unit, List<Unit> sendUnits)
        {
            if (sendUnits == null || sendUnits.Count == 0)
            {
                return;
            }

            M2C_CreateUnits createUnits = M2C_CreateUnits.Create();
            foreach (Unit u in sendUnits)
            {
                createUnits.Units.Add(UnitHelper.CreateUnitInfo(u));
            }

            SendToClient(unit, createUnits);
        }
        
        public static void NoticeUnitRemove(Unit unit, Unit sendUnit)
        {
            M2C_RemoveUnits removeUnits = M2C_RemoveUnits.Create();
            removeUnits.Units.Add(sendUnit.Id);
            MapMessageHelper.SendToClient(unit, removeUnits);
        }
        
        public static void Broadcast(Unit unit, IMessage message)
        {
            (message as MessageObject).IsFromPool = false;
            Dictionary<long, EntityRef<AOIEntity>> dict = unit.GetBeSeePlayers();
            // 网络底层做了优化，同一个消息不会多次序列化
            foreach (AOIEntity u in dict.Values)
            {
                SendToClient(u.Unit, message);
            }
        }
        
        /// <summary>
        /// 给客户端发消息。
        /// Gate 兼任 Map，Unit 与本 Gate 的 Player/Session 在同一个纤程里，
        /// 直接按 UnitId 找到 Player 的 Session 发送即可，不再需要 Location 转发。
        /// </summary>
        public static void SendToClient(Unit unit, IMessage message)
        {
            Scene root = unit.Root();
            Player player = root.GetComponent<PlayerComponent>().GetById(unit.Id);
            if (player == null)
            {
                return;
            }

            player.GetComponent<PlayerSessionComponent>()?.Session?.Send(message);
        }
        
        /// <summary>
        /// 发送协议给Actor
        /// </summary>
        public static void Send(Scene root, ActorId actorId, IMessage message)
        {
            root.GetComponent<MessageSender>().Send(actorId, message);
        }
    }
}
