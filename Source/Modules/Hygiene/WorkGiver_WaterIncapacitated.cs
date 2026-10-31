using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 给无法自己行动的病人 / 囚徒喂水：医生或看守拿一个装水的脸盆/水桶过去，
	/// 盆里没水就先去取水点/水龙头/水井/水箱装水。病人不需要起身。
	/// </summary>
	public class WorkGiver_WaterIncapacitated : WorkGiver_Scanner
	{
		private bool ForPrisoners => def.defName == "CK_WaterPrisoner";

		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.Pawn);

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Pawn patient) || patient == pawn)
			{
				return false;
			}
			if (ForPrisoners != patient.IsPrisonerOfColony)
			{
				return false;
			}
			if (!patient.Downed && !FoodUtility.ShouldBeFedBySomeone(patient))
			{
				return false;
			}
			Need_Thirst thirst = patient.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null || thirst.CurLevel >= 0.55f)
			{
				return false;
			}
			if (patient.IsForbidden(pawn)
				|| !pawn.CanReserveAndReach(patient, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			return TryFindContainer(pawn, patient, out _, out _);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return t is Pawn patient ? TryMakeFeedJob(pawn, patient) : null;
		}

		/// <summary>
		/// 造一个「拿容器打水 → 床边喂水」的 Job；右键菜单和送水的兜底路径共用。
		/// 找不到容器或水源时返回 null。
		/// </summary>
		internal static Job TryMakeFeedJob(Pawn pawn, Pawn patient)
		{
			if (pawn == null || patient == null || pawn == patient)
			{
				return null;
			}
			if (!TryFindContainer(pawn, patient, out CompBasinWater container, out LocalTargetInfo source))
			{
				return null;
			}
			JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_WaterPatient");
			if (jobDef == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(jobDef, patient);
			if (container.parent.Spawned)
			{
				job.targetB = container.parent;
			}
			if (source.IsValid)
			{
				job.targetC = source;
			}
			return job;
		}

		private static bool TryFindContainer(
			Pawn pawn, Pawn patient, out CompBasinWater container, out LocalTargetInfo source)
		{
			container = WaterContainerUtility.CarriedContainer(pawn, requireWater: true)
				?? WaterContainerUtility.CarriedContainer(pawn, requireWater: false);
			if (container == null)
			{
				Thing thing = WaterContainerUtility.FindSpawnedContainer(pawn, requireWater: true)
					?? WaterContainerUtility.FindSpawnedContainer(pawn, requireWater: false);
				container = thing?.TryGetComp<CompBasinWater>();
			}
			source = LocalTargetInfo.Invalid;
			if (container == null)
			{
				return false;
			}
			if (container.Water < 5f)
			{
				source = WaterSourceUtility.FindFillSource(pawn, patient.Position);
				if (!source.IsValid && container.Water <= 0.1f)
				{
					return false;
				}
			}
			return true;
		}
	}
}
