using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>卫生值低时自动寻找最近淋浴器。</summary>
	public class JobGiver_HaveWash : ThinkNode_JobGiver
	{
		public override float GetPriority(Pawn pawn)
		{
			// 模块关闭或检测到 DBH 时，即使旧存档里还留着 CK_Hygiene，
			// 也不应该继续自动派洗澡工作。
			if (!HygieneSettings.Current.ShouldRunHygiene)
			{
				return 0f;
			}
			Need_Hygiene need = pawn.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null || FoodUtility.ShouldBeFedBySomeone(pawn))
			{
				return 0f;
			}

			float threshold = 0.2f;
			// 优先本包的 CK_Hygienic，没定义时退回 DBH 的 Hygienic。
			TraitDef hygienic = DefDatabase<TraitDef>.GetNamedSilentFail("CK_Hygienic")
				?? DefDatabase<TraitDef>.GetNamedSilentFail("Hygienic");
			if (hygienic != null)
			{
				switch (pawn.story?.traits?.DegreeOfTrait(hygienic) ?? 0)
				{
					case 2: threshold = 0.4f; break;
					case 1: threshold = 0.3f; break;
					case 0: threshold = 0.2f; break;
					case -1: threshold = 0.1f; break;
					case -2: threshold = 0f; break;
				}
			}

			if (pawn.mindState?.IsIdle == true)
			{
				threshold = 0.5f;
			}
			Need_Rest rest = pawn.needs?.TryGetNeed<Need_Rest>();
			if (rest != null && rest.CurLevel > 0.95f)
			{
				threshold = 0.6f;
			}
			return need.CurLevel < threshold ? 9.25f : 0f;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (GetPriority(pawn) <= 0f || pawn.Map == null)
			{
				return null;
			}

			Building_Shower best = null;
			float bestDistance = float.MaxValue;
			foreach (Building_Shower shower in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Shower>())
			{
				if (!shower.CanUse(pawn) || shower.IsForbidden(pawn))
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(shower.Position);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = shower;
				}
			}

			if (best == null)
			{
				Job bathJob = TryGiveBathJob(pawn);
				if (bathJob != null)
				{
					return bathJob;
				}
			}

			if (best == null)
			{
				// 没有空位但有人在洗：去门口排队，占用者结束后由等待 Job 接手。
				Building_Shower busy = FindBusyShower(pawn);
				if (busy != null)
				{
					JobDef waitDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_WaitForShower");
					return waitDef == null ? null : JobMaker.MakeJob(waitDef, busy);
				}
				return null;
			}
			JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_TakeShower");
			return jobDef == null ? null : JobMaker.MakeJob(jobDef, best);
		}

		/// <summary>没有空闲淋浴时找浴缸：优先双人浴缸，并按设置概率邀请一名异性。</summary>
		private static Job TryGiveBathJob(Pawn pawn)
		{
			Building_DoubleBath bestDouble = null;
			float bestDoubleDistance = float.MaxValue;
			Building_Bath bestSingle = null;
			float bestSingleDistance = float.MaxValue;
			foreach (Building_Bath bath in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Bath>())
			{
				if (!bath.CanUse(pawn) || bath.IsForbidden(pawn))
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(bath.Position);
				if (bath is Building_DoubleBath doubleBath)
				{
					if (distance < bestDoubleDistance)
					{
						bestDoubleDistance = distance;
						bestDouble = doubleBath;
					}
				}
				else if (distance < bestSingleDistance)
				{
					bestSingleDistance = distance;
					bestSingle = bath;
				}
			}

			if (bestDouble != null && Building_DoubleBath.TogetherJobDef != null)
			{
				Job job = JobMaker.MakeJob(Building_DoubleBath.TogetherJobDef, bestDouble);
				TryInvitePartner(pawn, bestDouble, job);
				return job;
			}
			if (bestSingle != null)
			{
				JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_TakeBath");
				return def == null ? null : JobMaker.MakeJob(def, bestSingle);
			}
			return null;
		}

		/// <summary>按设置里的概率决定要不要邀请；概率为 0 时完全不掷骰。</summary>
		private static void TryInvitePartner(Pawn pawn, Building_DoubleBath bath, Job job)
		{
			float chance = HygieneSettings.Current.doubleBathInviteChance;
			if (chance <= 0f || !Rand.Chance(chance))
			{
				return;
			}
			Pawn partner = Building_DoubleBath.FindPartner(pawn, bath);
			if (partner != null)
			{
				job.targetB = partner;
			}
		}

		private static Building_Shower FindBusyShower(Pawn pawn)
		{
			Building_Shower best = null;
			float bestDistance = float.MaxValue;
			foreach (Building_Shower shower in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Shower>())
			{
				bool reserved = shower.Map.reservationManager.IsReserved(shower);
				if ((shower.occupiedSinceTick < 0 && !reserved)
					|| shower.IsForbidden(pawn)
					|| !shower.HasWaterSupply
					|| !shower.Working().Accepted)
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(shower.Position);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = shower;
				}
			}
			return best;
		}
	}
}
