using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 账号文档（MongoDB 集合 <c>Account</c>）。
    /// 为什么不继承 Entity：Entity 的 Id 是 long 且带 [BsonId]，取不到"邮箱当主键"；
    /// 这里直接把 <see cref="Email"/> 作为 [BsonId]，好处是"一个邮箱只能有一条账号"由主键本身保证
    /// （插入重名会抛重复键，天然防并发重复注册），不用再额外建唯一索引。
    /// 代价是不能用框架的 DBComponent.Query/Save（它们约束 T : Entity 且按 long Id 查），
    /// 所以读写都在 <see cref="AccountRepository"/> 里直接用 MongoDB 驱动。
    /// </summary>
    [EnableClass]
    public sealed class Account
    {
        /// <summary>邮箱：登录名，同时是文档主键(_id)。写入前统一小写去空格，避免 A@x.com / a@x.com 变成两个账号。</summary>
        [BsonId]
        public string Email { get; set; }

        /// <summary>密码盐（Base64，每账号独立随机）</summary>
        [BsonElement("salt")]
        public string Salt { get; set; }

        /// <summary>密码哈希（PBKDF2-SHA256，Base64）。库里不存明文密码。</summary>
        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; }

        /// <summary>注册时间（Unix 毫秒）</summary>
        [BsonElement("createdAt")]
        public long CreatedAt { get; set; }

        /// <summary>最后登录时间（Unix 毫秒；0 表示还没登录过）</summary>
        [BsonElement("lastLogin")]
        public long LastLogin { get; set; }

        /// <summary>
        /// 免密凭据表，**一台设备一条**（最多 <see cref="AccountRepository.MaxTokens"/> 台，超出丢最旧那台）。
        /// **永久有效**，没有过期时间 —— 作废只有两条路：**换密码**（`AccountRepository.ResetPassword` 把整个字段 `$unset` 掉），
        /// 或把那条从数组里删掉。
        /// ⚠️ 库里可能是**缺字段**（老文档、换过密码被 `$unset`）或**显式 null**（注册时没初始化过）：
        /// 缺字段读出来是空列表（字段初始值兜底），显式 null 读出来是 null —— 两种都当"没有凭据"处理。
        /// 但**写库时不能留 null**：`$pull` / `$push` 对缺字段是 no-op，对显式 null 会直接报错。
        /// </summary>
        [BsonElement("tokens")]
        public List<AccountToken> Tokens { get; set; } = new();

        /// <summary>
        /// 角色列表（这个账号的存档位），由 RealM 读写 —— 客户端选角界面显示的就是它，Gate 只转发。
        /// ⚠️ 和 <see cref="Tokens"/> 一样：库里的老文档可能**缺字段**或**显式 null**，
        /// 而 <c>$push</c> / <c>$pull</c> 对显式 null 会报错 ⇒ 写之前先归一成空数组（见 <see cref="AccountRepository"/>）。
        /// </summary>
        [BsonElement("roles")]
        public List<AccountRole> Roles { get; set; } = new();
    }
}
