using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「去喝水」的 JobGiver（DBH JobGiver_DrinkWater 的移植）：
	/// 口渴低于 30% 才派活；快渴死时（如已脱水）用冲刺、把搜索范围放到全图。
	/// </summary>
	public class JobGiver_DrinkWater : ThinkNode_JobGiver
	{
		public virtual float MinLevel => 0.3f;

		public override float GetPriority(Pawn pawn)
		{
			Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null || FoodUtility.ShouldBeFedBySomeone(pawn))
			{
				return 0f;
			}
			if (thirst.CurLevel > MinLevel)
			{
				return 0f;
			}
			return 9.6f;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null || GetPriority(pawn) <= 0f)
			{
				return null;
			}
			bool urgent = thirst.CurLevel <= 0f || pawn.health.hediffSet.HasHediff(HediffDefOf.Heatstroke);
			LocomotionUrgency urgency = urgent ? LocomotionUrgency.Sprint : LocomotionUrgency.Jog;
			float range = 20f;
			if (thirst.CurLevel <= 0.3f)
			{
				range = 30f;
			}
			if (thirst.CurLevel <= 0.2f)
			{
				range = 40f;
			}
			if (thirst.CurLevel <= 0.1f)
			{
				range = 9999f;
			}
			LocalTargetInfo target = ClosestSanitation.FindBestDrink(pawn, urgent, range);
			if (!target.IsValid)
			{
				return null;
			}
			Job job;
			if (target.HasThing)
			{
				JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromBasin");
				if (def == null)
				{
					return null;
				}
				job = JobMaker.MakeJob(def, target.Thing);
			}
			else
			{
				JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromGround");
				if (def == null)
				{
					return null;
				}
				job = JobMaker.MakeJob(def, target.Cell);
			}
			job.locomotionUrgency = urgency;
			return job;
		}
	}

	/// <summary>快渴死时也允许喝（DBH JobGiver_DrinkWaterUrgent）。</summary>
	public class JobGiver_DrinkWaterUrgent : JobGiver_DrinkWater
	{
		public override float MinLevel => 0f;
	}
}
