namespace ET.Client
{
    public static class RoleConfigHelper
    {
        public static CharacterSelectList MakeRole(int index)
        {
            return CharacterSelectListCategory.Instance.Contain(index)
                    ? CharacterSelectListCategory.Instance.Get(index)
                    : null;
        }

        public static ClassConfig Class(int classId)
        {
            return ClassConfigCategory.Instance.Contain(classId) ? ClassConfigCategory.Instance.Get(classId) : null;
        }

        public static NpcConfig Npc(int npcId)
        {
            return NpcConfigCategory.Instance.Contain(npcId) ? NpcConfigCategory.Instance.Get(npcId) : null;
        }

        public static MonsterConfig Monster(int monsterId)
        {
            return MonsterConfigCategory.Instance.Contain(monsterId)
                    ? MonsterConfigCategory.Instance.Get(monsterId)
                    : null;
        }

        public static HairStyleConfig HairStyle(int id)
        {
            return HairStyleConfigCategory.Instance.Contain(id) ? HairStyleConfigCategory.Instance.Get(id) : null;
        }

        public static HairColorConfig HairColor(int id)
        {
            return HairColorConfigCategory.Instance.Contain(id) ? HairColorConfigCategory.Instance.Get(id) : null;
        }

        public static CharacterGenderConfig Gender(int gender)
        {
            return CharacterGenderConfigCategory.Instance.Contain(gender)
                    ? CharacterGenderConfigCategory.Instance.Get(gender)
                    : null;
        }

        public static CharacterPreviewConfig Preview => CharacterPreviewConfigCategory.Instance.GetOne();

        public static SystemConfig System => SystemConfigCategory.Instance.GetOne();
    }
}
