using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace ET
{
    [Invoke]
    public class GetAllConfigBytes: AInvokeHandler<ConfigLoader.GetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
    {
        public override async ETTask<Dictionary<Type, byte[]>> Handle(ConfigLoader.GetAllConfigBytes args)
        {
            Dictionary<Type, byte[]> output = new Dictionary<Type, byte[]>();
            HashSet<Type> configTypes = CodeTypes.Instance.GetTypes(typeof (ConfigAttribute));
            foreach (Type configType in configTypes)
            {
                output[configType] = ConfigPath.Read(configType.Name);
            }

            await ETTask.CompletedTask;
            return output;
        }
    }

    [Invoke]
    public class GetOneConfigBytes: AInvokeHandler<ConfigLoader.GetOneConfigBytes, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(ConfigLoader.GetOneConfigBytes args)
        {
            await ETTask.CompletedTask;
            return ConfigPath.Read(args.ConfigName);
        }
    }

    internal static class ConfigPath
    {
        private static readonly List<string> StartConfigs = new List<string>()
        {
            "StartMachineConfigCategory",
            "StartProcessConfigCategory",
            "StartSceneConfigCategory",
            "StartZoneConfigCategory",
        };

        // 路径基准是「进程的工作目录」：客户端跑在 Godot 进程里，而 Godot 用 --path 启动时会
        // set_cwd(工程根) ⇒ 工程根就是仓库根，客户端配置的邻居目录是 Bundles/、框架层的在 .FrameWork/。
        private const string ClientDir = "Bundles/Config";
        private const string ServerDir = ".FrameWork/Config/Excel_BytesExport/s";
        private const string ClientServerDir = ".FrameWork/Config/Excel_BytesExport/cs";

        private static string BytesDir => Init.codeMode switch
        {
            CodeMode.Client => ClientDir,
            CodeMode.Server => ServerDir,
            CodeMode.Client | CodeMode.Server => ClientServerDir,
            _ => null,
        };

        internal static byte[] Read(string configName)
        {
            bool isStartConfig = StartConfigs.Contains(configName);

            if (OS.HasFeature("editor"))
            {
                if (BytesDir == null)
                {
                    throw new Exception($"配置缺失: {configName}.bytes —— Init.codeMode = {Init.codeMode}，" +
                            "映射不到任何配置目录（None 表示没有任何身份声明者跑过；" +
                            "检查 Hotfix/Client 与 Hotfix/Server 下的 CodeModeEvent 订阅者）");
                }

                string bytesPath = isStartConfig
                        ? $"{BytesDir}/{Options.Instance.StartConfig}/{configName}.bytes"
                        : $"{BytesDir}/{configName}.bytes";
                if (!File.Exists(bytesPath))
                {
                    throw new Exception($"配置缺失: {bytesPath}（当前身份 {Init.codeMode}，先跑一次导表）");
                }

                return File.ReadAllBytes(bytesPath);
            }

            byte[] bytes = Godot.FileAccess.GetFileAsBytes($"res://Bundles/Config/{configName}.bytes");
            if (bytes == null || bytes.Length == 0)
            {
                throw new Exception($"配置缺失: res://Bundles/Config/{configName}.bytes（当前身份 {Init.codeMode}）");
            }

            return bytes;
        }
    }
}
