using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 底部菜单测试按钮：只弹一条演示消息，用来验证 MainButtonService 的显示/顺序能力。
	/// 研究树入口在 vanilla Research 窗口的「示例研究」页签，不在底部菜单。
	/// </summary>
	public sealed class MainButtonWorker_Demo : MainButtonWorker
	{
		public override void Activate()
		{
			Find.WindowStack.Add(new Dialog_MessageBox("ModTemplate.DemoButton.Message".Loc()));
		}
	}
}
