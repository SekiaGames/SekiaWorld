using System;
using System.Security.Cryptography;
using System.Text;
using Godot;

namespace ET.Client
{
    public static class DeviceCodeHelper
    {
        public static string Get()
        {
            string uniqueId = OS.GetUniqueId();
            if (string.IsNullOrEmpty(uniqueId))
            {
                Log.Error("[登录] 取不到设备唯一标识，无法生成设备码");
                return null;
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uniqueId))).ToLowerInvariant();
        }
    }
}
