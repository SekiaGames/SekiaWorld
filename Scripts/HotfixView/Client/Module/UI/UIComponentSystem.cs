using System.Collections.Generic;

namespace ET.Client
{
	[EntitySystemOf(typeof(UIComponent))]
	[FriendOf(typeof(UIComponent))]
	public static partial class UIComponentSystem
	{
		[EntitySystem]
		private static void Awake(this UIComponent self)
		{
			self.UIGlobalComponent = self.Root().GetComponent<UIGlobalComponent>();
		}

		public static async ETTask<UI> Create(this UIComponent self, string uiType, UILayer uiLayer)
		{
			EntityRef<UIComponent> selfRef = self;
			UI ui = await self.UIGlobalComponent.OnCreate(self, uiType, uiLayer);
			self = selfRef;
			if (self == null)
			{
				return ui;
			}

			self.UIs.Add(uiType, ui);
			return ui;
		}

		public static void Remove(this UIComponent self, string uiType)
		{
			if (!self.UIs.TryGetValue(uiType, out EntityRef<UI> uiRef))
			{
				return;
			}

			self.UIGlobalComponent.OnRemove(self, uiType);

			self.UIs.Remove(uiType);
			UI ui = uiRef;
			ui?.Dispose();
		}

		public static UI Get(this UIComponent self, string name)
		{
			self.UIs.TryGetValue(name, out EntityRef<UI> uiRef);
			return uiRef;
		}
	}
}
