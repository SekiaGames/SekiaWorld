using Unity.Mathematics;

namespace ET
{
    [ComponentOf]
    public class AIComponent: Entity, IAwake<int>, IDestroy
    {
        public int AIConfigId;

        /// <summary>
        /// 出生点（巡逻的圆心）—— <c>Awake</c> 那一刻的 Unit 位置，之后不再变。
        ///
        /// <para>
        /// 服务端的巡逻是「在 <c>birthPos</c> 周围 <c>territory</c> 米内随机取点」，
        /// 这边把 birthPos 记下来，由 AI 每 tick 写进 BTEnv 的 <c>HomePos</c>。
        /// </para>
        /// </summary>
        public float3 BirthPos;

        public ETCancellationToken CancellationToken;

        public long Timer;

        public int Current;
    }
}
