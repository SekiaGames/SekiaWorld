using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [EnableClass]
    public class RoleGroup
    {
        public Node3D Node { get; set; }

        public Node3D MaleNode { get; set; }

        public Node3D FemaleNode { get; set; }

        public int DefaultGender { get; set; } = 1;

        public string MaleIdle { get; set; } = "choose_wait";
        public string FemaleIdle { get; set; } = "choose_wait";

        public string IdleAction(int gender)
        {
            return gender == 2 ? this.FemaleIdle : this.MaleIdle;
        }

        public EntityRef<RoleComponent> MaleRole { get; set; }
        public EntityRef<RoleComponent> FemaleRole { get; set; }

        public Node3D PetNode { get; set; }

        public EntityRef<RoleComponent> PetRole { get; set; }

        public string PetIdle { get; set; }

        public RoleComponent Role(int gender)
        {
            return gender == 2 ? this.FemaleRole : this.MaleRole;
        }

        public void SetGenderVisible(int gender)
        {
            if (this.MaleNode != null)
            {
                this.MaleNode.Visible = gender != 2;
            }
            if (this.FemaleNode != null)
            {
                this.FemaleNode.Visible = gender == 2;
            }
        }
    }

    [ComponentOf(typeof(Scene))]
    public class CharacterSelectComponent: Entity, IAwake, IUpdate
    {
        public ETTask loadTcs;

        public Node sceneNode;

        public Camera3D camera;

        public bool active;

        public AnimationPlayer cameraAnim;

        public bool introPlaying;

        public bool introDone;

        public Node3D rolesRoot;

        public List<RoleGroup> groups { get; set; } = new();

        public bool rolesReady;

        public int activeClassIndex = 1;

        public int activeGender = 1;

        public string selectAnim;

        public int selectPhase;

        public RoleParts stashedMale;
        public RoleParts stashedFemale;
    }
}
