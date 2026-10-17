using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ET
{
    public class CodeLoader: Singleton<CodeLoader>, ISingletonAwake
    {
        private static readonly string[] HotfixAssemblyNames =
        {
            "SekiaWorld.Model",
            "SekiaWorld.ModelView",
            "SekiaWorld.Hotfix",
            "SekiaWorld.HotfixView",
        };

        private const string ModelAssemblyName = "SekiaWorld.Model";
        private const string HotfixDirectory = "res://Bundles/Code";

        private readonly List<Assembly> hotfixAssemblies = new List<Assembly>();

        public void Awake()
        {
        }

        public void Start()
        {
            this.LoadHotfixAssemblies();

            IStaticMethod start = new StaticMethod(this.GetHotfixAssembly(ModelAssemblyName), "ET.Entry", "Start");
            start.Run();
        }

        public void LoadHotfixAssemblies()
        {
            this.LoadHotfix();

            World.Instance.AddSingleton<CodeTypes, Assembly[]>(this.GetCodeAssemblies());
        }

        public void Reload()
        {
            this.LoadHotfix();

            CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(this.GetCodeAssemblies());
            codeTypes.CreateCode();

            Log.Info($"reload dll finish!");
        }

        private Assembly[] GetCodeAssemblies()
        {
            List<Assembly> assemblies =
            [
                typeof (CodeLoader).Assembly,   // SekiaWorld
                typeof (Entity).Assembly,       // Core
                .. this.hotfixAssemblies,      
            ];

            return assemblies.ToArray();
        }

        /// <summary>
        /// 热更程序集必须加载进 Godot的 AssemblyLoadContext
        /// 加载 4 个热更程序集。
        /// </summary>
        private void LoadHotfix()
        {
            this.hotfixAssemblies.Clear();

            AssemblyLoadContext loadContext =  AssemblyLoadContext.GetLoadContext(typeof (CodeLoader).Assembly);

            foreach (string name in HotfixAssemblyNames)
            {
                this.hotfixAssemblies.Add(LoadOne(loadContext, name));
            }
        }

        private static Assembly LoadOne(AssemblyLoadContext loadContext, string name)
        {
            byte[] dllBytes = ReadBundleBytes($"{name}.dll");
            if (dllBytes != null)
            {
                byte[] pdbBytes = ReadBundleBytes($"{name}.pdb");
                using MemoryStream dll = new MemoryStream(dllBytes);
                if (pdbBytes != null)
                {
                    using MemoryStream pdb = new MemoryStream(pdbBytes);
                    return loadContext.LoadFromStream(dll, pdb);
                }

                return loadContext.LoadFromStream(dll);
            }

            Log.Error($"热更程序集 {name} 不在 {HotfixDirectory}。" +
                    $"请确认跑过全量构建。");
            return null;
        }

        private static byte[] ReadBundleBytes(string fileName)
        {
            string path = $"{HotfixDirectory}/{fileName}.bytes";
            if (!Godot.FileAccess.FileExists(path))
            {
                return null;
            }

            try
            {
                return Godot.FileAccess.GetFileAsBytes(path);
            }
            catch (Exception e)
            {
                Log.Warning($"读取随包热更程序集失败，退回按名解析: {path} ({e.Message})");
                return null;
            }
        }

        private Assembly GetHotfixAssembly(string name)
        {
            foreach (Assembly assembly in this.hotfixAssemblies)
            {
                if (assembly.GetName().Name == name)
                {
                    return assembly;
                }
            }

            throw new Exception($"热更程序集未加载: {name}");
        }
    }
}
