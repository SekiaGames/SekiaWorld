namespace ET
{
    public static partial class ConstValue
    {
        public const int SessionTimeoutTime = 30 * 1000;

        /// <summary>
        /// 剥离软路由后，客户端直连的 Realm 地址（对应 StartConfig 里 Realm 的端口，Localhost 为 30002）。
        /// </summary>
        public const string RealmAddress = "127.0.0.1:30002";
    }
}
