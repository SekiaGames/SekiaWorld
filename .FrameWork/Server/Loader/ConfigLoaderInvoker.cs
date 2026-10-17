using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    // 服务端只读 s 变体（服务端专用配置）。
    // 路径基准是「进程的当前目录」：服务端从 Bin/ 启动（产物落 Bin/，见 Launch02_RunServer.bat 的 pushd），
    // 所以 "../Config/..." = 从 Bin/ 往上一层到 .FrameWork/，再进 Config/。
    // ⚠️ 客户端那份（Scripts/Loader/ConfigLoaderInvoker.cs）基准不同：它跑在 Godot 进程里，
    //    工作目录是工程根（= 仓库根），写的是 ".FrameWork/Config/..."。
    // 2026-09-29 目录重构：../Config/Excel/s/ → ../Config/Excel_BytesExport/s/
    //   （源表搬进了 Config/Excel/，导出的二进制让位到 Excel_BytesExport/）。
    [Invoke]
    public class GetAllConfigBytes: AInvokeHandler<ConfigLoader.GetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
    {
        public override async ETTask<Dictionary<Type, byte[]>> Handle(ConfigLoader.GetAllConfigBytes args)
        {
            Dictionary<Type, byte[]> output = new Dictionary<Type, byte[]>();
            List<string> startConfigs = new List<string>()
            {
                "StartMachineConfigCategory", 
                "StartProcessConfigCategory", 
                "StartSceneConfigCategory", 
                "StartZoneConfigCategory",
            };
            HashSet<Type> configTypes = CodeTypes.Instance.GetTypes(typeof (ConfigAttribute));
            foreach (Type configType in configTypes)
            {
                string configFilePath;
                if (startConfigs.Contains(configType.Name))
                {
                    configFilePath = $"../Config/Excel_BytesExport/s/{Options.Instance.StartConfig}/{configType.Name}.bytes";    
                }
                else
                {
                    configFilePath = $"../Config/Excel_BytesExport/s/{configType.Name}.bytes";
                }
                output[configType] = File.ReadAllBytes(configFilePath);
            }

            await ETTask.CompletedTask;
            return output;
        }
    }
    
    // 返回类型必须与调用方 ConfigLoader.Reload 的
    //   EventSystem.Invoke<GetOneConfigBytes, ETTask<byte[]>>
    // 完全一致。EventSystem.Invoke<A, R> 是按 (argsType, returnType) 精确配对查表的，
    // 少一层 ETTask 包装就会永远找不到 handler，只在跑到「重载配置」这条冷路径时抛
    // "Invoke error6, not AInvokeHandler"。
    // 客户端侧（Scripts/Loader/ConfigLoaderInvoker.cs）本来就是 ETTask<byte[]>，这里与之对齐。
    [Invoke]
    public class GetOneConfigBytes: AInvokeHandler<ConfigLoader.GetOneConfigBytes, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(ConfigLoader.GetOneConfigBytes args)
        {
            byte[] configBytes = File.ReadAllBytes($"../Config/Excel_BytesExport/s/{args.ConfigName}.bytes");
            await ETTask.CompletedTask;
            return configBytes;
        }
    }
}