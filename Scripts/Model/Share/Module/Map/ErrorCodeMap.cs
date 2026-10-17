namespace ET
{
    /// <summary>
    /// 地图 / 传送的错误码（200141-200149），与 <c>ErrorCode.cs</c> 同属一个 partial 类。
    /// 全部 &gt; 200000 ⇒ 属"不抛异常"语义：服务端把它们写进 response.Error/Message，
    /// 客户端直接读出来显示，不会变成 RpcException（≤200000 的码会被 Session.Call 抛出来）。
    /// <para>
    /// 放 Model/Share 而不是各端一份：本仓源码只有一份，客户端与服务端共用这一处定义。
    /// </para>
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>传送失败：目标地图在 MapConfig 里不存在</summary>
        public const int ERR_MapNotFound = 200141;

        /// <summary>传送失败：目标地图的服务端导航数据没烘（.FrameWork/Config/RecastOutput/{SceneName} 缺文件）</summary>
        public const int ERR_MapNavMissing = 200142;

        /// <summary>传送失败：玩家还没进图（本 Gate 场景里没有他的 Unit）</summary>
        public const int ERR_MapUnitNotFound = 200143;
    }
}
