using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 医生给卫生值过低的卧床病人/婴儿洗澡（DBH WorkGiver_washPatient 的精简版）。
	/// 只负责找人；具体打水与擦洗流程由 <see cref="JobDriver_WashPatient"/> 完成。
	/// </summary>
	public class WorkGiver_WashPatient : WorkGiver_Scanner
	{
		public const float WashThreshold = 0.3f;

		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.Pawn);

		public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			foreach (Pawn patient in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				Need_Hygiene need = patient.needs?.TryGetNeed<Need_Hygiene>();
				if (need != null && need.CurLevel < WashThreshold)
				{
					yield return patient;
				}
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Pawn patient) || patient == pawn)
			{
				return false;
			}
			Need_Hygiene need = patient.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null || need.CurLevel >= WashThreshold)
			{
				return false;
			}
			if (!ShouldBeWashedBySomeone(patient))
			{
				return false;
			}
			// 卧床（或婴儿床里的婴儿）才需要别人帮忙洗。
			if (!WorkGiver_Tend.GoodLayingStatusForTend(patient, pawn)
				&& patient.ageTracker.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
			{
				return false;
			}
			if (!pawn.CanReserve(patient, 1, -1, null, forced))
			{
				return false;
			}
			return TryGiveWashJob(pawn, patient) != null;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return t is Pawn patient ? TryGiveWashJob(pawn, patient) : null;
		}

		/// <summary>给病人创建一个洗澡 Job；找不到水源时返回 null。</summary>
		public static Job TryGiveWashJob(Pawn pawn, Pawn patient)
		{
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_WashPatient");
			if (def == null)
			{
				return null;
			}
			LocalTargetInfo source = WaterSourceUtility.FindFillSource(pawn, patient.Position);
			if (!source.IsValid)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(def, patient);
			job.targetB = source;
			job.count = 1;
			return job;
		}

		public static bool ShouldBeWashedBySomeone(Pawn pawn)
		{
			return ShouldBeWashed(pawn) || PrisonerShouldBeWashed(pawn);
		}

		public static bool PrisonerShouldBeWashed(Pawn p)
		{
			if (p.IsPrisonerOfColony && p.InBed() && p.guest != null && p.guest.CanBeBroughtFood)
			{
				return HealthAIUtility.ShouldSeekMedicalRest(p);
			}
			return false;
		}

		public static bool ShouldBeWashed(Pawn p)
		{
			if (p == null || p.Dead || !p.Spawned || p.GetPosture() == PawnPosture.Standing)
			{
				return false;
			}
			bool isBaby = p.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby;
			if (!p.RaceProps.Humanlike)
			{
				Building_Bed bed = p.CurrentBed();
				if (bed == null || bed.Faction != Faction.OfPlayer)
				{
					return false;
				}
			}
			else
			{
				if (p.Faction != Faction.OfPlayer && p.HostFaction != Faction.OfPlayer)
				{
					return false;
				}
				if (!p.InBed() && !isBaby)
				{
					return false;
				}
			}
			if (p.Map.designationManager.DesignationOn(p, DesignationDefOf.Slaughter) != null)
			{
				return false;
			}
			if (!isBaby && !HealthAIUtility.ShouldSeekMedicalRest(p))
			{
				return false;
			}
			if (p.HostFaction != null)
			{
				if (p.HostFaction != Faction.OfPlayer)
				{
					return false;
				}
				if (p.guest != null && !p.guest.CanBeBroughtFood)
				{
					return false;
				}
			}
			return true;
		}
	}
}
