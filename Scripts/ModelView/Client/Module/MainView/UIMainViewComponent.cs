using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(UI))]
    public class UIMainViewComponent: Entity, IAwake
    {
        public Label level;

        public Label sysTime;

        public Dictionary<string, Button> buttons = new();

        public int expectedButtonCount;

        public Control moreBord;

        public Dictionary<string, Button> moreCells = new();
    }
}
