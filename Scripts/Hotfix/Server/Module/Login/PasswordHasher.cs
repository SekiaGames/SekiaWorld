using System;
using System.Security.Cryptography;

namespace ET.Server
{
    /// <summary>
    /// 密码哈希：PBKDF2-SHA256 + 每账号独立随机盐，库里只存 salt 与 hash，不存明文。
    /// 校验用 FixedTimeEquals 等时比较，避免"逐字节提前返回"带来的时序侧信道。
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const int Iterations = 100_000;

        /// <summary>生成盐与哈希（都是 Base64，方便直接进 Mongo 的字符串字段）</summary>
        public static void Hash(string password, out string saltBase64, out string hashBase64)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
            saltBase64 = Convert.ToBase64String(salt);
            hashBase64 = Convert.ToBase64String(hash);
        }

        /// <summary>校验密码是否与库里的盐+哈希匹配</summary>
        public static bool Verify(string password, string saltBase64, string hashBase64)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(saltBase64) || string.IsNullOrEmpty(hashBase64))
            {
                return false;
            }

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(saltBase64);
                expected = Convert.FromBase64String(hashBase64);
            }
            catch (FormatException)
            {
                // 库里数据被手工改坏时不要抛异常给客户端，直接当校验失败
                return false;
            }

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
