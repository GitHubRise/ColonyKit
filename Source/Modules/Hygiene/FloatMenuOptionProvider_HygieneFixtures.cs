using System.Collections.Generic;
using System.Linq;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 选中殖民者后右键洁具/水井时的指令菜单：
	/// 马桶→使用，洗手池→洗手，淋浴器→淋浴，浴缸/双人浴缸→泡澡，水井→打水；
	/// 另有清理堵塞（CompBlockage）和清洁附近污渍。
	/// </summary>
	public class FloatMenuOptionProvider_HygieneFixtures : FloatMenuOptionProvider
	{
		protected override bool Drafted => false;

		protected override bool Undrafted => true;

		protected override bool Multiselect => false;

		protected override bool RequiresManipulation => true;

		public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
		{
			Pawn pawn = context.FirstSelectedPawn;
			if (pawn == null
				|| !pawn.IsColonistPlayerControlled
				|| !(clickedThing is Building building)
				|| building.Map != pawn.Map
				|| building.Faction != Faction.OfPlayer)
			{
				yield break;
			}

			// 使用类：各设备自己的 CanUse 已经包含供水、堵塞、占用、可达与预约检查。
			if (clickedThing is Building_Toilet toilet)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.UseToilet", pawn, toilet, "CK_UseToilet",
					toilet.CanUse(pawn), RejectReason(toilet));
			}
			else if (clickedThing is Building_Basin basin)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.WashHands", pawn, basin, "CK_WashHands",
					basin.CanUse(pawn), RejectReason(basin));

				// 摆放式水盆：随身带的脸盆里有水时，可以倒进去补水（最多补满 30 次）。
				if (clickedThing is Building_WaterTub tub)
				{
					CompBasinWater filled = JobDriver_FillBasin.CarriedBasinWithWater(pawn);
					if (filled != null)
					{
						bool full = tub.usesRemaining >= Building_WaterTub.MaxUses;
						bool canPour = !full && pawn.CanReserveAndReach(tub, PathEndMode.Touch, pawn.NormalMaxDanger());
						yield return UseOption("CK.Hygiene.FloatMenu.FillTub", pawn, tub, "CK_FillTub",
							canPour,
							full
								? "CK.Hygiene.TubFull".Translate().ToString()
								: "CannotReach".Translate().ToString());
					}
				}
			}
			else if (clickedThing is Building_Shower shower)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.Shower", pawn, shower, "CK_TakeShower",
					shower.CanUse(pawn), RejectReason(shower));
			}
			else if (clickedThing is Building_Bath bath)
			{
				// 双人浴缸也用同一个泡澡 Job；想邀请别人由 JobGiver_HaveWash 自动掷骰。
				yield return UseOption("CK.Hygiene.FloatMenu.Bath", pawn, bath, "CK_TakeBath",
					bath.CanUse(pawn), RejectReason(bath));
			}

			// 清理堵塞：复用机器人「疏通洁具」Job（JobDriver_NannyUnblock 只依赖 CompBlockage）。
			CompBlockage blockage = building.TryGetComp<CompBlockage>();
			if (blockage != null && blockage.blocked)
			{
				bool canUnblock = pawn.CanReserveAndReach(building, PathEndMode.Touch, pawn.NormalMaxDanger());
				yield return UseOption("CK.Hygiene.FloatMenu.Unblock", pawn, building, "CK_NannyUnblock",
					canUnblock, canUnblock ? null : "CannotReach".Translate().ToString());
			}

			// 清洁：洁具周围一格内最近的污渍，走原版 Clean Job。
			Filth filth = FindFilth(building);
			if (filth != null && pawn.CanReserveAndReach(filth, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				yield return new FloatMenuOption(
					"CK.Hygiene.FloatMenu.Clean".Translate(),
					() =>
					{
						Job job = JobMaker.MakeJob(JobDefOf.Clean);
						job.AddQueuedTarget(TargetIndex.A, filth);
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					},
					MenuOptionPriority.Default,
					null,
					filth);
			}

			// 水龙头/免费取水点：可以直接喝水（洗手走上面的 Building_Basin 分支）。
			if (building is Building_WaterPoint waterPoint)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.Drink", pawn, waterPoint, "CK_DrinkFromBasin",
					waterPoint.CanUse(pawn), RejectReason(waterPoint));

				// 带净水器的水龙头：直接装一瓶 1 L 纯净水，不会中毒。
				bool canPurified = waterPoint.CanBottlePurifiedWater(pawn);
				yield return UseOption(
					"CK.Hygiene.FloatMenu.FillPurified", pawn, waterPoint, "CK_FillPurifiedWater",
					canPurified,
					canPurified ? null : "CK.Hygiene.FloatMenu.NeedsFilter".Translate().ToString());
			}
			else if (building is Building_WaterSpot waterSpot)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.Drink", pawn, waterSpot, "CK_DrinkFromBasin",
					waterSpot.CanUse(pawn), RejectReason(waterSpot));

				// 直接打水：拿一只空桶到取水点打满，不依赖清单/WorkGiver。
				Thing spotBucket = WaterContainerUtility.FindBucketForJob(pawn);
				bool spotReachable = pawn.CanReserveAndReach(
					waterSpot, PathEndMode.Touch, pawn.NormalMaxDanger());
				bool bucketReachable = spotBucket == null
					|| !spotBucket.Spawned
					|| pawn.CanReserveAndReach(
						spotBucket, PathEndMode.ClosestTouch, pawn.NormalMaxDanger());
				string drawReason = !spotReachable
					? "CannotReach".Translate().ToString()
					: spotBucket == null
						? "CK.Hygiene.FloatMenu.NeedBucket".Translate().ToString()
						: bucketReachable
							? null
							: "CannotReach".Translate().ToString();
				Thing bucketTarget = spotBucket != null && spotBucket.Spawned ? spotBucket : null;
				yield return UseOption("CK.Hygiene.FloatMenu.DrawFromSpot", pawn, waterSpot, "CK_FillSpotBucket",
					spotReachable && spotBucket != null && bucketReachable, drawReason, bucketTarget);

				// 脸盆：不占用清单任务，直接拿盆到取水点打水。
				CompBasinWater carriedBasin = WaterContainerUtility.CarriedBasin(pawn);
				Thing spareBasin = carriedBasin == null
					? WaterContainerUtility.FindSpawnedBasin(pawn)
					: null;
				CompBasinWater basin = carriedBasin ?? spareBasin?.TryGetComp<CompBasinWater>();
				bool basinReachable = spareBasin == null
					|| pawn.CanReserveAndReach(
						spareBasin, PathEndMode.ClosestTouch, pawn.NormalMaxDanger());
				bool canFillBasin = spotReachable && basinReachable;
				if (basin != null)
				{
					yield return UseOption("CK.Hygiene.FloatMenu.FillBasin", pawn, waterSpot, "CK_FillBasin",
						canFillBasin,
						canFillBasin ? null : "CannotReach".Translate().ToString(),
						spareBasin);
				}
				else if (spotBucket == null)
				{
					yield return UseOption("CK.Hygiene.FloatMenu.FillBasin", pawn, waterSpot, "CK_FillBasin",
						false,
						"CK.Hygiene.FloatMenu.NeedContainer".Translate().ToString());
				}

				// 不需要任何容器：直接装一瓶 1 L 矿泉水。
				yield return UseOption(
					"CK.Hygiene.FloatMenu.FillBottle", pawn, waterSpot, "CK_FillWaterBottle",
					spotReachable,
					spotReachable ? null : "CannotReach".Translate().ToString());
			}

			// 打水：水井（地下水）、水箱（同网存水）都能用随身容器接。
			// 取水点在上面单独处理：水桶走清单 Job，脸盆走 CK_FillBasin，避免重复菜单项。
			bool isWell = building.TryGetComp<CompWaterInlet>() != null;
			bool isStorage = building.TryGetComp<CompWaterStorage>() != null;
			if (!(building is Building_WaterSpot) && (isWell || isStorage || building is Building_WaterPoint))
			{
				CompPipe pipe = building.GetComp<CompPipe>();
				PlumbingNet net = pipe?.pipeNet;
				bool reachable = pawn.CanReserveAndReach(building, PathEndMode.Touch, pawn.NormalMaxDanger());
				bool sourceHasWater = isWell || (net != null && net.WaterStorage > 1f);

				if (isWell && !(building is Building_WaterSpot))
				{
					// 保留原「打水」：从水井往同网水箱手动加水。
					bool canDraw = reachable
						&& net != null
						&& net.WaterTowers.Any(t => t != null && t.WorkingNow && t.space > 1f);
					yield return UseOption("CK.Hygiene.FloatMenu.DrawWater", pawn, building, "CK_DrawWater",
						canDraw,
						canDraw ? null : "CK.Hygiene.FloatMenu.NoWaterTank".Translate().ToString());
				}

				// 容器：物品栏里带着就直接装；没带就自动去地上找一只（不占武器位、不装备）。
				CompBasinWater carried = WaterContainerUtility.CarriedContainer(pawn, requireWater: false);
				Thing spare = carried == null ? WaterContainerUtility.FindSpawnedContainer(pawn, requireWater: false) : null;
				CompBasinWater container = carried ?? spare?.TryGetComp<CompBasinWater>();
				if (container != null)
				{
					string fillKey = WaterContainerUtility.IsBucket(container)
						? "CK.Hygiene.FloatMenu.FillBucket"
						: "CK.Hygiene.FloatMenu.FillBasin";
					bool canFill = reachable && sourceHasWater;
					yield return UseOption(fillKey, pawn, building, "CK_FillBasin",
						canFill, canFill ? null : FillReason(reachable, sourceHasWater), spare);
				}
				else
				{
					CompBasinWater fullCarried = WaterContainerUtility.CarriedContainerAny(pawn);
					if (fullCarried != null)
					{
						string fullKey = WaterContainerUtility.IsBucket(fullCarried)
							? "CK.Hygiene.FloatMenu.FillBucket"
							: "CK.Hygiene.FloatMenu.FillBasin";
						yield return UseOption(fullKey, pawn, building, "CK_FillBasin", false,
							"CK.Hygiene.FloatMenu.ContainerFull".Translate().ToString());
					}
					else
					{
						yield return UseOption("CK.Hygiene.FloatMenu.FillBasin", pawn, building, "CK_FillBasin", false,
							"CK.Hygiene.FloatMenu.NeedContainer".Translate().ToString());
					}
				}
			}

			// 倒水：随身带的脸盆里有水时，把水倒进水箱所在水网。
			CompWaterStorage storage = building.TryGetComp<CompWaterStorage>();
			if (storage != null)
			{
				CompBasinWater basin = JobDriver_FillBasin.CarriedBasinWithWater(pawn);
				if (basin != null)
				{
					CompPipe pipe = building.GetComp<CompPipe>();
					PlumbingNet net = pipe?.pipeNet;
					bool canPour = pawn.CanReserveAndReach(building, PathEndMode.Touch, pawn.NormalMaxDanger())
						&& net != null
						&& net.WaterTowers.Any(t => t != null && t.WorkingNow && t.space > 0.1f);
					yield return UseOption("CK.Hygiene.FloatMenu.PourBasin", pawn, building, "CK_EmptyBasin",
						canPour,
						canPour ? null : "CK.Hygiene.FloatMenu.NoWaterTank".Translate().ToString());
				}
			}
		}

		/// <summary>
		/// 右键小池塘/河边的水面：用随身容器打水。取水 Job 的 TargetA 是岸边可站格，
		/// 司机再从它反查相邻水格。
		/// </summary>
		public override IEnumerable<FloatMenuOption> GetOptions(FloatMenuContext context)
		{
			Pawn pawn = context.FirstSelectedPawn;
			Map map = context.map;
			IntVec3 waterCell = context.ClickedCell;
			if (pawn == null
				|| map == null
				|| !pawn.IsColonistPlayerControlled
				|| !waterCell.InBounds(map)
				|| !waterCell.GetTerrain(map).IsWater)
			{
				yield break;
			}
			IntVec3 stand = WaterSourceUtility.FindStandCell(pawn, waterCell);
			if (!stand.IsValid)
			{
				yield break;
			}

			CompBasinWater carried = WaterContainerUtility.CarriedContainer(pawn, requireWater: false);
			Thing spare = carried == null ? WaterContainerUtility.FindSpawnedContainer(pawn, requireWater: false) : null;
			CompBasinWater container = carried ?? spare?.TryGetComp<CompBasinWater>();
			if (container != null)
			{
				string fillKey = WaterContainerUtility.IsBucket(container)
					? "CK.Hygiene.FloatMenu.FillBucket"
					: "CK.Hygiene.FloatMenu.FillBasin";
				yield return UseOption(fillKey, pawn, stand, "CK_FillBasin", true, null, spare);
				yield break;
			}

			CompBasinWater fullCarried = WaterContainerUtility.CarriedContainerAny(pawn);
			if (fullCarried != null)
			{
				string fullKey = WaterContainerUtility.IsBucket(fullCarried)
					? "CK.Hygiene.FloatMenu.FillBucket"
					: "CK.Hygiene.FloatMenu.FillBasin";
				yield return UseOption(fullKey, pawn, stand, "CK_FillBasin", false,
					"CK.Hygiene.FloatMenu.ContainerFull".Translate().ToString());
			}
			else
			{
				yield return UseOption("CK.Hygiene.FloatMenu.FillBasin", pawn, stand, "CK_FillBasin", false,
					"CK.Hygiene.FloatMenu.NeedContainer".Translate().ToString());
			}
		}

		private static FloatMenuOption UseOption(
			string labelKey, Pawn pawn, LocalTargetInfo target, string jobDefName, bool canDo, string reason,
			Thing targetB = null)
		{
			string label = labelKey.Translate();
			if (!canDo)
			{
				return new FloatMenuOption(
					label + " " + "CK.Hygiene.FloatMenu.Disabled".Translate(reason ?? labelKey.Translate()),
					null);
			}
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail(jobDefName);
			if (def == null)
			{
				return new FloatMenuOption(label + " " + "CK.Hygiene.FloatMenu.Disabled".Translate(jobDefName), null);
			}
			return new FloatMenuOption(
				label,
				() =>
				{
					Job job = JobMaker.MakeJob(def);
					job.targetA = target;
					if (targetB != null)
					{
						job.targetB = targetB;
					}
					pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				},
				MenuOptionPriority.Default,
				null,
				target.Thing);
		}

		private static string FillReason(bool reachable, bool sourceHasWater)
		{
			if (!reachable)
			{
				return "CannotReach".Translate().ToString();
			}
			return sourceHasWater
				? "CK.Hygiene.NoWaterSupply".Translate().ToString()
				: "CK.Hygiene.FloatMenu.NoWaterSource".Translate().ToString();
		}

		private static string RejectReason(Building_AssignableFixture fixture)
		{
			if (fixture.blockageComp != null && fixture.blockageComp.blocked)
			{
				return "CK.Hygiene.Blocked".Translate().ToString();
			}
			if (!fixture.HasWaterSupply)
			{
				return "CK.Hygiene.NoWaterSupply".Translate().ToString();
			}
			AcceptanceReport report = fixture.Working();
			if (!report.Accepted)
			{
				return report.Reason;
			}
			if (!fixture.Spawned)
			{
				return "CK.Hygiene.FloatMenu.CannotUse".Translate().ToString();
			}
			return "CK.Hygiene.FloatMenu.CannotUse".Translate().ToString();
		}

		private static Filth FindFilth(Building building)
		{
			Map map = building.Map;
			if (map == null)
			{
				return null;
			}
			foreach (IntVec3 cell in building.OccupiedRect().ExpandedBy(1))
			{
				if (!cell.InBounds(map))
				{
					continue;
				}
				foreach (Thing thing in cell.GetThingList(map))
				{
					if (thing is Filth filth && !filth.Destroyed && filth.Map == map)
					{
						return filth;
					}
				}
			}
			return null;
		}
	}
}
