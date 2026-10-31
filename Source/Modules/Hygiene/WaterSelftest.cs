using System.Collections.Generic;
using System.Linq;
using ColonyKit.Modules.Hygiene.Dbh;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 取水动作自检：带 <c>-colonykit-water-test</c> 启动时，在 quicktest 地图上搭
	/// 「水井 → 水管 → 水龙头 + 水桶」，让真实小人执行 WorkGiver 派出的
	/// <c>CK_FillWaterPoint</c>，观察水箱和取水次数是否真的变化；随后再让同一小人
	/// 在免费取水点执行 <c>CK_FillBasin</c>，确认水桶真的装满。
	/// </summary>
	internal static class WaterSelftest
	{
		public const string CommandLineKey = "-colonykit-water-test";

		private const int MaxTicks = 18000;

		private static bool ran;
		private static bool armed;
		private static bool tapPhaseDone;
		private static bool spotPhaseDone;
		private static bool drinkPhaseDone;
		private static bool listPhaseDone;
		private static bool finishPending;
		private static Map map;
		private static Pawn pawn;
		private static Building_WaterPoint tap;
		private static Building_WaterSpot spot;
		private static Thing bucket;
		private static Thing basin;
		private static Bill_Production listBill;
		private static Bill_Production bulkBill;
		private static readonly System.Collections.Generic.List<Thing> bulkBuckets =
			new System.Collections.Generic.List<Thing>();
		private static bool bulkPhaseDone;
		private static Bill_Production bottleBill;
		private static bool bottlePhaseDone;
		private static bool waterPatientPhaseDone;
		private static Pawn waterPatient;
		private static bool basinPhaseDone;
		private static bool bringWaterPhaseDone;
		private static Pawn bringWaterTarget;
		private static bool bringWaterNeeded;
		private static bool bringWaterWorkGiver;
		private static bool bringWaterTapBottle;
		private static IntVec3 rigOrigin;
		private static int startTick;
		private static int nextLogTick;
		private static string failure;
		private static bool intakeChecked;
		private static float intakeStartWater;

		public static void RunIfRequested(Game game)
		{
			if (ran || game == null || !GenCommandLine.CommandLineArgPassed(CommandLineKey))
			{
				return;
			}
			ran = true;
			try
			{
				Run(game);
			}
			catch (System.Exception exception)
			{
				ModLog.Error("Hygiene", "water selftest crashed: " + exception);
				Fail("exception");
			}
		}

		private static void Run(Game game)
		{
			map = Find.AnyPlayerHomeMap ?? game.Maps.FirstOrDefault();
			if (map == null)
			{
				Fail("no map");
				return;
			}
			if (!CheckWaterSearchRadiusGuard())
			{
				Fail("GenRadial radius log");
				return;
			}
			pawn = map.mapPawns.FreeColonists.FirstOrDefault(p =>
				p.RaceProps.Humanlike
				&& !p.Downed
				&& p.workSettings != null
				&& !p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)
				&& p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
			if (pawn == null)
			{
				Fail("no hauler");
				return;
			}
			if (!TryBuildRig(out string error))
			{
				Fail(error);
				return;
			}
			startTick = Find.TickManager.TicksGame;
			nextLogTick = startTick + 250;
			armed = true;
			Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
			ModLog.Info(
				"Hygiene",
				$"water selftest armed: pawn={pawn.LabelShort} tap={tap.Position} spot={spot.Position} bucket={bucket.Position}");
		}

		/// <summary>
		/// 回归：全图找水（range 9999）时搜索半径必须压在 GenRadial 径向表内。
		/// 以前半径原样传进 80，<c>NumCellsInRadius</c> 会打印
		/// "Not enough squares to get to radius 80. Max is 79.81228"，
		/// 站在地图中间的小人每次找水都刷一条红字；现在按
		/// <see cref="WaterSourceUtility.MaxWaterSearchRadius"/> 封顶。
		/// </summary>
		private static bool CheckWaterSearchRadiusGuard()
		{
			int before = CountGenRadialRadiusLogs();
			WaterSourceUtility.FindSurfaceWaterStandCell(map, map.Center, 9999f);
			Pawn drinker = map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.RaceProps.Humanlike);
			if (drinker != null)
			{
				ClosestSanitation.TryFindWaterCell(drinker, 9999f);
			}
			int after = CountGenRadialRadiusLogs();
			bool pass = after == before;
			ModLog.Info("Hygiene", $"water selftest radius guard: pass={pass} before={before} after={after}");
			return pass;
		}

		private static int CountGenRadialRadiusLogs()
		{
			int count = 0;
			foreach (LogMessage message in Verse.Log.Messages)
			{
				if (message.text != null && message.text.Contains("Not enough squares to get to radius"))
				{
					count += message.repeats;
				}
			}
			return count;
		}

		private static bool TryBuildRig(out string error)		{
			error = null;
			if (!TryFindArea(map, out IntVec3 origin))
			{
				error = "no clear area";
				return false;
			}
			rigOrigin = origin;
			IntVec3 wellCell = origin + new IntVec3(1, 0, 1);
			IntVec3 pipeCell = origin + new IntVec3(2, 0, 1);
			IntVec3 tapCell = origin + new IntVec3(3, 0, 1);
			IntVec3 spotCell = origin + new IntVec3(4, 0, 1);
			IntVec3 bucketCell = origin + new IntVec3(4, 0, 0);
			IntVec3 pawnCell = origin + new IntVec3(2, 0, 0);

			ThingDef wellDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_DbhWaterWell");
			ThingDef pipeDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_DbhWaterPipe");
			ThingDef tapDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_WaterPoint");
			ThingDef spotDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_WaterSpot");
			ThingDef bucketDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_WaterBucket");
			if (wellDef == null || pipeDef == null || tapDef == null || spotDef == null || bucketDef == null)
			{
				error = "missing defs";
				return false;
			}

			SpawnBuilding(wellDef, wellCell);
			SpawnBuilding(pipeDef, pipeCell);
			tap = SpawnBuilding(tapDef, tapCell) as Building_WaterPoint;
			// 免费取水点只能建在水面：把它的格改成浅水，并先验证 PlaceWorker 的放行规则。
			map.terrainGrid.SetTerrain(spotCell, TerrainDefOf.WaterShallow);
			PlaceWorker waterSpotPlace = new PlaceWorker_WaterSpot();
			bool waterAllowed = waterSpotPlace.AllowsPlacing(spotDef, spotCell, Rot4.North, map).Accepted;
			bool landAllowed = waterSpotPlace.AllowsPlacing(spotDef, pawnCell, Rot4.North, map).Accepted;
			if (!waterAllowed || landAllowed)
			{
				error = "water spot place rule";
				return false;
			}
			spot = SpawnBuilding(spotDef, spotCell) as Building_WaterSpot;
			if (tap == null || spot == null)
			{
				error = "wrong thingClass";
				return false;
			}
			ModLog.Info("Hygiene", "water selftest: water spot place rule water=True land=False");
			tap.SetDrawOrder(1);
			intakeStartWater = tap.StoredWater;
			bucket = SpawnItem(bucketDef, bucketCell);
			for (int i = 0; i < 5; i++)
			{
				bulkBuckets.Add(SpawnItem(bucketDef, origin + new IntVec3(i, 0, 2)));
			}

			ThingDef basinDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_WashBasinItem");
			if (basinDef == null || !PawnUtility.CanPickUp(pawn, basinDef))
			{
				error = "basin is not pick-up-able";
				return false;
			}
			basin = ThingMaker.MakeThing(basinDef);
			if (!pawn.inventory.innerContainer.TryAdd(basin))
			{
				error = "basin not added to inventory";
				return false;
			}

			pawn.Position = pawnCell;
			pawn.Notify_Teleported(true, true);
			FillNeeds();
			pawn.drafter.Drafted = false;
			pawn.workSettings.EnableAndInitialize();
			pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
			WorkTypeDef waterFetching = DefDatabase<WorkTypeDef>.GetNamedSilentFail("CK_WaterFetching");
			if (waterFetching == null)
			{
				error = "no water fetching work type";
				return false;
			}
			pawn.workSettings.SetPriority(waterFetching, 1);
			map.GetComponent<MapComponent_Hygiene>()?.PipeComp?.RegenPipeGrids();

			WorkGiverDef workGiverDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_FillWaterPoint");
			WorkGiver_Scanner workGiver = workGiverDef?.Worker as WorkGiver_Scanner;
			if (workGiver == null)
			{
				error = "no workgiver";
				return false;
			}
			if (!workGiver.HasJobOnThing(pawn, tap))
			{
				error = "workgiver rejected";
				return false;
			}
			Job job = workGiver.JobOnThing(pawn, tap);
			if (job == null)
			{
				error = "workgiver produced no job";
				return false;
			}
			pawn.jobs.StopAll(false);
			pawn.jobs.StartJob(job, JobCondition.InterruptForced);
			return true;
		}

		private static void FillNeeds()
		{
			Need food = pawn.needs?.food;
			if (food != null) food.CurLevel = food.MaxLevel;
			Need rest = pawn.needs?.rest;
			if (rest != null) rest.CurLevel = rest.MaxLevel;
			Need joy = pawn.needs?.joy;
			if (joy != null) joy.CurLevel = joy.MaxLevel;
			Need mood = pawn.needs?.mood;
			if (mood != null) mood.CurLevel = mood.MaxLevel;
		}

		private static Thing SpawnBuilding(ThingDef def, IntVec3 cell)
		{
			Thing thing = ThingMaker.MakeThing(def);
			GenSpawn.Spawn(thing, cell, map);
			thing.SetFaction(Faction.OfPlayer);
			return thing;
		}

		private static Thing SpawnItem(ThingDef def, IntVec3 cell)
		{
			Thing thing = ThingMaker.MakeThing(def);
			GenSpawn.Spawn(thing, cell, map);
			return thing;
		}

		private static bool TryFindArea(Map map, out IntVec3 origin)
		{
			for (int z = 2; z < map.Size.z - 4; z++)
			{
				for (int x = 2; x < map.Size.x - 6; x++)
				{
					IntVec3 candidate = new IntVec3(x, 0, z);
					bool clear = true;
					for (int dz = 0; dz < 3 && clear; dz++)
					{
						for (int dx = 0; dx < 5 && clear; dx++)
						{
							IntVec3 cell = candidate + new IntVec3(dx, 0, dz);
							if (!cell.InBounds(map)
								|| cell.Fogged(map)
								|| cell.GetEdifice(map) != null
								|| cell.GetThingList(map).Any(t => t is Pawn))
							{
								clear = false;
							}
						}
					}
					if (!clear)
					{
						continue;
					}
					for (int dz = 0; dz < 3; dz++)
					{
						for (int dx = 0; dx < 5; dx++)
						{
							IntVec3 cell = candidate + new IntVec3(dx, 0, dz);
							foreach (Thing thing in cell.GetThingList(map).ToList())
							{
								if (!(thing is Pawn))
								{
									thing.Destroy(DestroyMode.Vanish);
								}
							}
							map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
						}
					}
					origin = candidate;
					return true;
				}
			}
			origin = IntVec3.Invalid;
			return false;
		}

		/// <summary>每帧由 <c>TickManagerUpdate</c> 后缀调用：推时间、写日志、阶段切换。</summary>
		public static void Pump()
		{
			if (!armed)
			{
				return;
			}
			TickManager ticks = Find.TickManager;
			if (ticks == null)
			{
				return;
			}
			if (ticks.CurTimeSpeed != TimeSpeed.Ultrafast)
			{
				ticks.CurTimeSpeed = TimeSpeed.Ultrafast;
			}
			RemoveForcePause();

			int elapsed = ticks.TicksGame - startTick;
			if (finishPending && elapsed >= 250)
			{
				Finish(false);
				return;
			}

			if (!intakeChecked && elapsed >= 300)
			{
				intakeChecked = true;
				float gained = tap.StoredWater - intakeStartWater;
				if (gained < 1.5f)
				{
					failure = $"unpowered intake only {gained:F1} L in 300 ticks";
					Finish(false);
					return;
				}
				ModLog.Info(
					"Hygiene",
					$"water selftest unpowered intake ok: +{gained:F1} L in 300 ticks " +
					$"(source={tap.IntakeSourceLabel})");
			}

			if (!tapPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("tap");
				}
				if (tap != null && tap.deliveredDraws >= 1 && tap.StoredWater > 1f)
				{
					tapPhaseDone = true;
					ModLog.Info(
						"Hygiene",
						$"water selftest tap phase done: delivered={tap.deliveredDraws} water={tap.StoredWater:F1}");
					StartSpotPhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!spotPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("spot");
				}
				CompBasinWater container = bucket?.TryGetComp<CompBasinWater>();
				if (container != null && container.Water >= 59f)
				{
					spotPhaseDone = true;
					ModLog.Info("Hygiene", "water selftest spot phase done: bucket full");
					StartDrinkPhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!drinkPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("drink");
				}
				Need_Thirst thirst = pawn?.needs?.TryGetNeed<Need_Thirst>();
				if (thirst != null && thirst.CurLevel >= 0.9f)
				{
					drinkPhaseDone = true;
					ModLog.Info("Hygiene", $"water selftest drink phase done: thirst={thirst.CurLevel:F2}");
					StartListPhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!listPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("list");
				}
				if (listBill != null && listBill.repeatCount <= 0)
				{
					listPhaseDone = true;
					StartBasinPhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!basinPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("basin");
				}
				CompBasinWater water = basin?.TryGetComp<CompBasinWater>();
				if (water != null && water.Water >= 29f)
				{
					basinPhaseDone = true;
					ModLog.Info("Hygiene", $"water selftest basin phase done: water={water.Water:F1}");
					StartBulkPhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!bulkPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("bulk");
				}
				int full = 0;
				foreach (Thing b in bulkBuckets)
				{
					CompBasinWater c = b?.TryGetComp<CompBasinWater>();
					if (c != null && c.Water >= 59f)
					{
						full++;
					}
				}
				if (bulkBill != null && bulkBill.repeatCount <= 0)
				{
					if (full < 2)
					{
						failure = $"bulk filled only {full} buckets";
						Finish(false);
						return;
					}
					bulkPhaseDone = true;
					ModLog.Info("Hygiene", $"water selftest bulk phase done: full={full}");
					StartWaterPatientPhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!waterPatientPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("patient");
				}
				Need_Thirst thirst = waterPatient?.needs?.TryGetNeed<Need_Thirst>();
				if (thirst != null && thirst.CurLevel >= 0.95f)
				{
					waterPatientPhaseDone = true;
					ModLog.Info("Hygiene", $"water selftest patient phase done: thirst={thirst.CurLevel:F2}");
					StartBottlePhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!bottlePhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("bottle");
				}
				if (bottleBill != null && bottleBill.repeatCount <= 0)
				{
					Thing bottle = MineralWaterUtility.Carried(pawn);
					Need_Thirst thirst = pawn?.needs?.TryGetNeed<Need_Thirst>();
					if (bottle == null || thirst == null)
					{
						failure = "mineral water bottle missing";
						Finish(false);
						return;
					}
					thirst.CurLevel = 0.2f;
					bottle.Ingested(pawn, 0f);
					if (thirst.CurLevel < 0.35f)
					{
						failure = "mineral water did not restore thirst";
						Finish(false);
						return;
					}
					bottlePhaseDone = true;
					ModLog.Info(
						"Hygiene",
						$"water selftest bottle phase done: thirst={thirst.CurLevel:F2}");
					StartBringWaterPhase();
					nextLogTick = elapsed + 250;
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
				return;
			}

			if (!bringWaterPhaseDone)
			{
				if (elapsed >= nextLogTick)
				{
					nextLogTick = elapsed + 500;
					Log("bringwater");
				}
				if (bringWaterTarget == null)
				{
					Finish(false);
					return;
				}
				Thing delivered = MineralWaterUtility.Carried(bringWaterTarget);
				Need_Thirst targetThirst = bringWaterTarget.needs?.TryGetNeed<Need_Thirst>();
				bool selfDrank = bringWaterTarget.CurJobDef == JobDefOf.Ingest
					|| (targetThirst != null && targetThirst.CurLevel > 0.3f);
				if (delivered != null || selfDrank)
				{
					bool drinkNode = PrisonerThinkTreeHasDrinkGiver();
					bool drinkJob = selfDrank;
					if (!drinkJob && delivered != null)
					{
						// 囚徒自己那条线的证据：拿手里的瓶装水，JobGiver 应当发出 Ingest。
						ThinkResult result = new JobGiver_DrinkWater().TryIssueJobPackage(
							bringWaterTarget, default(JobIssueParams));
						drinkJob = result.Job != null && result.Job.def == JobDefOf.Ingest;
					}
					bringWaterPhaseDone = true;
					ModLog.Info(
						"Hygiene",
						$"water selftest bring water done: bottles={delivered?.stackCount ?? 0} " +
						$"selfDrank={selfDrank} needWater={bringWaterNeeded} " +
						$"workGiver={bringWaterWorkGiver} tapBottle={bringWaterTapBottle} " +
						$"drinkNode={drinkNode} drinkJob={drinkJob} " +
						$"thirst={(targetThirst?.CurLevel ?? -1f):F2}");
					if (!drinkNode || !drinkJob)
					{
						failure = "prisoner never drinks delivered water";
						Finish(false);
						return;
					}
					Finish(true);
				}
				else if (elapsed >= MaxTicks)
				{
					Finish(false);
				}
			}
		}

		private static void StartSpotPhase()
		{
			JobDef fillDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_FillBasin");
			if (fillDef == null || spot == null || bucket == null)
			{
				Fail("spot phase missing");
				return;
			}
			CompBasinWater container = bucket.TryGetComp<CompBasinWater>();
			if (container != null)
			{
				container.Water = 0f;
			}
			Job job = JobMaker.MakeJob(fillDef, spot);
			ForceJob(job, "spot job rejected");
		}

		/// <summary>把口渴压到 20%，让小人从免费取水点喝水，验证「口渴能喝到水」。</summary>
		private static void StartDrinkPhase()
		{
			Need_Thirst thirst = pawn?.needs?.TryGetNeed<Need_Thirst>();
			JobDef drinkDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromBasin");
			if (thirst == null || drinkDef == null || spot == null)
			{
				Fail("drink phase missing");
				return;
			}
			thirst.CurLevel = 0.2f;
			Job job = JobMaker.MakeJob(drinkDef, spot);
			ForceJob(job, "drink job rejected");
		}

		/// <summary>在免费取水点清单里加一条「取水 1 次」，让 WorkGiver 派活验证清单真的会执行。</summary>
		private static void StartListPhase()
		{
			if (spot == null || bucket == null)
			{
				Fail("list phase missing");
				return;
			}
			CompBasinWater container = bucket.TryGetComp<CompBasinWater>();
			if (container != null)
			{
				container.Water = 0f;
			}
			RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("CK_DrawWaterBucket");
			Bill_Production bill = recipe?.MakeNewBill() as Bill_Production;
			if (bill == null)
			{
				Fail("list recipe missing");
				return;
			}
			bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
			bill.repeatCount = 1;
			spot.BillStack.AddBill(bill);
			listBill = bill;
			WorkGiverDef def = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_FillWaterSpot");
			WorkGiver_Scanner workGiver = def?.Worker as WorkGiver_Scanner;
			if (workGiver == null || !workGiver.HasJobOnThing(pawn, spot))
			{
				Fail("list workgiver rejected");
				return;
			}
			Job job = workGiver.JobOnThing(pawn, spot);
			if (job == null)
			{
				Fail("list workgiver produced no job");
				return;
			}
			ForceJob(job, "list job rejected");
		}

		/// <summary>把随身脸盆拿到免费取水点打满，验证脸盆可携带、可当容器打水。</summary>
		private static void StartBasinPhase()
		{
			JobDef fillDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_FillBasin");
			CompBasinWater water = basin?.TryGetComp<CompBasinWater>();
			if (fillDef == null || spot == null || water == null)
			{
				Fail("basin phase missing");
				return;
			}
			water.Water = 0f;
			Job job = JobMaker.MakeJob(fillDef, spot);
			ForceJob(job, "basin job rejected");
		}

		private static void StartBulkPhase()
		{
			RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("CK_DrawWaterBulk");
			Bill_Production bill = recipe?.MakeNewBill() as Bill_Production;
			if (bill == null || spot == null)
			{
				Fail("bulk phase missing");
				return;
			}
			bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
			bill.repeatCount = 1;
			spot.BillStack.AddBill(bill);
			bulkBill = bill;
			WorkGiverDef def = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_FillWaterSpot");
			WorkGiver_Scanner giver = def?.Worker as WorkGiver_Scanner;
			if (giver == null || !giver.HasJobOnThing(pawn, spot))
			{
				Fail("bulk workgiver rejected");
				return;
			}
			Job job = giver.JobOnThing(pawn, spot);
			if (job == null || job.def.defName != "CK_FillSpotBucketBulk")
			{
				Fail("bulk job not produced");
				return;
			}
			ModLog.Info(
				"Hygiene",
				$"water selftest bulk job: queue={job.targetQueueB?.Count ?? 0} count={job.count} " +
				$"carried={pawn.inventory?.innerContainer?.Count ?? -1}");
			ForceJob(job, "bulk job rejected");
		}

		private static void StartWaterPatientPhase()
		{
			waterPatient = map.mapPawns.FreeColonists.FirstOrDefault(p =>
				p != pawn && p.RaceProps.Humanlike);
			if (waterPatient == null || basin == null)
			{
				Fail("patient phase missing");
				return;
			}
			waterPatient.jobs.StopAll(false);
			waterPatient.drafter.Drafted = false;
			Need_Thirst thirst = waterPatient.needs?.TryGetNeed<Need_Thirst>();
			CompBasinWater water = basin.TryGetComp<CompBasinWater>();
			if (thirst == null || water == null)
			{
				Fail("patient phase missing needs");
				return;
			}
			thirst.CurLevel = 0.2f;
			water.Water = water.Props.Capacity;
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_WaterPatient");
			if (def == null)
			{
				Fail("water patient job missing");
				return;
			}
			Job job = JobMaker.MakeJob(def, waterPatient);
			ForceJob(job, "water patient job rejected");
		}

		private static void StartBottlePhase()
		{
			RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("CK_DrawWaterBottle");
			Bill_Production bill = recipe?.MakeNewBill() as Bill_Production;
			if (bill == null || spot == null)
			{
				Fail("bottle phase missing");
				return;
			}
			bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
			bill.repeatCount = 1;
			spot.BillStack.AddBill(bill);
			bottleBill = bill;
			WorkGiverDef def = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_FillWaterSpot");
			WorkGiver_Scanner giver = def?.Worker as WorkGiver_Scanner;
			if (giver == null || !giver.HasJobOnThing(pawn, spot))
			{
				Fail("bottle workgiver rejected: " + DescribeWaterSpotBills());
				return;
			}
			Job job = giver.JobOnThing(pawn, spot);
			if (job == null || job.def.defName != "CK_FillWaterBottle")
			{
				Fail("bottle job not produced");
				return;
			}
			ForceJob(job, "bottle job rejected");
		}

		/// <summary>
		/// 送水自检：在离水源很远的地方放一个口渴 15% 的囚徒，医生身边放三瓶凉白开，
		/// 让医生执行 CK_BringWater；水进囚徒物品栏后，确认囚徒自己那条线真的会去喝
		/// （ThinkTree 里有喝水节点，JobGiver 能对着手里的瓶装水发出 Ingest）。
		/// 顺便验证水龙头也能直接装 1 L 矿泉水（水网水量应当少 1 升）。
		/// </summary>
		/// <summary>取水点清单的现场状态，供「工作器拒绝」时定位原因。</summary>
		private static string DescribeWaterSpotBills()
		{
			if (spot == null)
			{
				return "spot=null";
			}
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			builder.Append($"bills={spot.BillStack.Count} any={spot.BillStack.AnyShouldDoNow}");
			foreach (Bill bill in spot.BillStack.Bills)
			{
				string mode = bill is Bill_Production prod ? prod.repeatMode?.defName ?? "?" : "-";
				int count = bill is Bill_Production prod2 ? prod2.repeatCount : -1;
				builder.Append(
					$" [{bill.recipe?.defName ?? "?"} mode={mode} count={count} " +
					$"should={bill.ShouldDoNow()} allowed={bill.PawnAllowedToStartAnew(pawn)}]");
			}
			builder.Append(
				$" reach={pawn.CanReserveAndReach(spot, PathEndMode.Touch, pawn.NormalMaxDanger())}" +
				$" forbidden={spot.IsForbidden(pawn)}" +
				$" spawned={spot.Spawned}");
			return builder.ToString();
		}

		private static void StartBringWaterPhase()
		{
			ThingDef boiled = MineralWaterUtility.BoiledDef;
			if (boiled == null || pawn == null || map == null)
			{
				Fail("bring water defs missing");
				return;
			}
			if (!TryFindDryCell(out IntVec3 cell))
			{
				Fail("no cell away from water");
				return;
			}
			Faction faction = Find.FactionManager.AllFactions.ToList().FirstOrDefault(f =>
				f != null && f != Faction.OfPlayer && !f.IsPlayer && f.def.humanlikeFaction);
			Pawn prisoner = PawnGenerator.GeneratePawn(PawnKindDefOf.Villager, faction);
			if (prisoner == null)
			{
				Fail("no prisoner generated");
				return;
			}
			GenSpawn.Spawn(prisoner, cell, map, WipeMode.Vanish);
			prisoner.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
			HygieneUtility.EnsureThirst(prisoner);
			Need_Thirst thirst = prisoner.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null)
			{
				Fail("prisoner has no thirst need");
				return;
			}
			thirst.CurLevel = 0.15f;
			bringWaterTarget = prisoner;
			bringWaterNeeded = BringWaterUtility.ShouldDeliverWater(prisoner);
			if (!bringWaterNeeded)
			{
				Fail("prisoner can still reach water");
				return;
			}
			WorkGiverDef giverDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_BringWaterPrisoner");
			bringWaterWorkGiver = (giverDef?.Worker as WorkGiver_Scanner)?.HasJobOnThing(pawn, prisoner) == true;

			float before = tap?.StoredWater ?? 0f;
			bool tapBottle = tap != null && tap.CanBottleMineralWater(pawn) && tap.TryDrawMineralBottle(out _);
			float after = tap?.StoredWater ?? 0f;
			bringWaterTapBottle = tapBottle && before - after >= 0.5f;

			Thing stack = ThingMaker.MakeThing(boiled);
			stack.stackCount = 3;
			GenSpawn.Spawn(stack, pawn.Position + IntVec3.South, map, WipeMode.Vanish);
			Job job = BringWaterUtility.TryMakeJob(pawn, prisoner);
			if (job == null || job.def.defName != "CK_BringWater" || job.count <= 0)
			{
				Fail("bring water job missing");
				return;
			}
			ModLog.Info(
				"Hygiene",
				$"water selftest bring water start: prisoner={prisoner.LabelShort} cell={cell} " +
				$"count={job.count} needWater={bringWaterNeeded} workGiver={bringWaterWorkGiver} " +
				$"tapBottle={bringWaterTapBottle} ({before:F1}->{after:F1})");
			ForceJob(job, "bring water job not started");
		}

		/// <summary>找一格离取水装置 40 格以上、附近也没有地表水的空地，模拟「牢房里没水」。</summary>
		private static bool TryFindDryCell(out IntVec3 cell)
		{
			cell = IntVec3.Invalid;
			if (map == null)
			{
				return false;
			}
			foreach (IntVec3 candidate in GenRadial.RadialCellsAround(rigOrigin, 50f, true))
			{
				if (!candidate.InBounds(map)
					|| !candidate.Standable(map)
					|| candidate.Fogged(map)
					|| candidate.DistanceTo(rigOrigin) < 42f)
				{
					continue;
				}
				if (WaterSourceUtility.FindSurfaceWaterStandCell(map, candidate, 40f).IsValid)
				{
					continue;
				}
				cell = candidate;
				return true;
			}
			return false;
		}

		/// <summary>囚徒那支 ThinkTree 里有没有喝水节点（原版囚徒分支没有）。</summary>
		private static bool PrisonerThinkTreeHasDrinkGiver()
		{
			ThinkTreeDef tree = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("Humanlike");
			if (tree?.thinkRoot == null)
			{
				return false;
			}
			foreach (ThinkNode node in tree.thinkRoot.ThisAndChildrenRecursive)
			{
				if (node is JobGiver_DrinkWater)
				{
					return true;
				}
			}
			return false;
		}

		private static void Log(string phase)
		{
			CompBasinWater container = bucket?.TryGetComp<CompBasinWater>();
			ModLog.Info(
				"Hygiene",
				$"water selftest {phase}: tick={Find.TickManager.TicksGame - startTick} " +
				$"job={pawn?.CurJobDef?.defName ?? "none"} " +
				$"tapDelivered={tap?.deliveredDraws ?? 0} tapWater={(tap?.StoredWater ?? 0f):F1} " +
				$"bucketWater={(container?.Water ?? -1f):F1} bucketSpawned={bucket?.Spawned} " +
				$"basinWater={(basin?.TryGetComp<CompBasinWater>()?.Water ?? -1f):F1} " +
				$"thirst={(pawn?.needs?.TryGetNeed<Need_Thirst>()?.CurLevel ?? -1f):F2} " +
				$"patientThirst={(waterPatient?.needs?.TryGetNeed<Need_Thirst>()?.CurLevel ?? -1f):F2} " +
				$"bringThirst={(bringWaterTarget?.needs?.TryGetNeed<Need_Thirst>()?.CurLevel ?? -1f):F2} " +
				$"bringCarried={MineralWaterUtility.Carried(bringWaterTarget)?.stackCount ?? 0} " +
				$"listCompleted={listBill?.repeatCount ?? -1}");
		}

		private static void Fail(string reason)
		{
			failure = reason;
			finishPending = true;
			armed = true;
			startTick = Find.TickManager?.TicksGame ?? 0;
			nextLogTick = startTick + 250;
		}

		private static void Finish(bool pass)
		{
			armed = false;
			finishPending = false;
			CompBasinWater container = bucket?.TryGetComp<CompBasinWater>();
			ModLog.Info(
				"Hygiene",
				$"water selftest: pass={pass} reason={failure ?? "ok"} " +
				$"tapDelivered={tap?.deliveredDraws ?? 0} tapWater={(tap?.StoredWater ?? 0f):F1} " +
				$"bucketWater={(container?.Water ?? -1f):F1} " +
				$"job={pawn?.CurJobDef?.defName ?? "none"}");
			Root.Shutdown();
		}

		private static void RemoveForcePause()
		{
			if (Find.WindowStack == null || !Find.WindowStack.WindowsForcePause)
			{
				return;
			}
			var windows = Find.WindowStack.Windows;
			for (int i = windows.Count - 1; i >= 0; i--)
			{
				Window window = windows[i];
				if (window != null && window.forcePause)
				{
					Find.WindowStack.TryRemove(window, false);
				}
			}
		}
	}

	/// <summary>自检每帧水泵：quicktest 有时会停在强制暂停窗口，这里把时间推起来。</summary>
	[HarmonyPatch(typeof(TickManager), "TickManagerUpdate")]
	internal static class Patch_WaterSelftestPump
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			WaterSelftest.Pump();
		}
	}
}
