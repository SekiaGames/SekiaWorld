namespace ET
{
    /// <summary>
    /// 条件：单位**当前没有在移动**（<c>MoveComponent</c> 的路径已走完）。
    ///
    /// <para>
    /// 对应服务端巡逻判定前置的「没有当前路径」。放在巡逻树最前面：
    /// 还没走到上一个目标点就整棵树失败，不会中途改目标。
    /// </para>
    /// </summary>
    public class BTMoveArrived: BTCondition
    {
    }
}
