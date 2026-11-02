using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「去喝水」的 JobGiver（DBH JobGiver_DrinkWater 的移植）：
	/// 口渴低于 30% 才派活；快渴死时（如已脱水）用冲刺、把搜索范围放到全图。
	/// 顺序是「附近的活水 → 自己的水袋 → 自己带的瓶装水/水桶」：
	/// 地图上有河、湖、水龙头、取水点、水盆、地上的瓶装水/水桶时先走过去喝；
	/// 附近真没水（野外、牢房、干旱图）才拿自己水袋里的水。
	/// 动物另外加一点随机抖动，避免同一群动物同时口渴、同时挤到同一个水边。
	/// </summary>
	public class JobGiver_DrinkWater : ThinkNode_JobGiver
	{
		public virtual float MinLevel => 0.3f;

		/// <summary>动物的口渴阈值在这个范围内随机，错开群体同时找水的时间。</summary>
		private const float AnimalJitterMin = 0.75f;

		private const float AnimalJitterMax = 1.25f;

		public override float GetPriority(Pawn pawn)
		{
			Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null || FoodUtility.ShouldBeFedBySomeone(pawn))
			{
				return 0f;
			}
			if (thirst.CurLevel > ThresholdFor(pawn))
			{
				return 0f;
			}
			return 9.6f;
		}

		/// <summary>人形按设置阈值；动物用编号做随机相位，让同一群动物错开找水的时间。</summary>
		private float ThresholdFor(Pawn pawn)
		{
			if (!pawn.RaceProps.Animal)
			{
				return MinLevel;
			}
			float jitter = Rand.ValueSeeded(pawn.thingIDNumber ^ 0x5A17);
			return MinLevel * Mathf.Lerp(AnimalJitterMin, AnimalJitterMax, jitter);
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
			bool hostile = pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);

			// 1) 附近有能喝的水就先过去喝：地表水、水盆/水槽、水龙头/取水点，
			//    非敌对的人形还会捡地上的瓶装水和桶/盆里的水（敌人只认地表水）。
			LocalTargetInfo target = FindDrinkTarget(pawn, urgent, range, allowGroundItems: !hostile);
			Job waterJob = MakeDrinkJob(target, urgency);
			if (waterJob != null)
			{
				return waterJob;
			}

			// 2) 附近没有水：拿自己水袋里的水喝（30 L 能顶好几天）。
			if (!pawn.RaceProps.Animal && WaterPouchUtility.GetWater(pawn) >= 0.1f)
			{
				JobDef pouchDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromPouch");
				if (pouchDef != null)
				{
					return JobMaker.MakeJob(pouchDef);
				}
			}

			// 3) 自己物品栏里的瓶装水/水桶（囚徒收到的瓶子、随身水桶）。
			Thing bottle = MineralWaterUtility.Carried(pawn);
			if (bottle != null)
			{
				return MakeBottleJob(bottle, urgency);
			}
			if (!pawn.RaceProps.Animal && WaterContainerUtility.CarriedContainer(pawn, requireWater: true) != null)
			{
				JobDef carriedDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromCarried");
				if (carriedDef != null)
				{
					Job job = JobMaker.MakeJob(carriedDef);
					job.locomotionUrgency = urgency;
					return job;
				}
			}
			return null;
		}

		/// <summary>
		/// 找能喝的水：设施/地表水（敌人只认地表水）和地上的瓶装水/水桶一起比距离，
		/// 谁近去谁那儿，远的那类不白跑。
		/// </summary>
		private static LocalTargetInfo FindDrinkTarget(
			Pawn pawn, bool urgent, float range, bool allowGroundItems)
		{
			LocalTargetInfo target = ClosestSanitation.FindBestDrink(
				pawn, urgent, range, allowColonyFixtures: !IsHostile(pawn));
			if (!allowGroundItems)
			{
				return target;
			}
			float best = target.IsValid
				? DistanceSquared(pawn, target)
				: float.MaxValue;
			Thing bottle = MineralWaterUtility.FindSpawned(pawn, range);
			if (bottle != null && bottle.Position.DistanceToSquared(pawn.Position) < best)
			{
				best = bottle.Position.DistanceToSquared(pawn.Position);
				target = new LocalTargetInfo(bottle);
			}
			Thing container = WaterContainerUtility.FindSpawnedContainer(
				pawn, requireWater: true, maxDistance: range);
			if (container != null && container.Position.DistanceToSquared(pawn.Position) < best)
			{
				target = new LocalTargetInfo(container);
			}
			return target;
		}

		private static float DistanceSquared(Pawn pawn, LocalTargetInfo target)
		{
			IntVec3 cell = target.HasThing ? target.Thing.Position : target.Cell;
			return cell.DistanceToSquared(pawn.Position);
		}

		private static bool IsHostile(Pawn pawn)
		{
			return pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
		}

		private static Job MakeDrinkJob(LocalTargetInfo target, LocomotionUrgency urgency)
		{
			if (!target.IsValid)
			{
				return null;
			}
			Job job = null;
			Thing thing = target.Thing;
			if (thing != null && MineralWaterUtility.IsBottledWater(thing))
			{
				return MakeBottleJob(thing, urgency);
			}
			if (thing != null && thing.TryGetComp<CompBasinWater>() != null)
			{
				JobDef carriedDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromCarried");
				if (carriedDef != null)
				{
					job = JobMaker.MakeJob(carriedDef, thing);
				}
			}
			else if (thing != null)
			{
				JobDef basinDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromBasin");
				if (basinDef != null)
				{
					job = JobMaker.MakeJob(basinDef, thing);
				}
			}
			else
			{
				JobDef groundDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromGround");
				if (groundDef != null)
				{
					job = JobMaker.MakeJob(groundDef, target.Cell);
				}
			}
			if (job != null)
			{
				job.locomotionUrgency = urgency;
			}
			return job;
		}

		private static Job MakeBottleJob(Thing bottle, LocomotionUrgency urgency)
		{
			Job job = JobMaker.MakeJob(JobDefOf.Ingest, bottle);
			// JobMaker 默认把 count 留成 -1，而原版 Toils_Misc.TakeItemFromInventoryToCarrier
			// 取的是 Mathf.Min(stackCount, count)，-1 等于一件也不拿、targetA 随即被清空，
			// 下一个 toil 的全局失效检查就读到 null 抛空引用（整局 Update 刷红字、小人卡死）。
			// 明确「喝 1 瓶」。
			job.count = 1;
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
