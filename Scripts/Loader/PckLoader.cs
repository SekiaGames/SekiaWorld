using System;
using System.Collections.Generic;
using Godot;

namespace ET
{
    /// <summary>
    /// 美术子工程产出的 `.pck` 挂载器。
    ///
    /// 美术资源不随主包走：它们由 `.Art/&lt;子工程&gt;/` 独立导出成 `.pck`，
    /// 落 `.Art/.pck/`，运行期在这里挂载进 `res://` 虚拟文件系统。
    ///
    /// 程序侧不登记、不约定包名 —— 扫目录拿到什么就挂什么。美术侧要新增一个包，
    /// 只管把 pck 丢进目标目录，这里一行都不用改。
    ///
    /// ⚠️ pck 不能放 `res://` 之内再挂自己 —— 导出后 `res://` 即主 pck 的内部，
    /// 那是「包中包」，必失败。所以挂载源一律走**磁盘物理路径**。
    /// </summary>
    public static class PckLoader
    {
        /// <summary>
        /// 美术 pck 的存放目录，两套解析见 <see cref="ResolveDir"/>。
        /// </summary>
        private const string EditorPckDir = ".Art/.pck";
        private const string ExportedPckDir = ".pck";

        private static readonly List<string> mounted = new List<string>();

        /// <summary>已挂载的包名（供排查用）。</summary>
        public static IReadOnlyList<string> Mounted => mounted;

        /// <summary>
        /// 挂载目标目录下的全部美术 pck。
        /// 目录不存在或里面没有 pck 只打 Warning ——
        /// 编辑器里没跑过导出、或该包与当前工程无关时，都不该拦住启动。
        /// </summary>
        public static void MountAll()
        {
            mounted.Clear();

            string dir = ResolveDir();
            string[] packFiles = FindPacks(dir);
            if (packFiles.Length == 0)
            {
                Log.Warning($"美术包目录 {dir} 里没有 .pck。" +
                        $"编辑器里请先跑 .Art/pack.sh；导出包请确认 pck 已随二进制一并分发。");
                return;
            }

            // 文件名排序，保证挂载顺序稳定（多个包覆盖同名资源时顺序才有意义）。
            Array.Sort(packFiles, StringComparer.Ordinal);

            foreach (string fileName in packFiles)
            {
                string path = $"{dir}/{fileName}";
                string name = fileName[..^".pck".Length];

                // 第二个参数 replace_files：true = 同名资源覆盖。
                // 美术包之间约定路径不重叠，这里保留默认行为便于将来打补丁。
                if (ProjectSettings.LoadResourcePack(path, true))
                {
                    mounted.Add(name);
                    Log.Info($"美术包已挂载: {fileName}");
                }
                else
                {
                    Log.Error($"美术包挂载失败: {path}");
                }
            }
        }

        /// <summary>
        /// 列出目录下的 `.pck` 文件名（不含路径）。目录不可读时返回空数组。
        ///
        /// ⚠️ `GetFiles()` 默认只给文件名、不给路径，正好是这里要的；
        /// 加了 `IncludeNavigational` 会把 `.` 和 `..` 也带进来，所以不加。
        /// </summary>
        private static string[] FindPacks(string dir)
        {
            using DirAccess dirAccess = DirAccess.Open(dir);
            if (dirAccess == null)
            {
                return Array.Empty<string>();
            }

            List<string> packs = new List<string>();
            foreach (string fileName in dirAccess.GetFiles())
            {
                if (fileName.EndsWith(".pck", StringComparison.OrdinalIgnoreCase))
                {
                    packs.Add(fileName);
                }
            }

            return packs.ToArray();
        }

        /// <summary>
        /// 解析美术包目录的物理路径。
        ///
        /// 编辑器里 cwd = 仓库根（Godot `--path` 会 set_cwd）⇒ `.Art/.pck` 直接用；
        /// 导出后 cwd 不可依赖，改为「可执行文件所在目录」旁找 `.pck/`。
        /// </summary>
        private static string ResolveDir()
        {
            if (OS.HasFeature("editor"))
            {
                return EditorPckDir;
            }

            return $"{OS.GetExecutablePath().GetBaseDir()}/{ExportedPckDir}";
        }
    }
}
