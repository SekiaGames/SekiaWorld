using System.Net;
using System.Net.Sockets;

namespace ET.Client
{
    public static class RealmRpcHelper
    {
        public static async ETTask<TResponse> CallAsync<TResponse>(Scene root, int ownerFiberId, IRequest request) where TResponse : class, IResponse
        {
            IPEndPoint realmAddress = NetworkHelper.ToIPEndPoint(ConstValue.RealmAddress);

            NetComponent netComponent = root.GetComponent<NetComponent>();
            if (netComponent == null)
            {
                root.AddComponent<NetComponent, AddressFamily, NetworkProtocol>(realmAddress.AddressFamily, NetworkProtocol.TCP);
                netComponent = root.GetComponent<NetComponent>();
            }

            root.GetComponent<FiberParentComponent>().ParentFiberId = ownerFiberId;

            using Session session = netComponent.Create(realmAddress);
            return await session.Call(request) as TResponse;
        }
    }
}
