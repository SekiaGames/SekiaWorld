using System.Collections.Generic;

namespace ET.Client
{
    public struct AccountOpResult
    {
        public int Error;

        public string Message;

        public int Cooldown;

        public long PlayerId;

        public string Token;

        public List<RoleInfo> Roles;

        public bool IsSuccess => this.Error == ErrorCode.ERR_Success;

        public static AccountOpResult Success(long playerId, string token = null, List<RoleInfo> roles = null)
        {
            return new AccountOpResult { Error = ErrorCode.ERR_Success, Message = string.Empty, PlayerId = playerId, Token = token, Roles = roles };
        }

        public static AccountOpResult SuccessWithCooldown(int cooldown)
        {
            return new AccountOpResult { Error = ErrorCode.ERR_Success, Message = string.Empty, Cooldown = cooldown };
        }

        public static AccountOpResult Fail(int error, string message, int cooldown = 0)
        {
            return new AccountOpResult { Error = error, Message = message, Cooldown = cooldown };
        }
    }
}
