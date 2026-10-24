using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 模板底部「研究树」按钮的 Worker：点击直接打开 <see cref="TechTreeWindow"/>。
	/// </summary>
	public sealed class MainButtonWorker_TechTree : MainButtonWorker
	{
		public override void Activate()
		{
			Find.WindowStack.Add(new TechTreeWindow());
		}
	}
}
