namespace ET
{
    [EntitySystemOf(typeof(Unit))]
    public static partial class UnitSystem
    {
        [EntitySystem]
        private static void Awake(this Unit self, int configId)
        {
            self.ConfigId = configId;
        }

        public static UnitConfig Config(this Unit self)
        {
            return UnitConfigCategory.Instance.Get(self.ConfigId);
        }

        /// <summary>
        /// 单位类型。先查 ET 自带的 <c>UnitConfig</c>（玩家与 demo 单位），查不到再落到
        /// <c>Table_Npc</c> / <c>Table_Monster</c> —— 地图上的 NPC 与怪物用的是**它们自己的表 id**
        /// （如 10001 波利），<c>UnitConfig</c> 里没有这些行。
        ///
        /// <para>
        /// ⚠️ 所以**不能**直接 <c>Config()</c>（生成的 <c>Get</c> 找不到会抛异常）—— 必须先 <c>Contain</c> 判一遍。
        /// 这个类型会经 <c>UnitInfo.Type</c> 下发，客户端按它决定「装配成玩家 / NPC / 怪物」。
        /// </para>
        /// </summary>
        public static UnitType Type(this Unit self)
        {
            if (UnitConfigCategory.Instance.Contain(self.ConfigId))
            {
                return (UnitType)UnitConfigCategory.Instance.Get(self.ConfigId).Type;
            }

            if (NpcConfigCategory.Instance.Contain(self.ConfigId))
            {
                return UnitType.NPC;
            }

            if (MonsterConfigCategory.Instance.Contain(self.ConfigId))
            {
                return UnitType.Monster;
            }

            Log.Warning($"[单位] ConfigId={self.ConfigId} 在 UnitConfig / NpcConfig / MonsterConfig 里都查不到 ⇒ 当 NPC 处理");
            return UnitType.NPC;
        }
    }
}