using System.Linq;

namespace ET.Server
{
    [FriendOf(typeof(PlayerComponent))]
    public static partial class PlayerComponentSystem
    {
        public static void Add(this PlayerComponent self, Player player)
        {
            self.dictionary.Add(player.Account, player);
        }
        
        public static void Remove(this PlayerComponent self, Player player)
        {
            self.dictionary.Remove(player.Account);
            player.Dispose();
        }
        
        public static Player GetByAccount(this PlayerComponent self,  string account)
        {
            self.dictionary.TryGetValue(account, out EntityRef<Player> player);
            return player;
        }

        /// <summary>
        /// 按 PlayerId 查 Player。Player 是 PlayerComponent 的子实体，其 Id 就是 PlayerId。
        /// Gate 兼任 Map 后 Unit.Id == Player.Id，M2C 消息靠它反查 Session。
        /// </summary>
        public static Player GetById(this PlayerComponent self, long id)
        {
            return self.GetChild<Player>(id);
        }
    }
}