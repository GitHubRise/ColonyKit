using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「渴得不行又找不到水」：只对非殖民地人形生效（访客、商队、敌人）。
	/// 敌人只认地表水，访客/商队可以用殖民地水源；水袋里还有水也不算。
	/// 命中后由 LordDuty 里的 JobGiver_ExitMapBest 让他们提前撤离，而不是原地渴死。
	/// </summary>
	public class ThinkNode_ConditionalThirstyNoWater : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			if (!HygieneSettings.Current.ShouldRunHygiene || pawn?.needs == null)
			{
				return false;
			}
			Need_Thirst thirst = pawn.needs.TryGetNeed<Need_Thirst>();
			if (thirst == null || thirst.CurLevel > 0.08f)
			{
				return false;
			}
			if (pawn.Downed
				|| pawn.Deathresting
				|| pawn.IsColonist
				|| pawn.IsSlave
				|| pawn.IsPrisonerOfColony
				|| pawn.GetCaravan() != null)
			{
				return false;
			}
			if (WaterPouchUtility.GetWater(pawn) >= 0.1f)
			{
				return false;
			}
			bool hostile = pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
			return !ClosestSanitation.FindBestDrink(
				pawn,
				urgent: true,
				range: 60f,
				allowColonyFixtures: !hostile).IsValid;
		}
	}
}
