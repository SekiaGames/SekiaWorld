using System;
using CommandLine;
using Godot;

namespace ET
{
	public partial class Init: Node
	{
		public static Init Instance { get; private set; }

		public static CodeMode codeMode = CodeMode.None;

		// 设计宽高比 16:9（设计分辨率 1280x720）。
		// 窗口宽高比被夹在 [12:9, 19.5:9] 之间：
		//   12:9   —— 兼容 iPad（偏方），到 12:9 不允许再变窄；
		//   19.5:9 —— 兼容宽屏手机（偏宽），到 19.5:9 不允许再变宽。
		private const float MinWindowRatio = 12f / 9f;
		private const float MaxWindowRatio = 19.5f / 9f;

		// 夹取后比例只可能差在取整误差上，留一点容差避免反复触发
		private const float RatioEpsilon = 0.0005f;

		private bool _clampingWindow;

		public override void _Ready()
		{
			// 最低分辨率 1280x720：桌面端窗口不允许被拖得更小
			Window window = GetWindow();
			window.MinSize = new Vector2I(1280, 720);
			window.SizeChanged += OnWindowSizeChanged;

			Instance = this;

			World.Instance.AddSingleton<Logger>().Log = new GodotLogger();

			GlobalComponent.RootNode = this;

			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				Log.Error(e.ExceptionObject.ToString());
			};

			// 只吃 `--` 之后那段用户参数（Godot 自己的 --headless/--path 不进来，否则 CommandLine 解析会报错）
			// 例：Godot --headless --path Godot -- --AgentMode=1
			Parser.Default.ParseArguments<Options>(OS.GetCmdlineUserArgs())
					.WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
					.WithParsed(o => World.Instance.AddSingleton(o));

			Options.Instance.StartConfig = "StartConfig/Localhost";

			Options.Instance.AppType = AppType.Server;

			ETTask.ExceptionHandler += Log.Error;

			// 美术包必须早于任何资源加载挂上：pck 里的资源靠 `res://` 路径取，
			// 挂晚了主工程就按「资源不存在」处理。
			PckLoader.MountAll();

			World.Instance.AddSingleton<TimeInfo>();
			World.Instance.AddSingleton<FiberManager>();

			CodeLoader codeLoader = World.Instance.AddSingleton<CodeLoader>();
			codeLoader.Start();
		}

		// 用户拖动窗口时把宽高比夹回兼容范围：只改宽度，高度不动。
		// 比 19.5:9 更宽则收窄到 19.5:9；比 12:9 更方则放宽到 12:9。
		private void OnWindowSizeChanged()
		{
			if (_clampingWindow)
			{
				return;
			}

			Window window = GetWindow();
			Vector2I size = window.Size;
			if (size.Y <= 0)
			{
				return;
			}

			float ratio = (float)size.X / size.Y;
			int width;
			if (ratio > MaxWindowRatio + RatioEpsilon)
			{
				width = Mathf.RoundToInt(size.Y * MaxWindowRatio);
			}
			else if (ratio < MinWindowRatio - RatioEpsilon)
			{
				width = Mathf.RoundToInt(size.Y * MinWindowRatio);
			}
			else
			{
				return;
			}

			_clampingWindow = true;
			window.Size = new Vector2I(width, size.Y);
			_clampingWindow = false;
		}

		public override void _Process(double delta)
		{
			if (Instance != this || TimeInfo.Instance == null)
			{
				return;
			}

			TimeInfo.Instance.Update();
			FiberManager.Instance.Update();
			FiberManager.Instance.LateUpdate();
		}

		public override void _Notification(int what)
		{
			if (what != NotificationWMCloseRequest || Instance != this)
			{
				return;
			}

			World.Instance.Dispose();
			this.GetTree().Quit();
		}
	}
}
