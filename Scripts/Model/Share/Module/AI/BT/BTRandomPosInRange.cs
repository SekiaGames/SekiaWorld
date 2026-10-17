namespace ET
{
    /// <summary>
    /// 巡逻取点：以「出生点」为圆心、<see cref="Radius"/> 米为半径**随机**取一个点，
    /// 写进 env 的 <see cref="Key"/>（下游 <c>BTMoveToPosAction</c> 读它去寻路）。
    ///
    /// <para>
    /// 对应服务端 <c>NpcAI::normal()</c> 里的 <c>getRandPos(birthPos, territory, p)</c>。
    /// 出生点由 AI 在跑树前写进 env（键名 <see cref="HomeKey"/>），见 <c>AI_XunLuo</c>。
    /// </para>
    /// </summary>
    public class BTRandomPosInRange: BTAction
    {
        /// <summary>
        /// 随机半径（米）。
        ///
        /// <para>
        /// ⚠️ 服务端这里用的是**点位上的 <c>territory</c>**（每只怪不一样，0 = 不巡逻）；
        /// 布点数据目前还没导出该字段 ⇒ 先用固定值，等 <c>spawn</c> 段补上 territory 再改成读点位。
        /// </para>
        /// </summary>
        public float Radius = 10;

        /// <summary>出生点在 env 里的键名</summary>
        public string HomeKey = "HomePos";

        /// <summary>取到的点写进哪个 env 键（= <c>BTMoveToPosAction</c> 读的那个）</summary>
        public string Key = "TargetPos";
    }
}
