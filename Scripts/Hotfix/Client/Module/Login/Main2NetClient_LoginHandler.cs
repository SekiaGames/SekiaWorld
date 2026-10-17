using System.Net;
using System.Net.Sockets;

namespace ET.Client
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_LoginHandler: MessageHandler<Scene, Main2NetClient_Login, NetClient2Main_Login>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_Login request, NetClient2Main_Login response)
        {
            string email = request.Account;
            string password = request.Password;
            string token = request.Token;
            string deviceCode = request.DeviceCode;

            IPEndPoint realmAddress = NetworkHelper.ToIPEndPoint(ConstValue.RealmAddress);

            NetComponent netComponent = root.GetComponent<NetComponent>();
            if (netComponent == null)
            {
                root.AddComponent<NetComponent, AddressFamily, NetworkProtocol>(realmAddress.AddressFamily, NetworkProtocol.TCP);
                netComponent = root.GetComponent<NetComponent>();
            }

            root.GetComponent<FiberParentComponent>().ParentFiberId = request.OwnerFiberId;

            R2C_Login r2CLogin;
            using (Session session = netComponent.Create(realmAddress))
            {
                C2R_Login c2RLogin = C2R_Login.Create();
                c2RLogin.Email = email;
                c2RLogin.Password = password;
                c2RLogin.Token = token;
                c2RLogin.DeviceCode = deviceCode;
                r2CLogin = (R2C_Login)await session.Call(c2RLogin);
            }

            if (r2CLogin.Error != ErrorCode.ERR_Success)
            {
                response.Error = r2CLogin.Error;
                response.Message = r2CLogin.Message;
                return;
            }

            Session gateSession = netComponent.Create(NetworkHelper.ToIPEndPoint(r2CLogin.Address));
            gateSession.AddComponent<ClientSessionErrorComponent>();
            gateSession.AddComponent<PingComponent>();
            root.AddComponent<SessionComponent>().Session = gateSession;
            C2G_LoginGate c2GLoginGate = C2G_LoginGate.Create();
            c2GLoginGate.Key = r2CLogin.Key;
            c2GLoginGate.GateId = r2CLogin.GateId;
            G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await gateSession.Call(c2GLoginGate);

            Log.Debug("登陆gate成功!");

            response.PlayerId = g2CLoginGate.PlayerId;
            response.Token = r2CLogin.Token;
            response.Roles.AddRange(g2CLoginGate.Roles);
        }
    }
}
