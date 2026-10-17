namespace ET.Client
{
    public struct RoleOpResult
    {
        public int Error;

        public string Message;

        public bool IsSuccess => this.Error == ErrorCode.ERR_Success;

        public static RoleOpResult Success()
        {
            return new RoleOpResult { Error = ErrorCode.ERR_Success, Message = string.Empty };
        }

        public static RoleOpResult Fail(int error, string message)
        {
            return new RoleOpResult { Error = error, Message = message };
        }
    }
}
