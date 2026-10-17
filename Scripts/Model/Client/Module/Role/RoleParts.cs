using System.Collections.Generic;

namespace ET.Client
{
    [EnableClass]
    public static class PartIndex
    {
        public const int Body = 1;
        public const int Hair = 2;
        public const int LeftWeapon = 3;
        public const int RightWeapon = 4;
        public const int Head = 5;
        public const int Wing = 6;
        public const int Face = 7;
        public const int Tail = 8;
        public const int Eye = 9;
        public const int Mouth = 10;
        public const int Mount = 11;

        public const int Count = 11;

        public const int Gender = 12;
        public const int HairColorIndex = 13;
        public const int EyeColorIndex = 14;
        public const int SmoothDisplay = 15;
        public const int BodyColorIndex = 16;

        [StaticField]
        public static readonly string[] SlotDir =
        {
            null,
            "Body", "Hair", "Weapon", "Weapon", "Head",
            null, null, null, "Eye", null, null,
        };

        [StaticField]
        public static readonly Dictionary<int, string> SlotMount = new()
        {
            { Hair, "CP_1" },
            { Head, "CP_4" },
            { Eye, "CP_8" },
            { RightWeapon, "CP_2" },
            { LeftWeapon, "CP_3" },
        };
    }

    [EnableClass]
    public class RoleParts
    {
        private readonly int[] ids = new int[PartIndex.Count + 1];

        public int Gender { get; set; }

        public int HairColorIndex { get; set; }

        public int EyeColorIndex { get; set; }
        public int BodyColorIndex { get; set; }

        public int this[int index]
        {
            get => (index >= 1 && index <= PartIndex.Count) ? this.ids[index] : 0;
            set { if (index >= 1 && index <= PartIndex.Count) this.ids[index] = value; }
        }

        public int Body { get => this[PartIndex.Body]; set => this[PartIndex.Body] = value; }
        public int Hair { get => this[PartIndex.Hair]; set => this[PartIndex.Hair] = value; }
        public int LeftWeapon { get => this[PartIndex.LeftWeapon]; set => this[PartIndex.LeftWeapon] = value; }
        public int RightWeapon { get => this[PartIndex.RightWeapon]; set => this[PartIndex.RightWeapon] = value; }
        public int Head { get => this[PartIndex.Head]; set => this[PartIndex.Head] = value; }
        public int Eye { get => this[PartIndex.Eye]; set => this[PartIndex.Eye] = value; }

        public static RoleParts FromNpc(NpcConfig npc)
        {
            if (npc == null)
            {
                return null;
            }

            RoleParts parts = new()
            {
                Body = npc.Body,
                Hair = npc.Hair,
                LeftWeapon = npc.LeftHand,
                RightWeapon = npc.RightHand,
                Head = npc.Head,
                Eye = npc.Eye,
                Gender = npc.Gender,
                HairColorIndex = npc.HeadDefaultColor,
                EyeColorIndex = npc.EyeDefaultColor,
                BodyColorIndex = npc.BodyDefaultColor,
            };
            return parts;
        }

        public static RoleParts FromMonster(MonsterConfig monster)
        {
            if (monster == null)
            {
                return null;
            }

            RoleParts parts = new()
            {
                Body = monster.Body,
                Hair = monster.Hair,
                LeftWeapon = monster.LeftHand,
                RightWeapon = monster.RightHand,
                Head = monster.Head,
                Eye = monster.Eye,
                HairColorIndex = monster.HeadDefaultColor,
            };
            return parts;
        }

        public RoleParts Clone()
        {
            RoleParts p = new()
            {
                Gender = this.Gender,
                HairColorIndex = this.HairColorIndex,
                EyeColorIndex = this.EyeColorIndex,
                BodyColorIndex = this.BodyColorIndex,
            };
            for (int i = 1; i <= PartIndex.Count; ++i)
            {
                p[i] = this[i];
            }
            return p;
        }

        public override string ToString()
        {
            return $"Body={this.Body} Hair={this.Hair} Head={this.Head} Eye={this.Eye} "
                    + $"L={this.LeftWeapon} R={this.RightWeapon} gender={this.Gender} hairColor={this.HairColorIndex}";
        }
    }
}
