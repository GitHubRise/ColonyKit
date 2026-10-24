using RimWorld;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 模板底部「科技树」按钮的 Worker：点击直接打开 <see cref="TechTreeWindow"/>。
	/// </summary>
	public sealed class MainButtonWorker_TechTree : MainButtonWorker
	{
		public override void Activate()
		{
			Find.WindowStack.Add(new TechTreeWindow());
		}
	}
}
