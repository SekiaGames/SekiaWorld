using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 一条邮箱验证码记录（纯内存态）。
    /// 用属性而不是字段：Mongo 驱动的默认约定按属性映射，万一哪天被序列化也不会出意外。
    /// </summary>
    public struct EmailCodeRecord
    {
        /// <summary>验证码（6 位数字）</summary>
        public string Code { get; set; }

        /// <summary>过期时间（Unix 毫秒）</summary>
        public long ExpireTime { get; set; }

        /// <summary>下次允许发码的时间（Unix 毫秒，用于冷却）</summary>
        public long NextSendTime { get; set; }

        /// <summary>已校验失败次数</summary>
        public int TryCount { get; set; }
    }

    /// <summary>
    /// 邮箱验证码的内存态存储（挂在 Realm 根场景上，按邮箱索引）。
    /// 为什么不落库：验证码是分钟级的短时效中间态，进程重启即失效对开发更省事，也免得给库添垃圾。
    /// 代价与边界：① 重启/热重载后旧码失效，玩家重新发一次即可；
    /// ② 以后多 Realm 部署时要改成共享存储（Redis 或 Mongo），否则玩家可能连到没有码的那个 Realm。
    /// 当前 Localhost 只有一个 Realm，够用。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class EmailCodeComponent : Entity, IAwake
    {
        /// <summary>
        /// 纯运行态，绝不能落库（BsonIgnore 同时避免 MongoRegister 启动时给这个集合建序列化映射）
        /// </summary>
        [BsonIgnore]
        public readonly Dictionary<string, EmailCodeRecord> records = new();
    }
}
