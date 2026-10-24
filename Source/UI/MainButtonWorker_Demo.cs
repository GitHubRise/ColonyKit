using System.Linq;
using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 底部菜单测试按钮：打开贴着底栏的抽屉。再点一次关闭；不暂停、不阻止地图拖动。
	/// 研究树入口仍在原版 Research 窗口的「示例研究」页签，不在这个抽屉里。
	/// </summary>
	public sealed class MainButtonWorker_Demo : MainButtonWorker
	{
		public override void Activate()
		{
			DemoDrawerWindow open = Find.WindowStack.Windows
				.OfType<DemoDrawerWindow>()
				.FirstOrDefault();
			if (open != null)
			{
				open.Close();
				return;
			}

			Find.WindowStack.Add(new DemoDrawerWindow());
		}
	}
}
