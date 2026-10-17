using System;
using System.IO;
using Godot;

namespace ET
{
    /// <summary>
    /// <see cref="MapPointsReadBytes"/> 的实现：按 <c>Init.codeMode</c>（编辑器里）或包内路径（打包后）
    /// 读一份布点 <c>bytes</c>。与 <c>ConfigLoaderInvoker.ConfigPath</c>、<c>RecastFileReader</c> 同一套路 ——
    /// **碰文件系统的活放 Loader 层**。
    ///
    /// <para>
    /// ⚠️ 请求类型 <see cref="MapPointsReadBytes"/> 在 **Core**（不在 Model 的 <c>MapPointsLoader</c> 里）：
    /// Loader 层看不到 Model 程序集，只有 Core 是两边共同的引用。
    /// </para>
    ///
    /// <para>
    /// 目录/变体：编辑器里按 <c>Init.codeMode</c>（Client → <c>Bundles/MapPoints</c>、Server →
    /// <c>.FrameWork/Config/MapPoints_BytesExport/s</c>、两端都跑 → <c>.../cs</c>）；打包后一律
    /// <c>res://Bundles/MapPoints/</c>。
    /// </para>
    /// </summary>
    [Invoke]
    public class MapPointsBytesReader: AInvokeHandler<MapPointsReadBytes, byte[]>
    {
        private const string ClientDir = "Bundles/MapPoints";
        private const string ServerDir = ".FrameWork/Config/MapPoints_BytesExport/s";
        private const string ClientServerDir = ".FrameWork/Config/MapPoints_BytesExport/cs";

        /// <summary>与 <c>ConfigPath.BytesDir</c> 同一套映射</summary>
        private static string BytesDir => Init.codeMode switch
        {
            CodeMode.Client => ClientDir,
            CodeMode.Server => ServerDir,
            CodeMode.Client | CodeMode.Server => ClientServerDir,
            _ => null,
        };

        public override byte[] Handle(MapPointsReadBytes args)
        {
            string mapName = args.MapName;
            if (OS.HasFeature("editor"))
            {
                string dir = BytesDir;
                if (dir == null)
                {
                    Log.Error($"[布点] Init.codeMode = {Init.codeMode}，映射不到布点目录（谁都没声明身份？）");
                    return null;
                }

                string path = $"{dir}/{mapName}.bytes";
                if (!File.Exists(path))
                {
                    Log.Error($"[布点] 找不到 {path}（先跑一次 .Art/NavExport-SekiaArt_Map.command）");
                    return null;
                }

                return File.ReadAllBytes(path);
            }

            byte[] bytes = Godot.FileAccess.GetFileAsBytes($"res://{ClientDir}/{mapName}.bytes");
            if (bytes == null || bytes.Length == 0)
            {
                Log.Error($"[布点] 找不到 res://{ClientDir}/{mapName}.bytes（包里的布点数据漏了？）");
                return null;
            }

            return bytes;
        }
    }
}
