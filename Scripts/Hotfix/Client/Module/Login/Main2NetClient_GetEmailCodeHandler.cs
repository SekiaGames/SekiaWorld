namespace ET.Client
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_GetEmailCodeHandler: MessageHandler<Scene, Main2NetClient_GetEmailCode, NetClient2Main_GetEmailCode>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_GetEmailCode request, NetClient2Main_GetEmailCode response)
        {
            C2R_GetEmailCode c2RGetEmailCode = C2R_GetEmailCode.Create();
            c2RGetEmailCode.Email = request.Email;
            c2RGetEmailCode.Type = request.Type;

            R2C_GetEmailCode r2CGetEmailCode = await RealmRpcHelper.CallAsync<R2C_GetEmailCode>(root, request.OwnerFiberId, c2RGetEmailCode);
            if (r2CGetEmailCode == null)
            {
                response.Error = ErrorCode.ERR_ConnectServerFail;
                response.Message = "连接服务器失败，请稍后再试";
                return;
            }

            using (r2CGetEmailCode)
            {
                response.Error = r2CGetEmailCode.Error;
                response.Message = r2CGetEmailCode.Message;
                response.Cooldown = r2CGetEmailCode.Cooldown;
            }
        }
    }
}
