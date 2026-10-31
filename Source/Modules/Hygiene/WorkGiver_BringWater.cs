using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 送水工作：医生给喝不到水的病人送水，看守给牢房里没水的囚徒送水。
	/// 只负责找人找水，动作流程由 <see cref="JobDriver_BringWater"/> 完成；
	/// 找不到瓶装水时退回「拿水盆/水桶去水源打水、当场喂」。
	/// </summary>
	public class WorkGiver_BringWater : WorkGiver_Scanner
	{
		private bool ForPrisoners => def.defName == "CK_BringWaterPrisoner";

		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.Pawn);

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				yield break;
			}
			foreach (Pawn target in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				if (target != pawn
					&& ForPrisoners == target.IsPrisonerOfColony
					&& BringWaterUtility.IsCandidate(target))
				{
					yield return target;
				}
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Pawn target) || target == pawn)
			{
				return false;
			}
			if (ForPrisoners != target.IsPrisonerOfColony)
			{
				return false;
			}
			// 找水源这一步比较贵，放在便宜的筛选之后，而且只在真的需要送水时才做。
			if (!BringWaterUtility.ShouldDeliverWater(target))
			{
				return false;
			}
			if (target.IsForbidden(pawn)
				|| !pawn.CanReserveAndReach(target, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			return BringWaterUtility.TryMakeJob(pawn, target) != null;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return t is Pawn target ? BringWaterUtility.TryMakeJob(pawn, target) : null;
		}
	}
}
