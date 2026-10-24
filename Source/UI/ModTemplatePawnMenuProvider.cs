using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 右键小人菜单的 Provider：原版在 <c>FloatMenuMakerMap.Init()</c> 里自动发现所有
	/// <see cref="FloatMenuOptionProvider"/> 子类，因此不需要 Harmony 补丁。
	/// </summary>
	/// <remarks>
	/// 只接管「点在小人身上」的菜单（<see cref="GetOptionsFor(Pawn, FloatMenuContext)"/>），
	/// 地面/物品的右键菜单不受影响。
	/// </remarks>
	public sealed class ModTemplatePawnMenuProvider : FloatMenuOptionProvider
	{
		// 征召/未征召都显示；单选；允许目标就是当前选中的人（右键自己）。
		protected override bool Drafted => true;

		protected override bool Undrafted => true;

		protected override bool Multiselect => false;

		protected override bool CanSelfTarget => true;

		public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
		{
			return PawnMenuService.GetOptions(clickedPawn);
		}
	}
}
