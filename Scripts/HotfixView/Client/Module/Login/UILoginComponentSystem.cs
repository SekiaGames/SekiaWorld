using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(UILoginComponent))]
    [FriendOf(typeof(UILoginComponent))]
    public static partial class UILoginComponentSystem
    {
        private const string SavedPasswordMask = "********";

        private const string LoginPage = "Login";
        private const string LoginEmail = "Login/EmailInput";
        private const string LoginPassword = "Login/PasswordInput";
        private const string LoginRememberMe = "Login/RememberMe/Check";
        private const string LoginStartButton = "Login/StartButton";
        private const string LoginGoRegisterButton = "Login/RegisterButton";
        private const string LoginGoLostButton = "Login/LostButton";

        private const string RegisterPage = "Register";
        private const string RegisterEmail = "Register/EmailInput";
        private const string RegisterPassword = "Register/PasswordInput";
        private const string RegisterCode = "Register/CodeInput";
        private const string RegisterSendCodeButton = "Register/SendButton";
        private const string RegisterNowButton = "Register/RegisterNowButton";
        private const string RegisterReturnButton = "Register/ReturnButton";

        private const string LostPage = "Lost";
        private const string LostEmail = "Lost/EmailInput";
        private const string LostCode = "Lost/CodeInput";
        private const string LostPassword = "Lost/PasswordInput";
        private const string LostSendCodeButton = "Lost/SendButton";
        private const string LostResetNowButton = "Lost/ResetNowButton";
        private const string LostReturnButton = "Lost/ReturnButton";

        private const string TipNode = "Tip";
        private const string ConfirmDialogNode = "ConfirmDialog";
        private const string ConfirmMessageNode = "ConfirmDialog/Box/ConfirmMessage";
        private const string ConfirmOkNode = "ConfirmDialog/Box/ConfirmOkButton";

        [EntitySystem]
        private static void Awake(this UILoginComponent self)
        {
            Node panel = self.GetParent<UI>().GameObject;

            self.loginPage = panel.GetNode<Control>(LoginPage);
            self.registerPage = panel.GetNode<Control>(RegisterPage);
            self.lostPage = panel.GetNode<Control>(LostPage);

            self.loginEmail = panel.GetNode<LineEdit>(LoginEmail);
            self.loginPassword = panel.GetNode<LineEdit>(LoginPassword);
            self.rememberMe = panel.GetNode<Button>(LoginRememberMe);
            self.startButton = panel.GetNode<Button>(LoginStartButton);
            self.goRegisterButton = panel.GetNode<Button>(LoginGoRegisterButton);
            self.goLostButton = panel.GetNode<Button>(LoginGoLostButton);

            self.registerEmail = panel.GetNode<LineEdit>(RegisterEmail);
            self.registerPassword = panel.GetNode<LineEdit>(RegisterPassword);
            self.registerCode = panel.GetNode<LineEdit>(RegisterCode);
            self.registerSendCodeButton = panel.GetNode<Button>(RegisterSendCodeButton);
            self.registerNowButton = panel.GetNode<Button>(RegisterNowButton);
            self.registerReturnButton = panel.GetNode<Button>(RegisterReturnButton);

            self.lostEmail = panel.GetNode<LineEdit>(LostEmail);
            self.lostCode = panel.GetNode<LineEdit>(LostCode);
            self.lostPassword = panel.GetNode<LineEdit>(LostPassword);
            self.lostSendCodeButton = panel.GetNode<Button>(LostSendCodeButton);
            self.resetNowButton = panel.GetNode<Button>(LostResetNowButton);
            self.lostReturnButton = panel.GetNode<Button>(LostReturnButton);

            self.tip = panel.GetNodeOrNull<Label>(TipNode);
            self.confirmDialog = panel.GetNodeOrNull<Control>(ConfirmDialogNode);
            self.confirmMessage = panel.GetNodeOrNull<Label>(ConfirmMessageNode);
            self.confirmOkBtn = panel.GetNodeOrNull<Button>(ConfirmOkNode);
            if (self.tip == null || self.confirmDialog == null)
            {
                Log.Warning($"[登录] 场景里没有 {TipNode} / {ConfirmDialogNode} 节点：" +
                        $"提示改为只写日志，注册/重置成功后直接回登录页。" +
                        $"需要界面提示的话，在 StartGamePanel 下补上 {TipNode} / {ConfirmDialogNode} 两块（名字层级保持一致）。");
            }

            self.startButton.Pressed += () => self.OnLogin();
            self.goRegisterButton.Pressed += () => self.SetMode(LoginMode.Register);
            self.goLostButton.Pressed += () => self.SetMode(LoginMode.ResetPassword);
            self.registerNowButton.Pressed += () => self.OnRegister();
            self.registerReturnButton.Pressed += () => self.SetMode(LoginMode.Login);
            self.registerSendCodeButton.Pressed += () => self.OnSendCode();
            self.resetNowButton.Pressed += () => self.OnResetPassword();
            self.lostReturnButton.Pressed += () => self.SetMode(LoginMode.Login);
            self.lostSendCodeButton.Pressed += () => self.OnSendCode();
            if (self.confirmOkBtn != null)
            {
                self.confirmOkBtn.Pressed += () => self.CloseConfirm();
            }

            self.rememberMe.Toggled += pressed =>
            {
                if (!pressed && self.loginPassword.Text == SavedPasswordMask)
                {
                    self.loginPassword.Text = string.Empty;
                }
            };

            self.SetMode(LoginMode.Login);

            LoginTokenStore.Load(out self.savedEmail, out self.savedToken);
            if (self.savedToken != null)
            {
                self.loginEmail.Text = self.savedEmail;
                self.loginPassword.Text = SavedPasswordMask;
                self.rememberMe.ButtonPressed = true;
            }

            self.loginEmail.TextChanged += text =>
            {
                if (text.Trim().ToLowerInvariant() != self.savedEmail)
                {
                    self.ClearSavedPassword();
                }
            };
        }

        public static void SetMode(this UILoginComponent self, LoginMode mode)
        {
            if (self.mode != mode)
            {
                string email = self.EmailInputOf(self.mode).Text;
                self.mode = mode;

                self.EmailInputOf(mode).Text = email;

                LineEdit code = self.CodeInputOf(mode);
                if (code != null)
                {
                    code.Text = string.Empty;
                }

                self.PasswordInputOf(mode).Text = string.Empty;
            }

            self.loginPage.Visible = mode == LoginMode.Login;
            self.registerPage.Visible = mode == LoginMode.Register;
            self.lostPage.Visible = mode == LoginMode.ResetPassword;

            self.SetBusy(false);
            self.ShowTip(string.Empty, false);
        }

        private static LineEdit EmailInputOf(this UILoginComponent self, LoginMode mode)
        {
            switch (mode)
            {
                case LoginMode.Register:
                    return self.registerEmail;
                case LoginMode.ResetPassword:
                    return self.lostEmail;
                default:
                    return self.loginEmail;
            }
        }

        private static LineEdit PasswordInputOf(this UILoginComponent self, LoginMode mode)
        {
            switch (mode)
            {
                case LoginMode.Register:
                    return self.registerPassword;
                case LoginMode.ResetPassword:
                    return self.lostPassword;
                default:
                    return self.loginPassword;
            }
        }

        private static LineEdit CodeInputOf(this UILoginComponent self, LoginMode mode)
        {
            switch (mode)
            {
                case LoginMode.Register:
                    return self.registerCode;
                case LoginMode.ResetPassword:
                    return self.lostCode;
                default:
                    return null;
            }
        }

        private static void ClearSavedPassword(this UILoginComponent self)
        {
            if (self.loginPassword.Text == SavedPasswordMask)
            {
                self.loginPassword.Text = string.Empty;
            }
        }

        private static void CloseConfirm(this UILoginComponent self)
        {
            self.confirmDialog.Visible = false;

            string password = self.PasswordInputOf(self.mode).Text;
            string message = self.confirmMessage.Text;

            self.SetMode(LoginMode.Login);
            self.loginPassword.Text = password;
            self.ShowTip($"{message}，请登录", false);
        }

        private static void ShowConfirm(this UILoginComponent self, string message)
        {
            if (self.confirmDialog == null)
            {
                string password = self.PasswordInputOf(self.mode).Text;
                self.SetMode(LoginMode.Login);
                self.loginPassword.Text = password;
                self.ShowTip($"{message}，请登录", false);
                return;
            }

            self.confirmMessage.Text = message;
            self.confirmDialog.Visible = true;
            self.SetBusy(false);
        }

        private static void SetBusy(this UILoginComponent self, bool busy)
        {
            self.busy = busy;
        }

        public static void OnLogin(this UILoginComponent self)
        {
            if (self.busy)
            {
                return;
            }

            self.LoginAsync().Coroutine();
        }

        public static void OnSendCode(this UILoginComponent self)
        {
            if (self.busy)
            {
                return;
            }

            self.SendCodeAsync().Coroutine();
        }

        public static void OnRegister(this UILoginComponent self)
        {
            if (self.busy)
            {
                return;
            }

            self.RegisterAsync().Coroutine();
        }

        public static void OnResetPassword(this UILoginComponent self)
        {
            if (self.busy)
            {
                return;
            }

            self.ResetPasswordAsync().Coroutine();
        }

        private static async ETTask LoginAsync(this UILoginComponent self)
        {
            string email = self.ReadEmail();
            if (email == null)
            {
                return;
            }

            bool useToken = self.rememberMe.ButtonPressed
                    && self.loginPassword.Text == SavedPasswordMask
                    && email == self.savedEmail
                    && self.savedToken != null;
            bool remember = self.rememberMe.ButtonPressed;

            string password = useToken ? null : self.loginPassword.Text;
            string token = useToken ? self.savedToken : null;
            string deviceCode = DeviceCodeHelper.Get();

            Scene root = self.Root();
            EntityRef<UILoginComponent> selfRef = self;
            self.SetBusy(true);
            self.ShowTip(useToken ? "正在免密登录…" : "登录中…", false);

            AccountOpResult result = await LoginHelper.Login(root, email, password, token, deviceCode);
            if (result.IsSuccess)
            {
                if (remember && !string.IsNullOrEmpty(result.Token))
                {
                    LoginTokenStore.Save(email, result.Token);
                }
                else if (!remember)
                {
                    LoginTokenStore.Clear();
                }

                return;
            }

            self = selfRef;
            if (self == null)
            {
                return;
            }

            if (result.Error == ErrorCode.ERR_TokenInvalid)
            {
                LoginTokenStore.Clear();
                self.savedEmail = null;
                self.savedToken = null;
                self.loginPassword.Text = string.Empty;
                self.rememberMe.ButtonPressed = false;
                self.SetBusy(false);
                self.ShowTip("登录状态已失效，请输入密码", true);
                return;
            }

            self.SetBusy(false);
            self.ShowTip($"登录失败：{result.Message}", true);
        }

        private static async ETTask SendCodeAsync(this UILoginComponent self)
        {
            string email = self.ReadEmail();
            if (email == null)
            {
                return;
            }

            bool isReset = self.mode == LoginMode.ResetPassword;
            int type = isReset ? EmailCodeType.ResetPassword : EmailCodeType.Register;

            Scene root = self.Root();
            EntityRef<UILoginComponent> selfRef = self;
            self.SetBusy(true);
            self.ShowTip(isReset ? "正在发送重置密码验证码…" : "正在发送验证码…", false);

            AccountOpResult result = await LoginHelper.SendEmailCode(root, email, type);

            self = selfRef;
            if (self == null)
            {
                return;
            }

            self.SetBusy(false);
            if (!result.IsSuccess)
            {
                self.ShowTip(result.Message, true);
                return;
            }

            self.ShowTip($"验证码已发送，请查收邮箱（收件箱找不到就看垃圾邮件箱），{result.Cooldown} 秒后可重发", false);
        }

        private static async ETTask RegisterAsync(this UILoginComponent self)
        {
            string email = self.ReadEmail();
            if (email == null)
            {
                return;
            }

            string code = self.ReadCode();
            if (code == null)
            {
                return;
            }

            string password = self.ReadPassword();
            if (password == null)
            {
                return;
            }

            Scene root = self.Root();
            EntityRef<UILoginComponent> selfRef = self;
            self.SetBusy(true);
            self.ShowTip("注册中…", false);

            AccountOpResult result = await LoginHelper.Register(root, email, code, password);

            self = selfRef;
            if (self == null)
            {
                return;
            }

            self.SetBusy(false);
            if (!result.IsSuccess)
            {
                self.ShowTip($"注册失败：{result.Message}", true);
                return;
            }

            self.ShowConfirm("注册成功");
        }

        private static async ETTask ResetPasswordAsync(this UILoginComponent self)
        {
            string email = self.ReadEmail();
            if (email == null)
            {
                return;
            }

            string code = self.ReadCode();
            if (code == null)
            {
                return;
            }

            string password = self.ReadPassword();
            if (password == null)
            {
                return;
            }

            Scene root = self.Root();
            EntityRef<UILoginComponent> selfRef = self;
            self.SetBusy(true);
            self.ShowTip("重置密码中…", false);

            AccountOpResult result = await LoginHelper.ResetPassword(root, email, code, password);

            self = selfRef;
            if (self == null)
            {
                return;
            }

            self.SetBusy(false);
            if (!result.IsSuccess)
            {
                self.ShowTip($"重置密码失败：{result.Message}", true);
                return;
            }

            LoginTokenStore.Clear();
            self.savedToken = null;

            self.ShowConfirm("重置密码成功");
        }

        private static string ReadEmail(this UILoginComponent self)
        {
            string email = self.EmailInputOf(self.mode).Text.Trim().ToLowerInvariant();
            if (email.Length == 0 || email.IndexOf('@') <= 0)
            {
                self.ShowTip("请先填写正确的邮箱", true);
                return null;
            }

            return email;
        }

        private static string ReadCode(this UILoginComponent self)
        {
            string code = self.CodeInputOf(self.mode).Text.Trim();
            if (code.Length == 0)
            {
                self.ShowTip("请先填写收到的邮箱验证码", true);
                return null;
            }

            return code;
        }

        private static string ReadPassword(this UILoginComponent self)
        {
            string password = self.PasswordInputOf(self.mode).Text;
            if (password.Length == 0)
            {
                self.ShowTip("请先填写密码", true);
                return null;
            }

            return password;
        }

        private static void ShowTip(this UILoginComponent self, string text, bool isError)
        {
            if (self.tip == null)
            {
                if (text.Length > 0)
                {
                    Log.Info(isError ? $"[登录][错误] {text}" : $"[登录] {text}");
                }

                return;
            }

            self.tip.Text = text;
            self.tip.AddThemeColorOverride("font_color",
                    isError? new Color(1f, 0.45f, 0.45f) : new Color(0.85f, 0.9f, 1f));
        }
    }
}
