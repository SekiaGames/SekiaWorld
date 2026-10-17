using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ET.Server
{
    /// <summary>
    /// 账号读写：直接用 MongoDB 驱动（为什么不用框架的 DBComponent.Query/Save，见 <see cref="Account"/> 的注释）。
    /// 并发说明：这里没套框架 DBComponentSystem 那层 CoroutineLockType.DB 分片锁，因为用到的都是单文档原子操作 ——
    /// 注册判重靠 _id 主键冲突（不是"先查后插"，所以并发下也不会写出两条同邮箱），
    /// 更新最后登录是单条 UpdateOne。以后真要做多文档读写再考虑加锁，别提前上复杂度。
    /// </summary>
    // [FriendOf]：DBComponent.database 是实体字段，分析器(ET0002)要求只能在友元类里访问。
    // 想用框架自己的 DBComponent.Query/Save 也得走这条路（它们同样要能摸到 database）。
    [FriendOf(typeof(DBComponent))]
    public static class AccountRepository
    {
        private const string CollectionName = "Account";

        /// <summary>一个账号最多留几台设备的免密凭据（超出丢最旧那台）</summary>
        public const int MaxTokens = 5;

        /// <summary>
        /// 取当前场景所属 zone 的账号集合。
        /// DBManagerComponent 由这里"没挂就挂"懒加载（同一 zone 共用一个 DBComponent），
        /// 所以不依赖框架的 FiberInit_Realm 去挂 —— 那边的改动留在框架仓，本功能不动框架。
        /// 真正的连接由 GetZoneDB 按 zone 懒加载，没配 DBConnection 的区才会抛异常。
        /// </summary>
        public static IMongoCollection<Account> GetCollection(Scene scene)
        {
            DBManagerComponent dbManagerComponent = scene.GetComponent<DBManagerComponent>();
            if (dbManagerComponent == null)
            {
                dbManagerComponent = scene.AddComponent<DBManagerComponent>();
            }

            DBComponent dbComponent = dbManagerComponent.GetZoneDB(scene.Zone());
            return dbComponent.database.GetCollection<Account>(CollectionName);
        }

        /// <summary>按邮箱查账号，没有返回 null</summary>
        public static async ETTask<Account> FindByEmail(Scene scene, string email)
        {
            IMongoCollection<Account> collection = GetCollection(scene);
            return await collection.Find(Builders<Account>.Filter.Eq(a => a.Email, email)).FirstOrDefaultAsync();
        }

        /// <summary>新建账号；邮箱已被占用时返回 false</summary>
        public static async ETTask<bool> TryCreate(Scene scene, Account account)
        {
            IMongoCollection<Account> collection = GetCollection(scene);
            try
            {
                await collection.InsertOneAsync(account);
                return true;
            }
            catch (MongoWriteException e) when (e.WriteError != null && e.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        /// <summary>登录成功后更新最后登录时间</summary>
        public static async ETTask UpdateLastLogin(Scene scene, string email, long time)
        {
            IMongoCollection<Account> collection = GetCollection(scene);
            await collection.UpdateOneAsync(
                Builders<Account>.Filter.Eq(a => a.Email, email),
                Builders<Account>.Update.Set(a => a.LastLogin, time));
        }

        /// <summary>
        /// 重置密码：整组替换盐与哈希，并**吊销该账号的全部免密凭据**。
        /// 三件事写在同一条 UpdateOne 里 ⇒ 单文档原子更新，不存在"密码换了但凭据还在"的中间态 ——
        /// 否则改密码后旧凭据仍能免密登入，等于没改。
        /// 返回 false 表示邮箱没找到 —— 正常路径下重置前刚查过账号，只可能是校验通过后账号被删了。
        /// </summary>
        public static async ETTask<bool> ResetPassword(Scene scene, string email, string salt, string passwordHash)
        {
            IMongoCollection<Account> collection = GetCollection(scene);
            UpdateResult result = await collection.UpdateOneAsync(
                Builders<Account>.Filter.Eq(a => a.Email, email),
                Builders<Account>.Update.Set(a => a.Salt, salt).Set(a => a.PasswordHash, passwordHash).Unset(a => a.Tokens));
            return result.MatchedCount > 0;
        }

        /// <summary>
        /// 签发一条免密凭据，绑定到 <paramref name="deviceCode"/>。
        /// 一台设备只留一条 ⇒ 先 $pull 掉这台设备的旧条目、再 $push 新条目 + $slice 只留最近 <see cref="MaxTokens"/> 台。
        /// 两次 UpdateOne 不是原子的，但中间窗口极小，最坏结果只是同一设备多留一条旧凭据（下次登录自愈）。
        /// 凭据**永久有效**：作废靠换密码（<see cref="ResetPassword"/> 清空整张表）或把那条从数组里删掉。
        /// </summary>
        public static async ETTask<string> IssueToken(Scene scene, string email, string deviceCode)
        {
            string token = NewToken();

            IMongoCollection<Account> collection = GetCollection(scene);
            FilterDefinition<Account> byEmail = Builders<Account>.Filter.Eq(a => a.Email, email);

            // ⚠️ tokens 可能是**显式 null**（早期注册没初始化、或外部写进来的老文档）：
            // $pull / $push 对"缺字段"是 no-op，对显式 null 直接报 "Cannot apply $pull to a non-array value"。
            // 先把它归一成空数组，顺手把这类坏文档自愈掉（只在确实是 null 时才写）。
            await collection.UpdateOneAsync(
                byEmail & Builders<Account>.Filter.Type(a => a.Tokens, BsonType.Null),
                Builders<Account>.Update.Set(a => a.Tokens, new List<AccountToken>()));

            await collection.UpdateOneAsync(
                byEmail,
                Builders<Account>.Update.PullFilter(a => a.Tokens, Builders<AccountToken>.Filter.Eq(t => t.Device, deviceCode)));
            await collection.UpdateOneAsync(
                byEmail,
                Builders<Account>.Update.PushEach(
                    a => a.Tokens, new[] { new AccountToken { Device = deviceCode, Token = token } }, slice: -MaxTokens));

            return token;
        }

        /// <summary>32 字节随机 → Base64Url（去掉 + / = 三个在 URL 与 JSON 里要转义的字符）</summary>
        private static string NewToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        // ================================================================ 角色列表（只有 RealM 会调这里）

        /// <summary>取账号的角色列表；账号不存在 / 字段是 null 都返回空列表</summary>
        public static async ETTask<List<AccountRole>> GetRoles(Scene scene, string email)
        {
            Account account = await FindByEmail(scene, email);
            return account?.Roles ?? new List<AccountRole>();
        }

        /// <summary>
        /// 在指定存档位建角色。**该槽已被占 / 账号不存在**都返回 false。
        /// 一条 UpdateOne 搞定：过滤条件里带「这个槽还空着」⇒ 不是"先查后写"，并发下也不会写出同账号同槽两个角色。
        /// </summary>
        public static async ETTask<bool> CreateRole(Scene scene, string email, AccountRole role)
        {
            IMongoCollection<Account> collection = GetCollection(scene);
            FilterDefinition<Account> byEmail = Builders<Account>.Filter.Eq(a => a.Email, email);
            await EnsureRolesArray(collection, byEmail);

            UpdateResult result = await collection.UpdateOneAsync(
                byEmail & Builders<Account>.Filter.Not(Builders<Account>.Filter.ElemMatch(
                        a => a.Roles, Builders<AccountRole>.Filter.Eq(r => r.Sequence, role.Sequence))),
                Builders<Account>.Update.Push(a => a.Roles, role));
            return result.MatchedCount > 0;
        }

        /// <summary>删掉指定存档位上的角色；该槽本来没角色 / 账号不存在返回 false</summary>
        public static async ETTask<bool> DeleteRole(Scene scene, string email, int sequence)
        {
            IMongoCollection<Account> collection = GetCollection(scene);
            FilterDefinition<Account> byEmail = Builders<Account>.Filter.Eq(a => a.Email, email);
            await EnsureRolesArray(collection, byEmail);

            UpdateResult result = await collection.UpdateOneAsync(
                byEmail,
                Builders<Account>.Update.PullFilter(a => a.Roles,
                        Builders<AccountRole>.Filter.Eq(r => r.Sequence, sequence)));
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 玩家**掉线**（心跳超时）时回写某个角色停在哪（位置 + 朝向 + 在哪张图）。
        /// 返回 false 表示账号没匹配上（该槽有没有角色这里不细分：值没变化时 Mongo 也算 Matched）。
        /// </summary>
        public static async ETTask<bool> SaveRolePosition(Scene scene, string email, int sequence,
                float posX, float posY, float posZ, float rotY, int mapId)
        {
            IMongoCollection<Account> collection = GetCollection(scene);
            FilterDefinition<Account> byEmail = Builders<Account>.Filter.Eq(a => a.Email, email);
            await EnsureRolesArray(collection, byEmail);

            // 只改数组里那一个元素的几个字段：用 $[r] + arrayFilters 定位（按 seq 找，不依赖下标）
            // ⚠️ 位置与地图必须一起写：分开写会出现"图换了位置没换"（在 A 图的坐标上落到 B 图）的中间态
            UpdateResult result = await collection.UpdateOneAsync(
                byEmail,
                Builders<Account>.Update
                        .Set("roles.$[r].posX", posX)
                        .Set("roles.$[r].posY", posY)
                        .Set("roles.$[r].posZ", posZ)
                        .Set("roles.$[r].rotY", rotY)
                        .Set("roles.$[r].mapId", mapId),
                new UpdateOptions
                {
                    ArrayFilters = new List<ArrayFilterDefinition>
                    {
                        new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("r.seq", sequence)),
                    },
                });
            return result.MatchedCount > 0;
        }

        /// <summary>把可能缺失 / 显式 null 的 roles 字段归一成空数组（$push / $pull 对显式 null 会直接报错）</summary>
        private static async ETTask EnsureRolesArray(IMongoCollection<Account> collection, FilterDefinition<Account> byEmail)
        {
            await collection.UpdateOneAsync(
                byEmail & Builders<Account>.Filter.Type(a => a.Roles, BsonType.Null),
                Builders<Account>.Update.Set(a => a.Roles, new List<AccountRole>()));
        }
    }
}
