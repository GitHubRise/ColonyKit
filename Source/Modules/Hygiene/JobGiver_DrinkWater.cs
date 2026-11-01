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
			// 随身水袋优先：30 L 能顶好几天，不用为每一口跑回水龙头。
			if (!pawn.RaceProps.Animal && WaterPouchUtility.GetWater(pawn) >= 0.1f)
			{
				JobDef pouchDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromPouch");
				if (pouchDef != null)
				{
					return JobMaker.MakeJob(pouchDef);
				}
			}

			// 瓶装水：病人/囚徒收到的瓶子谁都能喝；从地上/仓库找瓶子只给殖民者，
			// 避免访客/敌人顺手拿走殖民地库存。
			bool colonistLike = pawn.IsColonist || pawn.IsSlave;
			Thing bottle = MineralWaterUtility.Carried(pawn);
			if (bottle == null && colonistLike && !pawn.RaceProps.Animal)
			{
				bottle = MineralWaterUtility.FindSpawned(pawn);
			}
			if (bottle != null)
			{
				Job bottleJob = JobMaker.MakeJob(JobDefOf.Ingest, bottle);
				// JobMaker 默认把 count 留成 -1，而原版 Toils_Misc.TakeItemFromInventoryToCarrier
				// 取的是 Mathf.Min(stackCount, count)，-1 等于一件也不拿、targetA 随即被清空，
				// 下一个 toil 的全局失效检查就读到 null 抛空引用（整局 Update 刷红字、小人卡死）。
				// 明确「喝 1 瓶」。
				bottleJob.count = 1;
				return bottleJob;
			}
			// 随身带着水桶/脸盆时先喝自己的水，省得跑回洗手池。
			if (!pawn.RaceProps.Animal && WaterContainerUtility.CarriedContainer(pawn, requireWater: true) != null)
			{
				JobDef carriedDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromCarried");
				if (carriedDef != null)
				{
					return JobMaker.MakeJob(carriedDef);
				}
			}
			// 敌人不许喝殖民地的水龙头/洗手池/水槽，只找地表水；
			// 访客、商队、囚徒和殖民者可以用殖民地设施。
			bool hostile = pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
			LocalTargetInfo target = ClosestSanitation.FindBestDrink(pawn, urgent, range, allowColonyFixtures: !hostile);
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
