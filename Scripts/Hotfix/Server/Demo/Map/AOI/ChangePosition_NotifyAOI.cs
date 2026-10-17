using Unity.Mathematics;

namespace ET.Server
{
    [Event(SceneType.Gate)]
    public class ChangePosition_NotifyAOI: AEvent<Scene, ChangePosition>
    {
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            float3 oldPos = args.OldPos;
            int oldCellX = (int) (oldPos.x * 1000) / AOIManagerComponent.CellSize;
            int oldCellY = (int) (oldPos.z * 1000) / AOIManagerComponent.CellSize;
            int newCellX = (int) (unit.Position.x * 1000) / AOIManagerComponent.CellSize;
            int newCellY = (int) (unit.Position.z * 1000) / AOIManagerComponent.CellSize;
            // 只比平面格：AOI 的地图维度来自 Unit.MapId，而它在一个 Unit（= 一个 AOI）的生命周期内不会变
            // （换图 = 清旧 Unit + 按目标图重建 ⇒ 换的是另一个 AOI，不会走到这里做"跨图挪格"）。
            // ⚠️ 将来若做"同 Unit 换图"（不重建），这里必须把地图一起比、并让 AOIEntity.MapId 跟着变。
            if (oldCellX == newCellX && oldCellY == newCellY)
            {
                return;
            }

            AOIEntity aoiEntity = unit.GetComponent<AOIEntity>();
            if (aoiEntity == null)
            {
                return;
            }

            unit.Scene().GetComponent<AOIManagerComponent>().Move(aoiEntity, newCellX, newCellY);
            await ETTask.CompletedTask;
        }
    }
}