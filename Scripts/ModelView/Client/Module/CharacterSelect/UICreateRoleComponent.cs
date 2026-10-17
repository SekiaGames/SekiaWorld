using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(UI))]
    public class UICreateRoleComponent: Entity, IAwake, IUpdate
    {

        public Button sexBtnMale;
        public Button sexBtnFemale;

        public Panel femaleCover;

        public List<Button> hairSlots = new();

        public List<Control> hairFrames = new();

        public List<TextureRect> hairIcons = new();

        public List<Control> hairSelects = new();

        public Button hairLeftBtn;
        public Button hairRightBtn;
        public Label hairColorLab;

        public List<Button> headSlots = new();

        public List<Control> headFrames = new();

        public List<TextureRect> headIcons = new();

        public LineEdit nameInput;

        public Label profLabel;
        public Label profLabelEng;
        public TextureRect profFlag;
        public TextureRect profIcon;

        public TextureRect symbolDisc;

        public Node2D attrPolygon;

        public List<Label> attrLabels = new();

        public Button enterBtn;
        public Button returnBtn;

        public TextureRect titleBanner;

        public Control leftPanel;

        public Control rightPanel;

        public int classIndex = 1;

        public int gender = 1;

        public int hairIndex;

        public int hairColorIndex;

        public int headIndex;

        public bool introRevealed;

        public bool busy;

        public bool mouseDown;

        public int selectedClass;

        public bool awaitingReveal;

        public bool awaitingCollapse;
    }
}
