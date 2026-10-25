using HarmonyLib;
using RimWorld;
using Verse;

namespace rjw
{
	///<summary>
	///RJW Designators 检查/更新
	///只为选中的小人更新一次 designator，而不是每 tick（每秒 60 次）都更新
	///</summary>
	[HarmonyPatch(typeof(Selector), nameof(Selector.Select))]
	[StaticConstructorOnStartup]
	static class PawnSelect
	{
		[HarmonyPrefix]
		private static bool UpdateDesignatorPermissions(object obj)
		{
			if (obj is Pawn pawn && pawn.HasComp<CompRJW>())
			{
				//ModLog.Message("Selector patch");
				//ModLog.Message("pawn: " + xxx.get_pawnname(pawn));
				pawn.UpdatePermissions();
			}
			return true;
		}
	}
}