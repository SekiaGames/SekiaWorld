namespace ET
{
    public static class CoroutineLockType
    {
        public const int None = 0;
        public const int Mailbox = 3;                   // Mailbox中队列
        public const int UnitId = 4;                    // 上线下线时使用
        public const int DB = 5;
        public const int Resources = 6;
        public const int ResourcesLoader = 7;

        public const int Max = 100; // 这个必须最大
    }
}