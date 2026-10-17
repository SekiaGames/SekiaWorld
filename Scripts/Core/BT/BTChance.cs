namespace ET
{
    /// <summary>
    /// 概率门（装饰节点）：按 <see cref="Percent"/>（0~100）掷骰，**中了才跑子节点**并返回子节点的结果；
    /// 没中就返回 <see cref="FailCode"/>（外层 Sequence/Selector 据此中断本次判定）。
    ///
    /// <para>对应服务端巡逻里「按 <c>MoveFrequency</c>(%) 掷骰决定这次动不动」。</para>
    /// </summary>
    public class BTChance: BTDecorate
    {
        /// <summary>命中概率（0~100）。100 = 必中，0 = 必不中</summary>
        public int Percent = 100;

        /// <summary>没命中时返回的错误码</summary>
        public int FailCode = 1;
    }
}
