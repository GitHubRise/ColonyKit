using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 育儿工作：给卫生值过低的婴儿洗澡。Job 与洗澡流程直接复用
	/// <see cref="WorkGiver_WashPatient"/> / <see cref="JobDriver_WashPatient"/>。
	/// </summary>
	public class WorkGiver_WashChild : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.Pawn);

		public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			foreach (Pawn child in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				if (child.ageTracker.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
				{
					continue;
				}
				Need_Hygiene need = child.needs?.TryGetNeed<Need_Hygiene>();
				if (need != null && need.CurLevel < WorkGiver_WashPatient.WashThreshold)
				{
					yield return child;
				}
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Pawn child) || child == pawn)
			{
				return false;
			}
			if (child.ageTracker.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
			{
				return false;
			}
			Need_Hygiene need = child.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null || need.CurLevel >= WorkGiver_WashPatient.WashThreshold)
			{
				return false;
			}
			if (!WorkGiver_WashPatient.ShouldBeWashedBySomeone(child))
			{
				return false;
			}
			if (!pawn.CanReserve(child, 1, -1, null, forced))
			{
				return false;
			}
			return WorkGiver_WashPatient.TryGiveWashJob(pawn, child) != null;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return t is Pawn child ? WorkGiver_WashPatient.TryGiveWashJob(pawn, child) : null;
		}
	}
}
