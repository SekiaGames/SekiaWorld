using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 一条免密凭据：**凭据与设备一对一** —— 一台设备最多留一条，凭据只认签发时那台设备。
    /// 只在**密码登录**成功时签发；用凭据登录不刷新、不续期。
    /// </summary>
    [EnableClass]
    public sealed class AccountToken
    {
        /// <summary>客户端自报的设备码（安装级 GUID）。⚠️ 不是硬件指纹 —— 防的是"凭据被拷到另一台机器"，不是防伪造</summary>
        [BsonElement("device")]
        public string Device { get; set; }

        /// <summary>随机凭据（32 字节熵的 Base64Url）。库里存明文：它是可吊销的凭据，不是密码</summary>
        [BsonElement("token")]
        public string Token { get; set; }
    }
}
