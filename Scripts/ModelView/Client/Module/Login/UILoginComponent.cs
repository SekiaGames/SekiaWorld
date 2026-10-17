using Godot;

namespace ET.Client
{
    public enum LoginMode
    {
        Login = 0,

        Register = 1,

        ResetPassword = 2,
    }

    [ComponentOf(typeof(UI))]
    public class UILoginComponent: Entity, IAwake
    {
        public LoginMode mode;

        public Control loginPage;
        public Control registerPage;
        public Control lostPage;

        public LineEdit loginEmail;
        public LineEdit loginPassword;

        public Button rememberMe;

        public Button startButton;

        public Button goRegisterButton;

        public Button goLostButton;

        public LineEdit registerEmail;
        public LineEdit registerPassword;
        public LineEdit registerCode;
        public Button registerSendCodeButton;

        public Button registerNowButton;

        public Button registerReturnButton;

        public LineEdit lostEmail;
        public LineEdit lostCode;
        public LineEdit lostPassword;
        public Button lostSendCodeButton;

        public Button resetNowButton;

        public Button lostReturnButton;

        public Label tip;

        public Control confirmDialog;

        public Label confirmMessage;

        public Button confirmOkBtn;

        public bool busy;

        public string savedEmail;

        public string savedToken;
    }
}
