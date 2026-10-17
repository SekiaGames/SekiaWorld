using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 仅保留 TCP。原 ET8.1 还有 KCP / Websocket / UDP，已整体裁掉
    /// （对应的 KChannel/KService/IKcpTransport/WChannel/WService 与 ThirdParty/Kcp 均已删除）。
    /// </summary>
    public enum NetworkProtocol
    {
        TCP,
    }

    public class NetServices: Singleton<NetServices>, ISingletonAwake
    {
        private long idGenerator;
        
        public void Awake()
        {
        }

        // 这个因为是NetClientComponent中使用，不会与Accept冲突
        public uint CreateConnectChannelId()
        {
            return RandomGenerator.RandUInt32();
        }

        // 防止与内网进程号的ChannelId冲突，所以设置为一个大的随机数
        private int acceptIdGenerator = int.MinValue;

        public uint CreateAcceptChannelId()
        {
            return (uint)Interlocked.Add(ref this.acceptIdGenerator, 1);
        }
        
    }
}