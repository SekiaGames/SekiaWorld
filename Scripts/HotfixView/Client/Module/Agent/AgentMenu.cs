#if DEBUG
using System;
using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [FriendOf(typeof(UILoginComponent))]
    [FriendOf(typeof(UIRolesListComponent))]
    [FriendOf(typeof(UICreateRoleComponent))]
    [FriendOf(typeof(UIMainViewComponent))]
    public static class AgentMenu
    {
        private const int WaitScreenInterval = 100;
        private const int WaitScreenMaxTimes = 50;

        private const string NoScreen = "";

        private const string ScreenLogin = "UILogin";

        private const string ScreenRolesList = "UIViewRolesList";
        private const string ScreenCreateRole = "CreateRoleViewV2";

        private const string ScreenMainView = "MainView";

        public static void Broadcast(Scene root)
        {
            List<(string Label, Func<string[], string> Action)> items = Items(root);

            Log.Console($"--- 当前界面：{DescribeScreen(root)}，可操作项 {items.Count} 条 ---");

            if (items.Count == 0)
            {
                Log.Console("（界面还在切换，或已进图）输入 ? 刷新，quit 退出游戏");
                return;
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Log.Console($"  {i + 1}. {items[i].Label}");
            }
        }

        public static async ETTask WaitScreenAsync(Scene root)
        {
            EntityRef<Scene> rootRef = root;
            EntityRef<TimerComponent> timerRef = root.TimerComponent;

            for (int i = 0; i < WaitScreenMaxTimes; ++i)
            {
                root = rootRef;
                if (root == null)
                {
                    return;
                }

                if (ScreenKey(root) != NoScreen)
                {
                    return;
                }

                TimerComponent timer = timerRef;
                await timer.WaitAsync(WaitScreenInterval);
            }
        }

        public static string ScreenKey(Scene root)
        {
            if (GetLogin(root) != null)
            {
                return ScreenLogin;
            }

            if (GetRolesList(root) != null)
            {
                return ScreenRolesList;
            }

            if (GetCreateRole(root) != null)
            {
                return ScreenCreateRole;
            }

            if (GetUI(root, UIType.MainView) != null)
            {
                return ScreenMainView;
            }

            return NoScreen;
        }

        public static string Invoke(Scene root, int index, string[] args)
        {
            List<(string Label, Func<string[], string> Action)> items = Items(root);
            if (index < 1 || index > items.Count)
            {
                return $"没有第 {index} 项（当前界面可操作项 {items.Count} 条，输入 ? 查看）";
            }

            try
            {
                return items[index - 1].Action(args);
            }
            catch (Exception e)
            {
                return $"执行第 {index} 项出错：{e.Message}";
            }
        }

        private static List<(string Label, Func<string[], string> Action)> Items(Scene root)
        {
            List<(string Label, Func<string[], string> Action)> items = new();

            UILoginComponent login = GetLogin(root);
            if (login != null)
            {
                items.Add(("账号密码登录    用法：1 <邮箱> <密码>", args => PasswordLogin(login, args)));
                return items;
            }

            if (GetUI(root, UIType.MainView) != null)
            {
                items.Add(("点地寻路（模拟：往屏幕中心打点地面）", args => ClickGroundCenter(root)));
                items.Add(("更多面板（开/关）", args => ToggleMoreBord(root)));
                items.Add(("设置界面：打开（= 更多面板里的「设置」格）", args => OpenSetView(root)));
                items.Add(("设置界面：切分页    用法：4 <1 系统|2 效果|3 安全>", args => SwitchSetViewTab(root, args)));
                items.Add(("设置界面：关闭", args => CloseSetView(root)));
                items.Add(("设置界面：点「切换角色」（退回角色选择界面）", args => ClickSwitchRole(root)));
                items.Add(("相机：报告状态（含玩家在不在视锥里）", args => CameraStatus(root)));
                items.Add(("传送到指定地图    用法：8 <地图Id，1=普隆德拉 / 2=普隆德拉南门 prt_1>",
                        args => ChangeMap(root, args)));
                items.Add(("走到传送阵（测试用：走进去即切图）", args => WalkToWarp(root)));
                return items;
            }

            UIRolesListComponent roles = GetRolesList(root);
            if (roles != null)
            {
                items.Add(("创建角色（进创角场景 + 创角界面）", args => CreateRole(roles)));
                items.Add(("返回（一期未实现回登录，只记日志）", args => BackToLogin(roles)));
                items.Add(("选中角色槽 1..3    用法：3 <序号>", args => SelectSlot(roles, args)));
                items.Add(("删除选中角色（不做冷却，直接删）", args => DeleteRole(roles)));
                return items;
            }

            UICreateRoleComponent create = GetCreateRole(root);
            if (create != null)
            {
                items.Add(("进入游戏（创角提交 + 进首图）", args => EnterGame(create)));
                items.Add(("返回选角", args => ReturnToRolesList(create)));
                items.Add(("选中职业 1..6    用法：3 <序号>", args => SelectClass(create, args)));
                items.Add(("切性别：男", args => SetGender(create, 1)));
                items.Add(("切性别：女", args => SetGender(create, 2)));
                items.Add(("换发型（下一款）", args => NextHair(create)));
                items.Add(("换发色（下一个）", args => NextHairColor(create)));
                items.Add(("换头饰（下一个）", args => NextHead(create)));
                items.Add(("射线拾取自检（逐个 NPC 打点选射线）", args => PickingSelfCheck(create)));
                items.Add(("输入名字        用法：10 <名字>", args => SetName(create, args)));
                return items;
            }

            return items;
        }

        private static string PasswordLogin(UILoginComponent login, string[] args)
        {
            if (args.Length < 3)
            {
                return "参数不足，用法：1 <邮箱> <密码>";
            }

            login.SetMode(LoginMode.Login);

            login.loginEmail.Text = args[1];
            login.loginPassword.Text = args[2];
            login.OnLogin();
            return $"已提交账号密码登录：{args[1]}";
        }

        private static string WalkToWarp(Scene root)
        {
            Scene currentScene = root.CurrentScene();
            if (currentScene == null)
            {
                return "还没进图（没有 Current 场景）";
            }

            List<WarpPoint> warps = MapWarp.GetWarps(currentScene.Name);
            if (warps.Count == 0)
            {
                return $"{currentScene.Name} 没有登记的传送阵";
            }

            OperaComponent opera = currentScene.GetComponent<OperaComponent>();
            if (opera == null)
            {
                return "当前场景没有 OperaComponent（没进图？）";
            }

            Camera3D camera = GlobalComponent.RootNode.GetViewport().GetCamera3D();
            if (camera == null)
            {
                return "拿不到相机";
            }

            Vector3 warp = warps[0].Position;
            Vector2 screen = camera.UnprojectPosition(warp);
            Vector2 view = GlobalComponent.RootNode.GetViewport().GetVisibleRect().Size;
            bool onScreen = screen.X >= 0 && screen.Y >= 0 && screen.X < view.X && screen.Y < view.Y;
            Log.Info($"[传送阵] 走到传送阵：阵心 {warp.X:F2}, {warp.Y:F2}, {warp.Z:F2} → 屏幕 ({screen.X:F0}, {screen.Y:F0})"
                    + (onScreen ? "（在视野内 ⇒ 按屏幕坐标点地）" : "（不在视野内 ⇒ 按世界坐标直接寻路）"));
            if (onScreen)
            {
                opera.RaycastMoveAt(screen).Coroutine();
            }
            else
            {
                opera.MoveToWorld(warp).Coroutine();
            }

            return "已按传送阵的位置走去（异步：等 [寻路] / [传送阵] / [进图] 日志，或 ? 刷新）";
        }

        private static string ChangeMap(Scene root, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int mapId))
            {
                return "用法：8 <地图Id>（1=普隆德拉 / 2=普隆德拉南门 prt_1）";
            }

            ChangeMapHelper.ChangeMapAsync(root, mapId).Coroutine();
            return $"已请求传送到地图 {mapId}（异步：等 [传送] 与 [进图] 加载地图场景 日志，或 ? 刷新）";
        }

        private static string CreateRole(UIRolesListComponent roles)
        {
            roles.OnClickStart();
            return "已提交「创建角色」（异步：加载创角场景 + 开创角界面，等界面变化或输入 ? 刷新）";
        }

        private static string BackToLogin(UIRolesListComponent roles)
        {
            roles.OnClickBack();
            return "已点「返回」（一期未实现回登录）";
        }

        private static string ClickGroundCenter(Scene root)
        {
            OperaComponent opera = root.CurrentScene()?.GetComponent<OperaComponent>();
            if (opera == null)
            {
                return "当前场景没有 OperaComponent（没进图？）";
            }

            opera.RaycastAtScreenCenter();
            return "已往屏幕中心打点地面（异步：等 [寻路] 日志或 ? 刷新）";
        }

        private static string ToggleMoreBord(Scene root)
        {
            UIMainViewComponent mainView = GetMainView(root);
            if (mainView == null)
            {
                return "没拿到主界面组件";
            }

            if (mainView.moreBord == null)
            {
                return "主界面里没有更多面板（MoreBord）";
            }

            mainView.ToggleMoreBord();
            return $"已切「更多面板」：现在{(mainView.moreBord.Visible ? "展开" : "收起")}"
                    + $"（菜单格 {mainView.moreCells.Count} 个）";
        }

        private static string OpenSetView(Scene root)
        {
            UIMainViewComponent mainView = GetMainView(root);
            if (mainView == null)
            {
                return "没拿到主界面组件";
            }

            mainView.OnClickMoreCell("设置");
            return "已点「更多面板 → 设置」（异步：等 [设置] 日志或 ? 刷新）";
        }

        private static string SwitchSetViewTab(Scene root, string[] args)
        {
            UISetViewComponent setView = GetSetView(root);
            if (setView == null)
            {
                return "设置界面没开着";
            }

            if (args.Length < 2)
            {
                return "参数不足，用法：4 <1 系统|2 效果|3 安全>";
            }

            string tab = args[1] switch
            {
                "1" => "SystemSetting",
                "2" => "EffectSetting",
                "3" => "SecuritySetting",
                _ => null,
            };
            if (tab == null)
            {
                return "参数不对，用法：4 <1 系统|2 效果|3 安全>";
            }

            setView.SwitchTab(tab);
            return $"已切设置分页：{tab}";
        }

        private static string CloseSetView(Scene root)
        {
            UISetViewComponent setView = GetSetView(root);
            if (setView == null)
            {
                return "设置界面没开着";
            }

            setView.OnClickClose();
            return "已点「关闭」";
        }

        private static string ClickSwitchRole(Scene root)
        {
            UISetViewComponent setView = GetSetView(root);
            if (setView != null)
            {
                setView.OnClickSwitchRole();
                return "已点设置界面的「切换角色」（异步：等 [回选角] 日志或 ? 刷新）";
            }

            CharacterChooseHelper.ReturnAsync(root).Coroutine();
            return "设置界面没开着 ⇒ 直接走「回选角」同一条路（异步：等 [回选角] 日志或 ? 刷新）";
        }

        private static string SelectSlot(UIRolesListComponent roles, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int slot) || slot < 1 || slot > 3)
            {
                return "参数不对，用法：3 <序号 1..3>";
            }

            roles.OnClickCell(slot - 1);
            RoleInfo selected = roles.SelectedRole();
            return selected != null
                    ? $"已选中角色槽 {slot}：「{selected.Name}」"
                    : $"已点角色槽 {slot}（现在是取消选中/或该槽是空位）";
        }

        private static string DeleteRole(UIRolesListComponent roles)
        {
            roles.OnClickDelete();
            return "已点「删除角色」（Unity 那边有删除冷却，这里不做，直接删）";
        }

        private static string CameraStatus(Scene root)
        {
            MainCameraComponent camera = root.GetComponent<MainCameraComponent>();
            if (camera == null)
            {
                return "没拿到 MainCameraComponent（全局主相机）";
            }

            Log.Info(camera.Describe());
            return "已报告相机状态（看日志里那条 [相机] 状态：…）";
        }

        private static string EnterGame(UICreateRoleComponent create)
        {
            create.OnClickEnter();
            return "已点「进入游戏」（本地校验通过则进首图）";
        }

        private static string ReturnToRolesList(UICreateRoleComponent create)
        {
            create.OnClickReturn();
            return "已点「返回」（已选中职业时先收回创角界面，再点一次才回选角）";
        }

        private static string SelectClass(UICreateRoleComponent create, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int index) || index < 1 || index > 6)
            {
                return "参数不足，用法：3 <序号 1..6>";
            }
            create.SelectClass(index);
            return $"已选中职业序号 {index}（播 wait{index} 镜头，播完展开创角界面）";
        }

        private static string PickingSelfCheck(UICreateRoleComponent create)
        {
            CharacterSelectComponent select = create.Root().GetComponent<CharacterSelectComponent>();
            if (select == null)
            {
                return "射线拾取自检：没有创角场景组件";
            }
            return $"射线拾取自检：{select.SelfCheckPicking()}";
        }

        private static string SetName(UICreateRoleComponent create, string[] args)
        {
            if (args.Length < 2)
            {
                return "参数不足，用法：10 <名字>";
            }

            create.nameInput.Text = args[1];
            return $"已输入名字「{args[1]}」";
        }

        private static string SetGender(UICreateRoleComponent create, int gender)
        {
            create.SetGender(gender);
            return $"已切性别为 {(gender == 2 ? "女" : "男")}";
        }

        private static string NextHair(UICreateRoleComponent create)
        {
            create.StepHair(1);
            return "已换下一款发型";
        }

        private static string NextHairColor(UICreateRoleComponent create)
        {
            create.StepHairColor(1);
            return "已换下一个发色";
        }

        private static string NextHead(UICreateRoleComponent create)
        {
            create.OnClickHeadSlot((create.headIndex + 1) % 3);
            return "已换下一个头饰";
        }

        private static UILoginComponent GetLogin(Scene root)
        {
            return GetUI(root, UIType.UILogin)?.GetComponent<UILoginComponent>();
        }

        private static UIRolesListComponent GetRolesList(Scene root)
        {
            return GetUI(root, UIType.UIViewRolesList)?.GetComponent<UIRolesListComponent>();
        }

        private static UICreateRoleComponent GetCreateRole(Scene root)
        {
            return GetUI(root, UIType.CreateRoleViewV2)?.GetComponent<UICreateRoleComponent>();
        }

        private static UIMainViewComponent GetMainView(Scene root)
        {
            return GetUI(root, UIType.MainView)?.GetComponent<UIMainViewComponent>();
        }

        private static UISetViewComponent GetSetView(Scene root)
        {
            return GetUI(root, UIType.SetView)?.GetComponent<UISetViewComponent>();
        }

        private static UI GetUI(Scene root, string uiType)
        {
            return root.GetComponent<UIComponent>()?.Get(uiType);
        }

        private static string DescribeScreen(Scene root)
        {
            switch (ScreenKey(root))
            {
                case ScreenLogin:
                    return "登录界面";
                case ScreenRolesList:
                    return "选角界面";
                case ScreenCreateRole:
                    return "创角界面";
                case ScreenMainView:
                    return "主界面（进游戏后）";
                default:
                    return "无（界面切换中或已进图）";
            }
        }
    }
}
#endif
