namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class CreatedRoleComponent: Entity, IAwake
    {
        public string RoleName { get; set; }

        public RoleParts Parts { get; set; }
    }
}
