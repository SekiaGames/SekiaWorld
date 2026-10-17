using Godot;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [FriendOf(typeof(GlobalComponent))]
    [FriendOf(typeof(CreatedRoleComponent))]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;

            GlobalComponent globalComponent = scene.Root().GetComponent<GlobalComponent>();

            PlayerComponent player = scene.Root().GetComponent<PlayerComponent>();
            CreatedRoleComponent created = scene.Root().GetComponent<CreatedRoleComponent>();
            if (player != null && created?.Parts != null && unit.Id == player.MyId)
            {
                RoleComponent role = created.AddChild<RoleComponent, RoleParts>(created.Parts);

                float yaw = Mathf.RadToDeg(Mathf.Atan2(unit.Forward.x, unit.Forward.z));
                role.AttachTo(globalComponent.Unit,
                        new Vector3(unit.Position.x, unit.Position.y, unit.Position.z),
                        new Vector3(0f, yaw, 0f));
                role.PlayAction("wait");

                unit.AddComponent<GameObjectComponent>().GameObject = role.Root;
                unit.AddComponent<AnimatorComponent>().SetRole(role);

                Log.Info($"[进图] 玩家角色已装配：名字「{created.RoleName}」[{created.Parts}] "
                        + $"位置=({unit.Position.x:F2}, {unit.Position.y:F2}, {unit.Position.z:F2}) 朝向={yaw:F1}°");
                return;
            }

            RoleParts npcParts = unit.Type() switch
            {
                UnitType.NPC => RoleParts.FromNpc(RoleConfigHelper.Npc(unit.ConfigId)),
                UnitType.Monster => RoleParts.FromMonster(RoleConfigHelper.Monster(unit.ConfigId)),
                _ => null,
            };

            if (npcParts == null || npcParts.Body <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            RoleComponent npcRole = scene.AddChild<RoleComponent, RoleParts>(npcParts);

            float npcYaw = Mathf.RadToDeg(Mathf.Atan2(unit.Forward.x, unit.Forward.z));
            npcRole.AttachTo(globalComponent.Unit,
                    new Vector3(unit.Position.x, unit.Position.y, unit.Position.z),
                    new Vector3(0f, npcYaw, 0f));

            npcRole.PlayAction("wait");

            unit.AddComponent<GameObjectComponent>().GameObject = npcRole.Root;
            unit.AddComponent<AnimatorComponent>().SetRole(npcRole);

            // Log.Info($"[进图] {unit.Type()} 已装配：ConfigId={unit.ConfigId} [{npcParts}] "
            //         + $"pos=({unit.Position.x:F2}, {unit.Position.y:F2}, {unit.Position.z:F2}) yaw={npcYaw:F1}°");
        }
    }
}
