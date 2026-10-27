using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 闲时偶尔找一块露天位置看云。</summary>
	public class JobGiver_MkVCloudwatch : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Drafted || !pawn.Spawned || pawn.Map == null)
			{
				return null;
			}
			if (!Rand.Chance(0.06f) || !JoyUtility.EnjoyableOutsideNow(pawn))
			{
				return null;
			}
			if (pawn.Map.weatherManager.curWeather.rainRate > 0.1f
				|| pawn.Map.weatherManager.curWeather.preventSkygaze
				|| !RCellFinder.TryFindSkygazeCell(pawn.Position, pawn, out IntVec3 cell))
			{
				return null;
			}
			JobDef cloudwatch = DefDatabase<JobDef>.GetNamedSilentFail("CK_MkVCloudwatch");
			return cloudwatch == null ? null : JobMaker.MakeJob(cloudwatch, cell);
		}
	}
}
