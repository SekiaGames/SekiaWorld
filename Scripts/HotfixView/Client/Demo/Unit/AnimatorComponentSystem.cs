namespace ET.Client
{
    [EntitySystemOf(typeof(AnimatorComponent))]
    [FriendOf(typeof(AnimatorComponent))]
    public static partial class AnimatorComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this AnimatorComponent self)
        {
            self.Parameter = null;
        }

        [EntitySystem]
        private static void Awake(this AnimatorComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this AnimatorComponent self)
        {
            if (self.isStop || self.MotionType == MotionType.None)
            {
                return;
            }

            self.MotionType = MotionType.None;
        }
    }
}
