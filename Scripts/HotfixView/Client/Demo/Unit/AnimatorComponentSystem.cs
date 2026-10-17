namespace ET.Client
{
    [EntitySystemOf(typeof(AnimatorComponent))]
    [FriendOf(typeof(AnimatorComponent))]
    public static partial class AnimatorComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this AnimatorComponent self)
        {
            self.Role = default;
        }

        [EntitySystem]
        private static void Awake(this AnimatorComponent self)
        {
            self.MotionType = MotionType.None;
        }

        [EntitySystem]
        private static void Update(this AnimatorComponent self)
        {
            MoveComponent moveComponent = self.GetParent<Unit>().GetComponent<MoveComponent>();
            MotionType motionType = moveComponent != null && !moveComponent.IsArrived()? MotionType.Move : MotionType.Idle;
            if (motionType == self.MotionType)
            {
                return;
            }

            self.MotionType = motionType;
            self.PlayMotion();
        }

        public static void SetRole(this AnimatorComponent self, RoleComponent role)
        {
            self.Role = role;
        }

        private static void PlayMotion(this AnimatorComponent self)
        {
            RoleComponent role = self.Role;
            if (role == null)
            {
                return;
            }

            string action = self.MotionType == MotionType.Move? self.MoveAction : self.IdleAction;
            if (role.CurrentAction == action)
            {
                return;
            }

            role.PlayAction(action);
        }
    }
}
