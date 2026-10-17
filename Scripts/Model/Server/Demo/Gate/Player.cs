namespace ET.Server
{
    [ChildOf(typeof(PlayerComponent))]
    public sealed class Player : Entity, IAwake<string>
    {
        public string Account { get; set; }

        /// <summary>
        /// 这个玩家**本次进图用的是哪个存档位上的角色**（进图时由 C2G_EnterMap 带上来）。
        /// 掉线（心跳超时）要回写角色位置时，靠它知道该写库里的哪一条。
        /// </summary>
        public int RoleSequence { get; set; } = -1;
    }
}