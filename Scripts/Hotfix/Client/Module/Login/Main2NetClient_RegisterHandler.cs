namespace ET.Client
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_RegisterHandler: MessageHandler<Scene, Main2NetClient_Register, NetClient2Main_Register>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_Register request, NetClient2Main_Register response)
        {
            C2R_Register c2RRegister = C2R_Register.Create();
            c2RRegister.Email = request.Email;
            c2RRegister.Code = request.Code;
            c2RRegister.Password = request.Password;

            R2C_Register r2CRegister = await RealmRpcHelper.CallAsync<R2C_Register>(root, request.OwnerFiberId, c2RRegister);
            if (r2CRegister == null)
            {
                response.Error = ErrorCode.ERR_ConnectServerFail;
                response.Message = "连接服务器失败，请稍后再试";
                return;
            }

            using (r2CRegister)
            {
                response.Error = r2CRegister.Error;
                response.Message = r2CRegister.Message;
            }
        }
    }
}
