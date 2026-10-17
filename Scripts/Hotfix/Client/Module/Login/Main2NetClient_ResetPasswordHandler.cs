namespace ET.Client
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_ResetPasswordHandler: MessageHandler<Scene, Main2NetClient_ResetPassword, NetClient2Main_ResetPassword>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_ResetPassword request, NetClient2Main_ResetPassword response)
        {
            C2R_ResetPassword c2RResetPassword = C2R_ResetPassword.Create();
            c2RResetPassword.Email = request.Email;
            c2RResetPassword.Code = request.Code;
            c2RResetPassword.Password = request.Password;

            R2C_ResetPassword r2CResetPassword = await RealmRpcHelper.CallAsync<R2C_ResetPassword>(root, request.OwnerFiberId, c2RResetPassword);
            if (r2CResetPassword == null)
            {
                response.Error = ErrorCode.ERR_ConnectServerFail;
                response.Message = "连接服务器失败，请稍后再试";
                return;
            }

            using (r2CResetPassword)
            {
                response.Error = r2CResetPassword.Error;
                response.Message = r2CResetPassword.Message;
            }
        }
    }
}
