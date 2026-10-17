using System;

namespace ET
{
	[Flags]
	public enum SceneType: long
	{
		// 已删除的成员（ET8.1 原值，位值不重排，便于与上游文档对照）：
		//   Http = 1 << 5        服务端 HTTP 服务器，随 Router「软路由」一起废弃
		//   Router = 1 << 8      软路由转发节点
		//   RouterManager = 1 << 9   软路由取址服务
		//   BenchmarkClient = 1 << 11 / BenchmarkServer = 1 << 12   压测模块
		//   Watcher = 1 << 19    注意与 AppType.Watcher 无关，后者仍在用
		//   DemoView = 1L << 34  ET8.1 的 View 层配对标记，本工程从未使用

		None = 0,
		Main = 1, // 主纤程,一个进程一个, 初始化从这里开始
		NetInner = 1 << 2, // 负责进程间消息通信
		Realm = 1 << 3,
		Gate = 1 << 4, // 同时兼任 Map，一张 Gate 就是一张地图

		// 客户端
		Demo = 1 << 30,
		Current = 1L << 31,
		NetClient = 1L << 35,

		All = long.MaxValue,
	}

	public static class SceneTypeHelper
	{
		public static bool HasSameFlag(this SceneType a, SceneType b)
		{
			if (((ulong) a & (ulong) b) == 0)
			{
				return false;
			}
			return true;
		}
	}
}