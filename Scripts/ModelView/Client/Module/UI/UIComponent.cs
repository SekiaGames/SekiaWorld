using System.Collections.Generic;

namespace ET.Client
{
	[ComponentOf]
	public class UIComponent: Entity, IAwake
	{
		public Dictionary<string, EntityRef<UI>> UIs = new();

		private EntityRef<UIGlobalComponent> uiGlobalComponent;

		public UIGlobalComponent UIGlobalComponent
		{
			get
			{
				return this.uiGlobalComponent;
			}
			set
			{
				this.uiGlobalComponent = value;
			}
		}
	}
}
