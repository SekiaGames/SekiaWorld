using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(UI))]
    public class UISetViewComponent: Entity, IAwake
    {
        public Button closeButton;

        public Button switchRoleButton;

        public Button quitButton;

        public Dictionary<string, Button> tabs = new();

        public Dictionary<string, Control> pages = new();

        public Dictionary<string, Control> tabMarks = new();

        public string currentTab;

        public int stubButtonCount;
    }
}
