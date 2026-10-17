using System;
using Godot;

namespace ET
{
	/// <summary>
	/// Godot 侧日志实现。对应 RO-Unity 的 UnityLogger。
	/// 客户端日志直接输出到 Godot 编辑器/控制台，不引 NLog。
	/// </summary>
	public class GodotLogger: ILog
	{
		public void Trace(string message)
		{
			GD.Print(message);
		}

		public void Debug(string message)
		{
			GD.Print(message);
		}

		public void Info(string message)
		{
			GD.Print(message);
		}

		public void Warning(string message)
		{
			GD.PushWarning(message);
		}

		public void Error(string message)
		{
			GD.PrintErr(message);
		}

		public void Error(Exception e)
		{
			GD.PushError(e);
		}
	}
}
