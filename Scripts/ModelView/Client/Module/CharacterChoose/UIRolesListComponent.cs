using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(UI))]
    public class UIRolesListComponent: Entity, IAwake
    {

        public Button backButton;

        public List<Button> cells = new();

        public List<TextureRect> cellFrames = new();

        public List<TextureRect> cellIcons = new();

        public Button startButton;

        public Button deleteButton;

        public Label startButtonTitle;

        public List<bool> cellCreatable = new();

        public List<RoleInfo> cellRoles = new();

        public int selectedIndex = -1;

        public bool busy;
    }
}
