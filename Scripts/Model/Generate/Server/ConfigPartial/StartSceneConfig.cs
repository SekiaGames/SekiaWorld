using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace ET
{
    public partial class StartSceneConfigCategory
    {
        public MultiMap<int, StartSceneConfig> Gates = new();
        
        public MultiMap<int, StartSceneConfig> ProcessScenes = new();
        
        public Dictionary<long, Dictionary<string, StartSceneConfig>> ClientScenesByName = new();

        public List<StartSceneConfig> Realms = new();

        // ET8.1 原本还有 LocationConfig / Routers / Maps / Match / Benchmark 五个字段，
        // 以及 EndInit() 里对应的 case SceneType.Location / Router / Map / Match / BenchmarkServer 分支。
        // 这些场景（Location / Router / Map / Match(LockStep) / Benchmark 压测）已全部删除，故一并移除。

        public List<StartSceneConfig> GetByProcess(int process)
        {
            return this.ProcessScenes[process];
        }
        
        public StartSceneConfig GetBySceneName(int zone, string name)
        {
            return this.ClientScenesByName[zone][name];
        }

        public override void EndInit()
        {
            foreach (StartSceneConfig startSceneConfig in this.GetAll().Values)
            {
                this.ProcessScenes.Add(startSceneConfig.Process, startSceneConfig);
                
                if (!this.ClientScenesByName.ContainsKey(startSceneConfig.Zone))
                {
                    this.ClientScenesByName.Add(startSceneConfig.Zone, new Dictionary<string, StartSceneConfig>());
                }
                this.ClientScenesByName[startSceneConfig.Zone].Add(startSceneConfig.Name, startSceneConfig);
                
                switch (startSceneConfig.Type)
                {
                    case SceneType.Realm:
                        this.Realms.Add(startSceneConfig);
                        break;
                    case SceneType.Gate:
                        this.Gates.Add(startSceneConfig.Zone, startSceneConfig);
                        break;
                }
            }
        }
    }
    
    public partial class StartSceneConfig: ISupportInitialize
    {
        public ActorId ActorId;
        
        public SceneType Type;

        public StartProcessConfig StartProcessConfig
        {
            get
            {
                return StartProcessConfigCategory.Instance.Get(this.Process);
            }
        }
        
        public StartZoneConfig StartZoneConfig
        {
            get
            {
                return StartZoneConfigCategory.Instance.Get(this.Zone);
            }
        }

        // 内网地址外网端口，通过防火墙映射端口过来
        private IPEndPoint innerIPPort;

        public IPEndPoint InnerIPPort
        {
            get
            {
                if (this.innerIPPort == null)
                {
                    this.innerIPPort = NetworkHelper.ToIPEndPoint($"{this.StartProcessConfig.InnerIP}:{this.Port}");
                }

                return this.innerIPPort;
            }
        }

        private IPEndPoint outerIPPort;

        // 外网地址外网端口
        public IPEndPoint OuterIPPort
        {
            get
            {
                if (this.outerIPPort == null)
                {
                    this.outerIPPort = NetworkHelper.ToIPEndPoint($"{this.StartProcessConfig.OuterIP}:{this.Port}");
                }

                return this.outerIPPort;
            }
        }

        public override void EndInit()
        {
            this.ActorId = new ActorId(this.Process, this.Id, 1);
            this.Type = EnumHelper.FromString<SceneType>(this.SceneType);
        }
    }
}