namespace ET.Server
{
    /// <summary>
    /// 怪物巡逻 AI —— 反复跑一棵 BTConfig 里的巡逻树。
    ///
    /// <para>
    /// 对应服务端 <c>NpcAI::normal()</c>：NORMAL 态下定时判定，满足「没有当前路径 + 可移动 + 按
    /// <c>MoveFrequency</c> 掷骰中了」就在出生点周围 <c>territory</c> 米内随机取点、寻路过去。
    /// 这边把「定时」做成 <see cref="TickInterval"/> 的 tick，「无路径 / 概率 / 取点 / 移动」全部交给树
    /// （巡逻树：<c>BTMoveArrived</c> → <c>BTChance</c> → <c>BTRandomPosInRange</c> + <c>BTMoveToPosAction</c>）。
    /// </para>
    /// <para>
    /// 树 id 取 <c>AIConfig.NodeParams[0]</c>；
    /// 出生点（巡逻圆心）由 <c>AIComponent.Awake</c> 记下，这里写进 BTEnv 的 <c>HomePos</c> 键。
    /// </para>
    /// </summary>
    [FriendOf(typeof(AIComponent))]
    public class AI_XunLuo: AAIHandler
    {
        /// <summary>tick 间隔（毫秒）—— 与服务端「每 3 秒判定一次」的节拍对齐</summary>
        private const int TickInterval = 3000;

        public override int Check(AIComponent aiComponent, AIConfig aiConfig)
        {
            int treeId = GetTreeId(aiConfig);
            return treeId > 0 && BTConfigHelper.HasTree(treeId)? 0 : 1;
        }

        public override async ETTask Execute(AIComponent aiComponent, AIConfig aiConfig, ETCancellationToken cancellationToken)
        {
            Unit unit = aiComponent.Parent as Unit;
            Scene scene = aiComponent.Root();
            if (unit == null || scene == null)
            {
                return;
            }

            int treeId = GetTreeId(aiConfig);
            BTNode tree = BTConfigHelper.Build(treeId);

            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> sceneRef = scene;
            using BTEnv env = BTEnv.Create(scene);
            env.AddEntity("Unit", unit);
            env.AddStruct("HomePos", aiComponent.BirthPos);

            Log.Info($"[巡逻] unit={unit.Id} 开始巡逻：树={treeId} 圆心={aiComponent.BirthPos}");

            while (true)
            {
                // ⚠️ 判定结果**不打日志**：一图上百只怪、每 3 秒一次，逐次打印会把 Godot
                //    远程调试器的输出打爆（`ERROR: [output overflow, print less text!]`，
                //    见 `network/limits/debugger/max_chars_per_second`）。要排查就临时在这儿加。
                BTHelper.RunTree(tree, env);

                unit = unitRef;
                if (unit == null)
                {
                    return;
                }

                scene = sceneRef;
                if (scene == null)
                {
                    return;
                }

                await scene.TimerComponent.WaitAsync(TickInterval);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }

        private static int GetTreeId(AIConfig aiConfig)
        {
            if (aiConfig.NodeParams != null && aiConfig.NodeParams.Length > 0)
            {
                return aiConfig.NodeParams[0];
            }

            return 0;
        }
    }
}
