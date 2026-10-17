namespace ET.Server
{
    [FriendOf(typeof(GateSessionKeyComponent))]
    public static partial class GateSessionKeyComponentSystem
    {
        public static void Add(this GateSessionKeyComponent self, long key, string account)
        {
            self.sessionKey.Add(key, account);
            self.TimeoutRemoveKey(key).Coroutine();
        }

        public static string Get(this GateSessionKeyComponent self, long key)
        {
            string account = null;
            self.sessionKey.TryGetValue(key, out account);
            return account;
        }

        public static void Remove(this GateSessionKeyComponent self, long key)
        {
            self.sessionKey.Remove(key);
        }

        private static async ETTask TimeoutRemoveKey(this GateSessionKeyComponent self, long key)
        {
            EntityRef<GateSessionKeyComponent> selfRef = self;
            await self.Root().TimerComponent.WaitAsync(20000);
            self = selfRef;
            if (self == null)
            {
                return;
            }

            self.sessionKey.Remove(key);
        }
    }
}