using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 开发用探针：启动参数带 <c>-colonykit-nannybot-test</c> 时，在新地图上放一座充电桩，
	/// 让桩组装一台机器人，并把种族/需求/工作能力写进 Player.log。
	/// 正常游戏不会触发。
	/// </summary>
	internal static class NannyBotSelftest
	{
		public const string CommandLineKey = "-colonykit-nannybot-test";

		private static bool ran;

		private static IntVec3 FindFreeRect(Map map, IntVec3 center, int radius, int width, int height)
		{
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dz = -radius; dz <= radius; dz++)
				{
					IntVec3 root = center + new IntVec3(dx, 0, dz);
					CellRect rect = new CellRect(root.x, root.z, width, height);
					if (rect.Contains(center))
					{
						continue;
					}
					bool free = true;
					foreach (IntVec3 cell in rect.Cells)
					{
						if (!cell.InBounds(map) || !cell.Standable(map)
							|| cell.GetEdifice(map) != null)
						{
							free = false;
							break;
						}
					}
					if (free)
					{
						return root;
					}
				}
			}
			return IntVec3.Invalid;
		}

		private static void ProbeMkVBay(Map map, StringBuilder builder)
		{
			ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("CK_MkVAssemblyBay");
			builder.Append(" mkvBayDef=").Append(def != null)
				.Append(" mkvBayTicker=").Append(def?.tickerType.ToString() ?? "none");
			if (def == null)
			{
				return;
			}

			IntVec3 cell = FindFreeRect(map, map.Center, 20, 2, 2);
			if (!cell.IsValid)
			{
				builder.Append(" mkvBayCell=none");
				return;
			}

			Thing bay = ThingMaker.MakeThing(def);
			GenSpawn.Spawn(bay, cell, map, Rot4.South);
			CompMkVAssemblyBay comp = bay.TryGetComp<CompMkVAssemblyBay>();
			builder.Append(" mkvBayComp=").Append(comp != null);
			if (comp == null)
			{
				bay.Destroy(DestroyMode.Vanish);
				return;
			}

			var complete = AccessTools.Method(typeof(CompMkVAssemblyBay), "CompleteAssembly");
			builder.Append(" mkvBayCompleteMethod=").Append(complete != null);
			try
			{
				complete?.Invoke(comp, null);
			}
			catch (Exception exception)
			{
				ModLog.Warn("Robotics", $"Mk-V bay probe failed: {exception.Message}");
			}

			Pawn mkv = null;
			foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
			{
				if (pawn.kindDef == NannyBotDefOf.CK_RobotMkVKind)
				{
					mkv = pawn;
					break;
				}
			}

			builder.Append(" mkvSpawned=").Append(mkv != null);
			if (mkv != null)
			{
				CompNannyBot mkvComp = mkv.TryGetComp<CompNannyBot>();
				CompMkVCombat combat = mkv.TryGetComp<CompMkVCombat>();
				ThingWithComps weapon = mkv.equipment?.Primary;
				Verb weaponVerb = weapon?.TryGetComp<CompEquippable>()?.PrimaryVerb;
				float weaponRange = weapon?.def?.Verbs != null && weapon.def.Verbs.Count > 0
					? weapon.def.Verbs[0].range
					: 0f;
				builder.Append(" mkvName=").Append(mkv.Name?.ToStringShort ?? "none")
					.Append(" mkvThink=").Append(mkv.RaceProps.thinkTreeMain?.defName ?? "none")
					.Append(" mkvDurability=").Append(mkvComp?.Durability.ToString("0") ?? "none")
					.Append("/").Append(mkvComp?.MaxDurability.ToString("0") ?? "none")
					.Append(" mkvDrafter=").Append(mkv.drafter != null)
					.Append(" mkvTools=").Append(mkv.def.tools?.Count ?? 0)
					.Append(" mkvWeapon=").Append(mkv.equipment?.Primary?.def?.defName ?? "none")
					.Append(" mkvCarry=").Append(mkv.GetStatValue(StatDefOf.CarryingCapacity).ToString("0"))
					.Append(" mkvCarrySetting=").Append(CarrySettingProbe(mkv))
					.Append(" mkvOffhand=").Append(MkVLoadout.Offhand(mkv)?.def?.defName ?? "none")
					.Append(" mkvWeaponRange=").Append(weaponRange.ToString("0.#"))
					.Append(" mkvEffectiveRange=")
					.Append(weaponVerb != null ? weaponVerb.EffectiveRange.ToString("0.#") : "none")
					.Append(" mkvCombat=").Append(combat != null)
					.Append(" mkvHomeBayIsBay=").Append(combat?.homeBay == bay)
					.Append(" mkvHomeIsBay=").Append(mkvComp?.homeStation == bay)
					.Append(" mkvHome=").Append(mkvComp?.homeStation?.def?.defName ?? "none")
					.Append(" mkvTraits=").Append(TraitList(mkv))
					.Append(" incomingDamage=").Append(IncomingDamageProbe(mkv))
					.Append(" mkvComfort=").Append(TemperatureProbe(mkv));
				ProbeMkVChores(map, mkv, builder);
				ProbeCareJobs(map, mkv, builder);
				ProbeMkVDualWield(mkv, builder);
				ProbeMkVBackupWeapon(mkv, builder);
				ProbeArcBladeEffects(mkv, map, builder);
				ProbeCaravanPriority(map, mkv, builder);
				ProbeMkvRandomTraits(mkv, builder);
				ProbeThoughtLog(map, mkv, builder);
				builder.Append(' ').Append(ProbeBedRest(map, mkv));
				builder.Append(' ').Append(SelfRepairProbe(mkv));
				builder.Append(' ').Append(PassionProbe(mkv));
				ProbeMkVThreat(map, mkv, builder);
				ProbeMedicalAid(map, mkv, builder);
				mkv.Destroy(DestroyMode.Vanish);
			}
			bay.Destroy(DestroyMode.Vanish);
		}

		private static string TraitList(Pawn pawn)
		{
			List<Trait> traits = pawn?.story?.traits?.allTraits;
			if (traits == null || traits.Count == 0)
			{
				return "none";
			}
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < traits.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}
				builder.Append(traits[i].def.defName).Append(':').Append(traits[i].Degree);
			}
			return builder.ToString();
		}

		/// <summary>
		/// 随机特性探针：临时打开「追加随机特性」再结算一次，确认只从池里抽 2 个、
		/// 格斗者和冷枪手不会同时出现；结果写进日志后立刻把开关还原。
		/// </summary>
		private static void ProbeMkvRandomTraits(Pawn pawn, StringBuilder builder)
		{
			RoboticsSettings settings = NannyBotUtility.Settings;
			bool oldValue = settings.mkvRandomTraits;
			settings.mkvRandomTraits = true;
			CompNannyBot.ApplyRobotTraitsForTest(pawn);
			settings.mkvRandomTraits = oldValue;

			string[] pool = { "Brawler", "ShootingAccuracy", "FastLearner", "GreatMemory" };
			List<string> picked = new List<string>();
			bool brawler = false;
			bool triggerHappy = false;
			foreach (string name in pool)
			{
				TraitDef def = DefDatabase<TraitDef>.GetNamedSilentFail(name);
				Trait trait = def == null ? null : pawn.story?.traits?.GetTrait(def);
				if (trait == null)
				{
					continue;
				}
				picked.Add(def.defName + ":" + trait.Degree);
				brawler |= def.defName == "Brawler";
				triggerHappy |= def.defName == "ShootingAccuracy";
			}
			builder.Append(" mkvRandomPicked=").Append(picked.Count)
				.Append(" mkvRandom=").Append(picked.Count == 0 ? "none" : string.Join(",", picked))
				.Append(" mkvRandomConflict=").Append(brawler && triggerHappy);
		}

		/// <summary>
		/// 远行队优先探针：把 Mk-V 塞进一支正在组建的商队 Lord，确认三件事：
		/// 1) 思考树最前面接上了 LordDuty 子树；
		/// 2) 进入「收集物资」阶段时它拿到和殖民者一样的打包职责；
		/// 3) 思考结果里的 Job 真的带上了这支 Lord，而不是家务/巡逻那种散活。
		/// 探针结束立刻撤掉 Lord，免得测试用的商队真把它带离地图。
		/// </summary>
		private static void ProbeCaravanPriority(Map map, Pawn mkv, StringBuilder builder)
		{
			bool node = mkv.thinker.TryGetMainTreeThinkNode<ThinkNode_ConditionalFormingCaravan>() != null;
			builder.Append(" caravanNode=").Append(node);

			IntVec3 meeting = CellFinder.RandomClosewalkCellNear(
				map.Center, map, 10, c => c.Standable(map) && c.GetEdifice(map) == null);
			IntVec3 exit = CellFinder.RandomClosewalkCellNear(
				map.Center, map, 14, c => c.Standable(map) && c.GetEdifice(map) == null);
			IntVec3 cargoCell = CellFinder.RandomClosewalkCellNear(
				mkv.Position, map, 6, c => c.Standable(map) && c.GetEdifice(map) == null);
			if (!meeting.IsValid || !exit.IsValid || !cargoCell.IsValid)
			{
				builder.Append(" caravanCells=none");
				return;
			}

			// 地上放一堆钢当货，检查组队时 Mk-V 会不会真的去搬。
			Thing cargo = ThingMaker.MakeThing(ThingDefOf.Steel);
			cargo.stackCount = 25;
			GenSpawn.Spawn(cargo, cargoCell, map);
			TransferableOneWay transferable = new TransferableOneWay();
			transferable.things.Add(cargo);
			transferable.ForceTo(25);
			List<TransferableOneWay> transferables = new List<TransferableOneWay> { transferable };

			LordJob job = new LordJob_FormAndSendCaravan(
				transferables,
				new List<Pawn>(),
				meeting,
				exit,
				map.Tile,
				map.Tile);
			Lord lord = LordMaker.MakeNewLord(Faction.OfPlayer, job, map, new[] { mkv });
			if (lord == null)
			{
				builder.Append(" caravanLord=False");
				cargo.Destroy(DestroyMode.Vanish);
				return;
			}

			// 手工跑一遍「收集物资」阶段：原版按 IsColonist 分流，Mk-V 本该被丢去
			// 集合点等待，我们的后缀会把它改写成和殖民者一样的打包职责。
			LordToil_PrepareCaravan_GatherItems gather = new LordToil_PrepareCaravan_GatherItems(meeting);
			gather.lord = lord;
			gather.UpdateAllDuties();
			builder.Append(" caravanFlag=").Append(NannyBotUtility.IsFormingCaravan(mkv))
				.Append(" caravanDuty=").Append(mkv.mindState?.duty?.def?.defName ?? "none");
			ThinkResult result = mkv.thinker.MainThinkNodeRoot
				.TryIssueJobPackage(mkv, new JobIssueParams());
			builder.Append(" caravanJob=").Append(result.Job?.def?.defName ?? "none")
				.Append(" caravanJobLord=").Append(result.Job != null && result.Job.lord == lord);

			// 手头有长活（研究、做饭之类）时，加入远行队要能把它立刻打断一次。
			CompNannyBot comp = NannyBotUtility.BotComp(mkv);
			Job busy = JobMaker.MakeJob(JobDefOf.Wait, 5000);
			mkv.jobs.StartJob(busy, JobCondition.InterruptForced);
			bool busyBefore = mkv.CurJobDef == JobDefOf.Wait;
			comp?.EnsureCaravanPriority(mkv);
			builder.Append(" caravanDropBusy=").Append(busyBefore)
				.Append("->").Append(mkv.CurJobDef != JobDefOf.Wait);

			map.lordManager.RemoveLord(lord);
			if (!cargo.Destroyed)
			{
				cargo.Destroy(DestroyMode.Vanish);
			}
			builder.Append(" caravanCleared=").Append(!NannyBotUtility.IsFormingCaravan(mkv));
		}

		/// <summary>舒适温度探针：隔寒/隔热设置应用后的舒适温度下限/上限。</summary>
		private static string TemperatureProbe(Pawn pawn)
		{
			float min = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin, true, 0);
			float max = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax, true, 0);
			return min.ToString("0.#") + "/" + max.ToString("0.#");
		}

		/// <summary>承伤乘数探针：原值 / 设置页临时调到 50% 后的值 / 坚韧乘数下的值。</summary>
		private static string IncomingDamageProbe(Pawn pawn)
		{
			StatDef stat = StatDefOf.IncomingDamageFactor;
			if (stat == null)
			{
				return "none";
			}
			float baseValue = pawn.GetStatValue(stat, true, 0);
			RoboticsSettings settings = NannyBotUtility.Settings;
			float oldFactor = settings.mkvIncomingDamageFactor;
			settings.mkvIncomingDamageFactor = 0.5f;
			float scaled = pawn.GetStatValue(stat, true, 0);
			settings.mkvIncomingDamageFactor = oldFactor;

			TraitSet traits = pawn.story?.traits;
			TraitDef toughDef = DefDatabase<TraitDef>.GetNamedSilentFail("Tough");
			bool addedTough = false;
			if (traits != null && toughDef != null && !traits.HasTrait(toughDef))
			{
				traits.GainTrait(new Trait(toughDef, 0), true);
				addedTough = true;
			}
			float withTough = pawn.GetStatValue(stat, true, 0);
			if (addedTough)
			{
				traits.RemoveTrait(new Trait(toughDef, 0));
			}
			return baseValue.ToString("0.00") + "/" + scaled.ToString("0.00")
				+ "/" + withTough.ToString("0.00");
		}

		/// <summary>Mk-V 家务探针：敌人尸体搬运。</summary>
		private static void ProbeMkVChores(Map map, Pawn mkv, StringBuilder builder)
		{
			CompNannyBot comp = mkv.TryGetComp<CompNannyBot>();
			builder.Append(" corpseChore=")
				.Append(comp != null ? comp.GetPriority(RobotChore.CorpseDisposal) : -1)
				.Append(" craftChore=")
				.Append(comp != null ? comp.GetPriority(RobotChore.Craft) : -1);

			// 尸体处理：敌对派系尸体 + 允许尸体的存储区，应给出 HaulToCell。
			Faction hostile = null;
			foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
			{
				if (faction.def.humanlikeFaction && faction.HostileTo(Faction.OfPlayer))
				{
					hostile = faction;
					break;
				}
			}
			if (hostile == null || PawnKindDefOf.Pirate == null)
			{
				builder.Append(" corpseProbe=nofaction");
				return;
			}
			IntVec3 corpseCell = CellFinder.RandomClosewalkCellNear(
				mkv.Position,
				map,
				4,
				c => c.Standable(map) && c != mkv.Position && c.GetZone(map) == null);
			if (!corpseCell.IsValid)
			{
				builder.Append(" corpseProbe=nocell");
				return;
			}
			Pawn victim = PawnGenerator.GeneratePawn(PawnKindDefOf.Pirate, hostile);
			GenSpawn.Spawn(victim, corpseCell, map);
			victim.Kill(null);
			Corpse corpse = victim.Corpse;
			builder.Append(" enemyCorpse=").Append(corpse != null);
			if (corpse == null)
			{
				return;
			}
			builder.Append(" corpseFlags=")
				.Append(corpse.IsForbidden(mkv)).Append('/')
				.Append(corpse.IsInValidStorage()).Append('/')
				.Append(mkv.CanReserve(corpse, 1, -1, null, false)).Append('/')
				.Append(mkv.CanReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly)).Append('/')
				.Append(NannyBotUtility.IsWithinWorkArea(mkv, corpse.Position));
			IntVec3 storeCell = CellFinder.RandomClosewalkCellNear(
				corpseCell,
				map,
				6,
				c => c.Standable(map) && c.GetEdifice(map) == null
					&& c != corpseCell && c.GetZone(map) == null);
			if (storeCell.IsValid)
			{
				Zone_Stockpile pile = new Zone_Stockpile(
					StorageSettingsPreset.DefaultStockpile,
					map.zoneManager);
				map.zoneManager.RegisterZone(pile);
				pile.AddCell(storeCell);
				pile.settings.filter.SetAllow(ThingCategoryDefOf.Corpses, true);
				ThinkResult corpseResult = new JobGiver_NannyBotCorpseDisposal()
					.TryIssueJobPackage(mkv, new JobIssueParams());
				builder.Append(" corpseHaulJob=")
					.Append(corpseResult.Job?.def?.defName ?? "none")
					.Append(" pileAllows=").Append(pile.settings.filter.Allows(corpse));
			}
			corpse.Destroy(DestroyMode.Vanish);
		}

		/// <summary>
		/// 照护 / 种植家务探针：砍树（砍伐标记）、收玉米（种植区里成熟的玉米）、
		/// 送水（口渴囚徒 + 一瓶凉白开）。只记录机器人实际能拿到的 Job 名，不参与通过判定。
		/// </summary>
		private static void ProbeCareJobs(Map map, Pawn mkv, StringBuilder builder)
		{
			try
			{
				// 砍树：一棵树 + 砍伐标记 → Plants 应当给出 CutPlant。
				ThingDef treeDef = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_TreeOak");
				IntVec3 treeCell = CellFinder.RandomClosewalkCellNear(
					mkv.Position, map, 6, c => c.Standable(map) && c.GetPlant(map) == null && c.GetZone(map) == null);
				if (treeDef != null && treeCell.IsValid)
				{
					Plant tree = GenSpawn.Spawn(ThingMaker.MakeThing(treeDef), treeCell, map) as Plant;
					if (tree != null)
					{
						tree.Growth = 1f;
						map.designationManager.AddDesignation(new Designation(tree, DesignationDefOf.CutPlant));
						ThinkResult cutResult = new JobGiver_NannyBotPlants()
							.TryIssueJobPackage(mkv, new JobIssueParams());
						builder.Append(" cutJob=").Append(cutResult.Job?.def?.defName ?? "none");
						tree.Destroy(DestroyMode.Vanish);
					}
				}

				// 收玉米：种植区里一株成熟的玉米 → Plants 应当给出 Harvest。
				ThingDef cornDef = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Corn");
				IntVec3 zoneCell = CellFinder.RandomClosewalkCellNear(
					mkv.Position, map, 8, c => c.Standable(map) && c.GetZone(map) == null && c.GetPlant(map) == null);
				if (cornDef != null && zoneCell.IsValid)
				{
					Zone_Growing zone = new Zone_Growing(map.zoneManager);
					map.zoneManager.RegisterZone(zone);
					zone.AddCell(zoneCell);
					zone.SetPlantDefToGrow(cornDef);
					Plant corn = GenSpawn.Spawn(ThingMaker.MakeThing(cornDef), zoneCell, map) as Plant;
					if (corn != null)
					{
						corn.Growth = 1f;
						ThinkResult harvestResult = new JobGiver_NannyBotPlants()
							.TryIssueJobPackage(mkv, new JobIssueParams());
						builder.Append(" harvestJob=").Append(harvestResult.Job?.def?.defName ?? "none");
						corn.Destroy(DestroyMode.Vanish);
					}
					map.zoneManager.DeregisterZone(zone);
				}

				// 送水：口渴 15% 的囚徒 + 机器人旁边一瓶凉白开。
				ThingDef boiledDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_BoiledWater");
				IntVec3 prisonerCell = CellFinder.RandomClosewalkCellNear(
					mkv.Position, map, 6, c => c.Standable(map) && c != mkv.Position);
				if (boiledDef != null && prisonerCell.IsValid)
				{
					Pawn prisoner = PawnGenerator.GeneratePawn(PawnKindDefOf.Villager, null);
					GenSpawn.Spawn(prisoner, prisonerCell, map, WipeMode.Vanish);
					prisoner.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
					Hygiene.HygieneUtility.EnsureThirst(prisoner);
					Hygiene.Need_Thirst thirst = prisoner.needs?.TryGetNeed<Hygiene.Need_Thirst>();
					if (thirst != null)
					{
						thirst.CurLevel = 0.15f;
					}
					Thing stack = ThingMaker.MakeThing(boiledDef);
					stack.stackCount = 2;
					GenSpawn.Spawn(stack, mkv.Position, map, WipeMode.Vanish);
					Job direct = Hygiene.BringWaterUtility.TryMakeJob(mkv, prisoner);
					ThinkResult waterResult = new JobGiver_NannyBotWater()
						.TryIssueJobPackage(mkv, new JobIssueParams());
					builder.Append(" waterJob=").Append(direct?.def?.defName ?? "none")
						.Append(" waterGiver=").Append(waterResult.Job?.def?.defName ?? "none");
					stack.Destroy(DestroyMode.Vanish);
					prisoner.Destroy(DestroyMode.Vanish);
				}
			}
			catch (Exception exception)
			{
				builder.Append(" careProbe=exception:").Append(exception.GetType().Name);
			}
		}

		/// <summary>
		/// 双持装备探针：第二把远程进副手、第三把武器顶掉非专属武器、专属武器保留；
		/// 远程 + 近战时副手近战动词可用，双远程时副手动词是 Verb_Shoot。
		/// </summary>
		private static void ProbeMkVDualWield(Pawn mkv, StringBuilder builder)
		{
			RoboticsSettings settings = NannyBotUtility.Settings;
			bool oldDual = settings.mkvDualWield;
			settings.mkvDualWield = true;
			try
			{
				ThingDef pistolDef = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_Autopistol");
				ThingDef swordDef = DefDatabase<ThingDef>.GetNamedSilentFail("MeleeWeapon_LongSword");
				builder.Append(" dualBase=").Append(MkVLoadout.HeldWeapons(mkv).Count);
				if (pistolDef == null || swordDef == null)
				{
					builder.Append(" dualProbe=nodef");
					return;
				}

				// 第二把远程：专属的电弧发射器留在主手，手枪进副手。
				mkv.equipment.AddEquipment(ThingMaker.MakeThing(pistolDef) as ThingWithComps);
				builder.Append(" dualRanged=").Append(MkVLoadout.HeldWeapons(mkv).Count)
					.Append(':').Append(mkv.equipment.Primary?.def?.defName ?? "none")
					.Append('/').Append(MkVLoadout.Offhand(mkv)?.def?.defName ?? "none");

				// 第三把是近战：专属武器保留，非专属的手枪被丢下换成长剑副手。
				mkv.equipment.AddEquipment(ThingMaker.MakeThing(swordDef) as ThingWithComps);
				builder.Append(" dualMelee=").Append(MkVLoadout.HeldWeapons(mkv).Count)
					.Append(':').Append(mkv.equipment.Primary?.def?.defName ?? "none")
					.Append('/').Append(MkVLoadout.Offhand(mkv)?.def?.defName ?? "none")
					.Append(":exclusive=")
					.Append(MkVLoadout.IsExclusive(mkv.equipment.Primary?.def));

				ThingWithComps meleeOffhand = MkVLoadout.Offhand(mkv);
				Verb meleeVerb = meleeOffhand?.TryGetComp<CompEquippable>()?.AllVerbs?
					.FirstOrDefault(v => v != null && v.IsMeleeAttack);
				builder.Append(" offhandMelee=").Append(meleeVerb != null);

				// 再装一把枪：近战被换掉，副手变回枪，主手开火时它会跟着打。
				// 这里走原版 JobDriver_Equip 的同一顺序（先 MakeRoomFor 再 AddEquipment），
				// 确认 MakeRoomFor 不会把主手丢到地上。
				ThingWithComps incoming = ThingMaker.MakeThing(pistolDef) as ThingWithComps;
				mkv.equipment.MakeRoomFor(incoming);
				mkv.equipment.AddEquipment(incoming);
				ThingWithComps rangedOffhand = MkVLoadout.Offhand(mkv);
				Verb offhandVerb = rangedOffhand?.TryGetComp<CompEquippable>()?.PrimaryVerb;
				builder.Append(" dualRangedAgain=").Append(MkVLoadout.HeldWeapons(mkv).Count)
					.Append(':').Append(mkv.equipment.Primary?.def?.defName ?? "none")
					.Append('/').Append(rangedOffhand?.def?.defName ?? "none")
					.Append(" offhandVerb=")
					.Append(offhandVerb?.GetType().Name ?? "none");

				// 真打一发：走和跟随开火相同的入口，确认副手动词字段/开火流程没有异常。
				IntVec3 aim = mkv.Position + IntVec3.East * 6;
				if (offhandVerb != null && aim.InBounds(mkv.Map))
				{
					int before = offhandVerb.LastShotTick;
					Patch_MkVDualWield.Fire(offhandVerb, new LocalTargetInfo(aim));
					builder.Append(" offhandFired=").Append(offhandVerb.LastShotTick != before);
				}
			}
			finally
			{
				settings.mkvDualWield = oldDual;
			}
		}

		/// <summary>
		/// 背包备用武器探针：按真实质量判断能不能存、存进背包后换到手上、再收回背包。
		/// </summary>
		private static void ProbeMkVBackupWeapon(Pawn mkv, StringBuilder builder)
		{
			ThingDef pistolDef = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_Autopistol");
			ThingWithComps spare = pistolDef == null
				? null
				: ThingMaker.MakeThing(pistolDef) as ThingWithComps;
			if (spare == null)
			{
				builder.Append(" spare=nodef");
				return;
			}
			bool canStore = MkVLoadout.CanStoreAsSpare(mkv, spare, out float missing);
			builder.Append(" spareCanStore=").Append(canStore)
				.Append(":missing=").Append(missing.ToString("0.#"));
			if (!canStore || !mkv.inventory.innerContainer.TryAddOrTransfer(spare))
			{
				spare.Destroy(DestroyMode.Vanish);
				return;
			}
			builder.Append(" spareInBag=").Append(MkVLoadout.SpareWeapons(mkv).Count)
				.Append(" free=").Append(MassUtility.FreeSpace(mkv).ToString("0.#"));
			builder.Append(" spareEquipped=").Append(MkVLoadout.TryEquipSpare(mkv, spare))
				.Append(" bag=").Append(MkVLoadout.SpareWeapons(mkv).Count);
			builder.Append(" spareStashed=").Append(MkVLoadout.TryStashToBag(mkv, spare))
				.Append(" bag=").Append(MkVLoadout.SpareWeapons(mkv).Count);
			if (mkv.inventory.innerContainer.Contains(spare))
			{
				spare.Destroy(DestroyMode.Vanish);
			}
		}

		/// <summary>
		/// 电弧刃命中特效探针：直接调用麻痹/连锁入口，确认目标倒地、挂上 hediff，
		/// 并且 5 格内的另一个敌人会被连锁。
		/// </summary>
		private static void ProbeArcBladeEffects(Pawn mkv, Map map, StringBuilder builder)
		{
			HediffDef def = NannyBotDefOf.CK_ArcBladeParalysis;
			builder.Append(" bladeHediffDef=").Append(def != null);
			if (def == null)
			{
				return;
			}

			// 面板数值探针：把伤害临时改成 77，确认同步到了 ThingDef.tools（近战结算和信息卡都读它）。
			RoboticsSettings bladeSettings = NannyBotUtility.Settings;
			float oldDamage = bladeSettings.mkvArcBladeDamage;
			bladeSettings.mkvArcBladeDamage = 77f;
			MkVArcBlade.ApplySettings();
			ThingDef bladeDef = NannyBotDefOf.CK_ArcBlade;
			float syncedDamage = bladeDef?.tools != null && bladeDef.tools.Count > 0
				? bladeDef.tools[0].power
				: -1f;
			builder.Append(" bladeDamage=").Append(syncedDamage.ToString("0"));
			bladeSettings.mkvArcBladeDamage = oldDamage;
			MkVArcBlade.ApplySettings();

			Faction hostile = null;
			foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
			{
				if (faction.def.humanlikeFaction && faction.HostileTo(Faction.OfPlayer))
				{
					hostile = faction;
					break;
				}
			}
			if (hostile == null || PawnKindDefOf.Pirate == null)
			{
				builder.Append(" bladeProbe=nofaction");
				return;
			}

			IntVec3 cell = CellFinder.RandomClosewalkCellNear(
				mkv.Position, map, 4,
				c => c.Standable(map) && c != mkv.Position && c.GetEdifice(map) == null);
			if (!cell.IsValid)
			{
				builder.Append(" bladeProbe=nocell");
				return;
			}
			IntVec3 chainedCell = CellFinder.RandomClosewalkCellNear(
				cell, map, 2,
				c => c != cell && c.Standable(map) && c.GetEdifice(map) == null);

			int ticks = Patch_MkVArcBladeEffects.HoursToTicks(10f);
			Pawn victim = PawnGenerator.GeneratePawn(PawnKindDefOf.Pirate, hostile);
			GenSpawn.Spawn(victim, cell, map);
			Pawn chained = null;
			if (chainedCell.IsValid)
			{
				chained = PawnGenerator.GeneratePawn(PawnKindDefOf.Pirate, hostile);
				GenSpawn.Spawn(chained, chainedCell, map);
			}

			bool before = victim.Downed;
			Patch_MkVArcBladeEffects.ApplyParalysis(victim, ticks);
			Hediff applied = victim.health.hediffSet.GetFirstHediffOfDef(def);
			builder.Append(" bladeParalysis=").Append(before).Append("->").Append(victim.Downed)
				.Append(":hediff=").Append(applied != null)
				.Append(":ticks=")
				.Append(applied?.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0);

			if (chained != null)
			{
				bool chainedBefore = chained.Downed;
				Patch_MkVArcBladeEffects.ApplyChain(mkv, victim, 5f, ticks);
				builder.Append(" bladeChain=").Append(chainedBefore).Append("->").Append(chained.Downed)
					.Append(":hediff=").Append(chained.health.hediffSet.HasHediff(def));
				chained.Destroy(DestroyMode.Vanish);
			}
			victim.Destroy(DestroyMode.Vanish);
		}

		/// <summary>
		/// 携带能力探针：把 Mk-V 的设置临时改成 300，确认 GetStatValue 立刻跟着变（说明补丁生效、
		/// 没有被 StatPart_BodySize 再乘一次），然后还原。
		/// </summary>
		private static string CarrySettingProbe(Pawn pawn)
		{
			RoboticsSettings settings = NannyBotUtility.Settings;
			float oldValue = settings.mkvCarryCapacity;
			settings.mkvCarryCapacity = 300f;
			float value = pawn.GetStatValue(StatDefOf.CarryingCapacity);
			settings.mkvCarryCapacity = oldValue;
			return value.ToString("0");
		}

		/// <summary>开局赠送数量探针：临时填 9，确认 Validate 夹回 5 台后还原。</summary>
		private static string StartMkVCountProbe()
		{
			RoboticsSettings settings = NannyBotUtility.Settings;
			int original = settings.startWithMkVCount;
			settings.startWithMkVCount = 9;
			settings.Validate();
			int clamped = settings.startWithMkVCount;
			settings.startWithMkVCount = original;
			return original + "/9->" + clamped;
		}

		public static void RunIfRequested(Game game)
		{
			if (ran || game == null || !GenCommandLine.CommandLineArgPassed(CommandLineKey))
			{
				return;
			}
			ran = true;

			try
			{
				Map map = Find.AnyPlayerHomeMap ?? (game.Maps.Count > 0 ? game.Maps[0] : null);
				if (map == null)
				{
					ModLog.Warn("Robotics", "selftest skipped: no map.");
					return;
				}

				IntVec3 cell = CellFinder.RandomClosewalkCellNear(
					map.Center,
					map,
					12,
					c => c.Standable(map) && !c.Fogged(map));
				if (!cell.IsValid)
				{
					cell = map.Center;
				}

				Thing station = ThingMaker.MakeThing(NannyBotDefOf.CK_NannyBotStation);
				GenSpawn.Spawn(station, cell, map, Rot4.South);
				CompNannyBotStation comp = station.TryGetComp<CompNannyBotStation>();
				if (comp == null)
				{
					ModLog.Error("Robotics", "selftest failed: station comp missing.");
					return;
				}

				bool spawned = comp.TrySpawnRobot(true);
				Pawn robot = comp.robot;
				StringBuilder builder = new StringBuilder();
				builder.Append("selftest station=").Append(station.Position)
					.Append(" spawned=").Append(spawned)
					.Append(" robot=").Append(robot != null);
				if (robot != null)
				{
					builder.Append(" def=").Append(robot.def.defName)
						.Append(" kind=").Append(robot.kindDef?.defName)
						.Append(" name=").Append(robot.Name?.ToStringFull)
						.Append(" faction=").Append(robot.Faction?.def?.defName)
						.Append(" thinkTree=").Append(robot.RaceProps.thinkTreeMain?.defName)
						.Append(" spawned=").Append(robot.Spawned)
						.Append(" needs=").Append(robot.needs?.AllNeeds?.Count ?? 0)
						.Append(" energy=").Append(NannyBotUtility.Energy(robot)?.CurLevel.ToString("0.00") ?? "none")
						.Append(" moving=").Append(robot.health.capacities.GetLevel(PawnCapacityDefOf.Moving).ToString("0.00"))
						.Append(" manipulation=").Append(robot.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation).ToString("0.00"))
						.Append(" consciousness=").Append(robot.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness).ToString("0.00"))
						.Append(" carry=").Append(robot.GetStatValue(StatDefOf.CarryingCapacity).ToString("0"))
						.Append(" playerSettings=").Append(robot.playerSettings != null);

					int humanlikesWithEnergy = 0;
					foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
					{
						if (pawn.RaceProps.Humanlike && NannyBotUtility.Energy(pawn) != null)
						{
							humanlikesWithEnergy++;
						}
					}
					builder.Append(" humanlikesWithEnergy=").Append(humanlikesWithEnergy);

					CompNannyBot botComp = NannyBotUtility.BotComp(robot);
					builder.Append(" priorities=");
					for (int i = 0; i < RobotChores.Count; i++)
					{
						if (i > 0)
						{
							builder.Append(',');
						}
						builder.Append(botComp?.GetPriority((RobotChore)i) ?? -1);
					}
					if (botComp != null)
					{
						botComp.SetPriority(RobotChore.Sweep, 1);
						botComp.SetPriority(RobotChore.Scrub, 8);
						builder.Append(" sweepSched=")
							.Append(new JobGiver_NannyBotSweep().GetPriority(robot).ToString("0"))
							.Append(" scrubSched=")
							.Append(new JobGiver_NannyBotScrub().GetPriority(robot).ToString("0"))
							.Append(" rescueSched=")
							.Append(new JobGiver_NannyBotRescue().GetPriority(robot).ToString("0"));
						botComp.SetPriority(RobotChore.Sweep, 3);
						botComp.SetPriority(RobotChore.Scrub, 3);
					}

					IntVec3 filthCell = IntVec3.Invalid;
					Filth filth = null;
					bool madeFilth = false;
					for (int dx = -30; dx <= 30 && !madeFilth; dx++)
					{
						for (int dz = -30; dz <= 30 && !madeFilth; dz++)
						{
							IntVec3 candidate = cell + new IntVec3(dx, 0, dz);
							if (!candidate.InBounds(map) || !candidate.Standable(map))
							{
								continue;
							}
							TerrainDef terrain = map.terrainGrid.TerrainAt(candidate);
							if (!FilthMaker.TerrainAcceptsFilth(
								terrain,
								ThingDefOf.Filth_Dirt,
								FilthSourceFlags.Pawn))
							{
								continue;
							}
							filthCell = candidate;
							madeFilth = FilthMaker.TryMakeFilth(
								filthCell,
								map,
								ThingDefOf.Filth_Dirt,
								out filth,
								1,
								FilthSourceFlags.Pawn);
						}
					}
					builder.Append(" filthCell=").Append(filthCell)
						.Append(" terrain=").Append(filthCell.IsValid
							? map.terrainGrid.TerrainAt(filthCell).defName
							: "none")
						.Append(" madeFilth=").Append(madeFilth);
					if (madeFilth)
					{
						// 新污渍默认 600 tick 内不可清理；测试里直接把年龄拨老。
						AccessTools.FieldRefAccess<Filth, int>("growTick")(filth) =
							Find.TickManager.TicksGame - 1000;
						ThinkResult cleanResult = new JobGiver_NannyBotScrub()
							.TryIssueJobPackage(robot, new JobIssueParams());
						builder.Append(" cleanJob=")
							.Append(cleanResult.Job?.def?.defName ?? "none");
					}

					IntVec3 stockCell = CellFinder.RandomClosewalkCellNear(
						cell,
						map,
						8,
						c => c.Standable(map) && c != cell);
					IntVec3 itemCell = CellFinder.RandomClosewalkCellNear(
						cell,
						map,
						8,
						c => c.Standable(map) && c != cell && c != stockCell);
					if (stockCell.IsValid && itemCell.IsValid)
					{
						Zone_Stockpile stockpile = new Zone_Stockpile(
							StorageSettingsPreset.DefaultStockpile,
							map.zoneManager);
						map.zoneManager.RegisterZone(stockpile);
						stockpile.AddCell(stockCell);
						Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
						steel.stackCount = 25;
						GenSpawn.Spawn(steel, itemCell, map);
						ThinkResult haulResult = new JobGiver_NannyBotHaul()
							.TryIssueJobPackage(robot, new JobIssueParams());
						builder.Append(" haulJob=")
							.Append(haulResult.Job?.def?.defName ?? "none");
					}

					IntVec3 fireCell = cell + new IntVec3(1, 0, 0);
					if (fireCell.InBounds(map))
					{
						Fire fire = ThingMaker.MakeThing(ThingDefOf.Fire) as Fire;
						if (fire != null)
						{
							GenSpawn.Spawn(fire, fireCell, map);
							ThinkResult fireResult = new JobGiver_NannyBotFirefight()
								.TryIssueJobPackage(robot, new JobIssueParams());
							builder.Append(" fireJob=")
								.Append(fireResult.Job?.def?.defName ?? "none");
							if (!fire.Destroyed)
							{
								fire.Destroy();
							}
						}
					}

					IntVec3 generatorCell = FindFreeRect(map, cell, 12, 2, 2);
					if (generatorCell.IsValid)
					{
						Thing generator = ThingMaker.MakeThing(ThingDefOf.WoodFiredGenerator);
						GenSpawn.Spawn(generator, generatorCell, map, Rot4.North);
						generator.SetFaction(Faction.OfPlayer);
						CompRefuelable fuel = generator.TryGetComp<CompRefuelable>();
						if (fuel != null && fuel.Fuel > 0f)
						{
							fuel.ConsumeFuel(fuel.Fuel);
						}
						IntVec3 woodCell = CellFinder.RandomClosewalkCellNear(
							generatorCell,
							map,
							4,
							c => c.Standable(map) && c.GetEdifice(map) == null);
						Thing wood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
						wood.stackCount = 75;
						GenSpawn.Spawn(wood, woodCell, map);
						ThinkResult refuelResult = new JobGiver_NannyBotRefuel()
							.TryIssueJobPackage(robot, new JobIssueParams());
						builder.Append(" refuelJob=")
							.Append(refuelResult.Job?.def?.defName ?? "none");
						wood.Destroy();
						generator.Destroy();
					}

					RoboticsSettings settings = NannyBotUtility.Settings;
					builder.Append(" batteryCap=").Append(settings.batteryCapacity)
						.Append(" chargeThreshold=").Append(settings.chargeThreshold.ToString("0.00"))
						.Append(" workRadius=").Append(settings.workRadius.ToString("0"))
						.Append(" mkvStartCount=").Append(StartMkVCountProbe());

					if (botComp != null)
					{
						float durabilityBeforeDamage = botComp.Durability;
						float healthBeforeDamage = robot.health.summaryHealth.SummaryHealthPercent;
						DamageWorker.DamageResult damageResult =
							robot.TakeDamage(new DamageInfo(DamageDefOf.Cut, 20f));
						builder.Append(" damageDurability=")
							.Append(durabilityBeforeDamage.ToString("0"))
							.Append("->")
							.Append(botComp.Durability.ToString("0"))
							.Append(" health=")
							.Append(healthBeforeDamage.ToString("0.00"))
							.Append("->")
							.Append(robot.health.summaryHealth.SummaryHealthPercent.ToString("0.00"))
							.Append(" damageDealt=")
							.Append(damageResult.totalDamageDealt.ToString("0.00"))
							.Append(" godMode=")
							.Append(DebugSettings.godMode);

						botComp.durability = 500f;
						Pawn colonist = null;
						foreach (Pawn candidate in map.mapPawns.FreeColonistsSpawned)
						{
							colonist = candidate;
							break;
						}
						WorkGiver_RepairNannyBot repair = new WorkGiver_RepairNannyBot();
						bool canRepair = colonist != null && repair.HasJobOnThing(colonist, robot);
						Job repairJob = canRepair ? repair.JobOnThing(colonist, robot) : null;
						builder.Append(" durability=").Append(botComp.Durability.ToString("0"))
							.Append(" repairJob=").Append(repairJob?.def?.defName ?? "none");

						Need_RobotEnergy preChargeEnergy = NannyBotUtility.Energy(robot);
						if (preChargeEnergy != null)
						{
							float oldLevel = preChargeEnergy.CurLevel;
							preChargeEnergy.CurLevel = 100f;
							builder.Append(" chargePriority=")
								.Append(new JobGiver_NannyBotCharge().GetPriority(robot).ToString("0"));
							preChargeEnergy.CurLevel = oldLevel;
						}

						botComp.Notify_RepairStarted();
						builder.Append(" canWorkWhileRepairing=")
							.Append(NannyBotUtility.CanWork(robot));
						Need_RobotEnergy lowEnergy = NannyBotUtility.Energy(robot);
						if (lowEnergy != null)
						{
							lowEnergy.CurLevel = 100f;
							ThinkResult chargeWhileRepair = new JobGiver_NannyBotCharge()
								.TryIssueJobPackage(robot, new JobIssueParams());
							builder.Append(" chargeWhileRepairing=")
								.Append(chargeWhileRepair.Job?.def?.defName ?? "none");
						}
						botComp.Notify_RepairFinished();
					}
				}
				ProbeMkVBay(map, builder);
				ModLog.Force("Robotics", builder.ToString());

				if (GenCommandLine.CommandLineArgPassed("-colonykit-robotics-ui-test"))
				{
					UiAutomationService.Register(new UiScenario(
						"90-robotics-work",
						() => new MainTabWindow_NannyBots
						{
							def = DefDatabase<MainButtonDef>.GetNamed("CK_RobotManagement")
						}));
					UiAutomationService.Register(new UiScenario(
						"91-robotics-status",
						() =>
						{
							MainTabWindow_NannyBots window = new MainTabWindow_NannyBots
							{
								def = DefDatabase<MainButtonDef>.GetNamed("CK_RobotManagement")
							};
							window.SelectTab(1);
							return window;
						}));
					if (robot != null)
					{
						UiAutomationService.Register(new UiScenario(
							"92-robotics-inspect",
							() =>
							{
								CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(robot));
								return null;
							}));
					}
					UiAutomationService.RunAll();
				}
			}
			catch (Exception exception)
			{
				ModLog.Error("Robotics", "selftest threw", exception);
			}
		}

		/// <summary>
		/// AI 想法要能从 PlayLog 走进「日志」页的渲染列表，并且只出现在这台机器人自己的日志里。
		/// 走的是 ITab_Pawn_Log 实际使用的 GenerateLogLinesFor，所以 Concerns 过滤和文案渲染一起验证。
		/// </summary>
		private static void ProbeThoughtLog(Map map, Pawn mkv, StringBuilder builder)
		{
			const string thought = "自检想法：我在检查日志页。";
			RoboticsSettings settings = NannyBotUtility.Settings;
			bool original = settings.aiThoughtLog;
			try
			{
				LogEntryDef def = DefDatabase<LogEntryDef>.GetNamedSilentFail("CK_MkVThought");
				builder.Append(" aiLogDef=").Append(def != null);

				settings.aiThoughtLog = true;
				PlayLogEntry_MkVThought entry = new PlayLogEntry_MkVThought(mkv, thought, def);
				Find.PlayLog.Add(entry);

				Pawn other = null;
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (pawn != mkv)
					{
						other = pawn;
						break;
					}
				}
				builder.Append(" aiLogConcerns=").Append(entry.Concerns(mkv))
					.Append(" aiLogOtherConcerns=").Append(other != null && entry.Concerns(other));

				StringBuilder rendered = new StringBuilder();
				bool found = false;
				foreach (ITab_Pawn_Log_Utility.LogLineDisplayable line
					in ITab_Pawn_Log_Utility.GenerateLogLinesFor(mkv, false, true, true, 300))
				{
					if (line.Matches(entry))
					{
						line.AppendTo(rendered);
						found = true;
						break;
					}
				}
				builder.Append(" aiLogLine=").Append(found)
					.Append(" aiLogText=").Append(rendered.ToString().Contains(thought));
			}
			catch (Exception exception)
			{
				builder.Append(" aiLogError=").Append(exception.Message);
			}
			finally
			{
				settings.aiThoughtLog = original;
			}
		}

		/// <summary>
		/// 医疗床休养要在真实 Job / 寻路 / Toil 上跑通：机器人走到床边、躺下、耐久按速率涨。
		/// 在 Mk-V 旁边放一张医院床，直接 StartJob，然后手动推 tick（PatherTick + JobTrackerTick），
		/// 结束后停任务、拆床、还原耐久。
		/// </summary>
		private static string ProbeBedRest(Map map, Pawn mkv)
		{
			CompNannyBot comp = mkv?.TryGetComp<CompNannyBot>();
			ThingDef hospitalBed = DefDatabase<ThingDef>.GetNamedSilentFail("HospitalBed");
			if (comp == null || hospitalBed == null)
			{
				return "bedRest=none";
			}
			Building_Bed bed = null;
			float oldDurability = comp.durability;
			try
			{
				IntVec3 cell = FindFreeRect(map, mkv.Position, 8, 1, 2);
				if (!cell.IsValid)
				{
					return "bedRest=nocell";
				}
				bed = (Building_Bed)ThingMaker.MakeThing(hospitalBed);
				GenSpawn.Spawn(bed, cell, map, Rot4.South);
				// 玩家造的床有阵营；没有阵营的床不会进 listerBuildings.allBuildingsColonist，
				// JobGiver 就找不到它（不是玩法问题，是自检里 MakeThing 的默认状态）。
				bed.SetFaction(Faction.OfPlayer);
				comp.durability = 1000f;
				float before = comp.durability;
				RoboticsSettings settings = NannyBotUtility.Settings;
				float oldBedRate = settings.robotBedRestRate;
				settings.robotBedRestRate = 600f;
				try
				{
				if (mkv.Drafted && mkv.drafter != null)
				{
					mkv.drafter.Drafted = false;
				}
				Job job = JobMaker.MakeJob(NannyBotDefOf.CK_RobotRestInBed, bed);
				IntVec3 slot = RestUtility.GetBedSleepingSlotPosFor(mkv, bed);
				bool canReach = mkv.CanReach(slot, PathEndMode.OnCell, Danger.Deadly);
				string giver = new JobGiver_NannyBotBedRest()
					.TryIssueJobPackage(mkv, new JobIssueParams()).Job?.def?.defName ?? "none";
				// 直接把机器人放到床位上：验证「躺下 → 按速率恢复」这一段，
				// 寻路本身交给原版补丁/正常 tick 循环（这里只记录 CanReach）。
				mkv.pather?.StopDead();
				mkv.Position = slot;
				mkv.jobs.StartJob(job, JobCondition.InterruptForced);
				int baseTicks = Find.TickManager.TicksGame;
				for (int i = 0; i < 600; i++)
				{
					Find.TickManager.DebugSetTicksGame(baseTicks + i);
					mkv.pather?.PatherTick();
					mkv.jobs?.JobTrackerTick();
					// 真实 tick 里 Pawn.Tick 每 tick 还会调 JobTrackerTickInterval → DriverTickInterval，
					// 休息的恢复量就在 tickIntervalAction 里；这里补上，否则只跑 DriverTick 永远不涨。
					mkv.jobs?.curDriver?.DriverTickInterval(1);
				}
				float after = comp.durability;
				bool jobRunning = mkv.CurJobDef == NannyBotDefOf.CK_RobotRestInBed;
				bool inBed = mkv.GetPosture().InBed();

				// 没有床时必须老实返回「没活可干」，不能把机器人钉在原地。
				mkv.jobs?.StopAll(false);
				if (bed != null && !bed.Destroyed)
				{
					bed.Destroy(DestroyMode.Vanish);
				}
				comp.durability = 1000f;
				string giverNoBed = new JobGiver_NannyBotBedRest()
					.TryIssueJobPackage(mkv, new JobIssueParams()).Job?.def?.defName ?? "none";

				return "bedRestSpawned=" + (bed != null)
					+ " bedRestCanReach=" + canReach
					+ " bedRestRate=" + oldBedRate.ToString("0")
					+ " bedRestEnergy=" + (NannyBotUtility.Energy(mkv)?.CurLevelPercentage.ToString("0.00") ?? "none")
					+ " bedRestGiver=" + giver
					+ " bedRestGiverNoBed=" + giverNoBed
					+ " bedRestJobRunning=" + jobRunning
					+ " bedRestInBed=" + inBed
					+ " bedRestDelta=" + (after - before).ToString("0.00")
					+ " bedRestDurability=" + after.ToString("0")
					+ " bedRestDriver=" + (mkv.jobs?.curDriver?.GetType().Name ?? "none")
					+ " bedRestDist=" + mkv.Position.DistanceTo(slot).ToString("0.0");
				}
				finally
				{
					settings.robotBedRestRate = oldBedRate;
				}
			}
			catch (Exception exception)
			{
				return "bedRestError=" + exception.GetType().Name + ":" + exception.Message;
			}
			finally
			{
				try
				{
					mkv?.jobs?.StopAll(false);
				}
				catch
				{
					// 自检里停任务失败不掩盖真正的结果。
				}
				if (bed != null && !bed.Destroyed)
				{
					bed.Destroy(DestroyMode.Vanish);
				}
				comp.durability = oldDurability;
			}
		}

		/// <summary>
		/// 技能热情：3 大 + 3~6 小（可能变异）、射击/格斗必大火、艺术/社交无热情；
		/// 顺手验一下 Mk-V 的技能遗忘减半补丁（10 级每 200 tick 原版 -0.1，Mk-V 应为 -0.05）。
		/// </summary>
		private static string PassionProbe(Pawn mkv)
		{
			if (mkv?.skills == null)
			{
				return "passions=none";
			}
			int major = 0;
			int minor = 0;
			foreach (SkillDef def in DefDatabase<SkillDef>.AllDefsListForReading)
			{
				SkillRecord record = mkv.skills.GetSkill(def);
				if (record == null)
				{
					continue;
				}
				if (record.passion == Passion.Major)
				{
					major++;
				}
				else if (record.passion == Passion.Minor)
				{
					minor++;
				}
			}
			SkillRecord artistic = mkv.skills.GetSkill(SkillDefOf.Artistic);
			SkillRecord social = mkv.skills.GetSkill(SkillDefOf.Social);
			SkillRecord shooting = mkv.skills.GetSkill(SkillDefOf.Shooting);
			SkillRecord melee = mkv.skills.GetSkill(SkillDefOf.Melee);

			// 遗忘：先在没有「超凡记忆」的情况下量（应 -0.05），再临时加上特质量一次（应 -0.025，验证叠加）。
			float decay = MeasureSkillDecay(social);
			float decayWithGreatMemory = decay;
			TraitSet traits = mkv.story?.traits;
			TraitDef greatMemoryDef = TraitDefOf.GreatMemory;
			Trait existing = traits?.GetTrait(greatMemoryDef);
			bool hadGreatMemory = existing != null;
			try
			{
				if (hadGreatMemory)
				{
					traits.RemoveTrait(existing);
				}
				decay = MeasureSkillDecay(social);
				if (traits != null && greatMemoryDef != null)
				{
					traits.GainTrait(new Trait(greatMemoryDef, 0));
					decayWithGreatMemory = MeasureSkillDecay(social);
				}
			}
			finally
			{
				if (traits != null && greatMemoryDef != null)
				{
					Trait added = traits.GetTrait(greatMemoryDef);
					if (added != null)
					{
						traits.RemoveTrait(added);
					}
				}
				if (hadGreatMemory)
				{
					traits.GainTrait(existing);
				}
			}

			return "passionMajor=" + major
				+ " passionMinor=" + minor
				+ " passionTotal=" + (major + minor)
				+ " passionShootMajor=" + (shooting?.passion == Passion.Major)
				+ " passionMeleeMajor=" + (melee?.passion == Passion.Major)
				+ " passionArtistic=" + (artistic?.passion.ToString() ?? "none")
				+ " passionSocial=" + (social?.passion.ToString() ?? "none")
				+ " skillDecay=" + decay.ToString("0.000")
				+ " skillDecayGreatMemory=" + decayWithGreatMemory.ToString("0.000");
		}

		/// <summary>把社交设成 10 级、100 经验，跑一次原版 Interval（含 Mk-V 减半补丁），返回经验变化。</summary>
		private static float MeasureSkillDecay(SkillRecord skill)
		{
			if (skill == null)
			{
				return 0f;
			}
			skill.levelInt = 10;
			skill.xpSinceLastLevel = 100f;
			skill.Interval();
			return skill.xpSinceLastLevel - 100f;
		}

		/// <summary>
		/// 野外自修复要在真实 tick 上真的涨耐久、关掉就不再涨、非 Mk-V 不该涨。
		/// 这里直接推进 TickManager 的 tick 并调用 CompTick，走的是游戏里同一条路径，
		/// 只是把 500 tick 压缩成一次循环；结束后恢复设置与原耐久（tick 不回退）。
		/// </summary>
		private static string SelfRepairProbe(Pawn mkv)
		{
			CompNannyBot comp = mkv?.TryGetComp<CompNannyBot>();
			if (comp == null)
			{
				return "selfRepair=none";
			}

			Pawn other = null;
			foreach (Pawn candidate in mkv.Map.mapPawns.AllPawnsSpawned)
			{
				if (candidate != mkv && NannyBotUtility.IsNannyBot(candidate))
				{
					other = candidate;
					break;
				}
			}

			RoboticsSettings settings = NannyBotUtility.Settings;
			bool oldEnabled = settings.mkvSelfRepair;
			float oldRate = settings.mkvSelfRepairRate;
			float oldDurability = comp.durability;
			CompNannyBot otherComp = other?.TryGetComp<CompNannyBot>();
			float otherDurability = otherComp?.durability ?? 0f;
			int oldTicks = Find.TickManager.TicksGame;
			try
			{
				// 旁边站着殖民者、能正常接维修任务，自修复照样要在「没人来」的空档里生效。
				settings.mkvSelfRepair = true;
				settings.mkvSelfRepairRate = 300f;
				comp.durability = 1000f;
				bool canRepair = NannyBotUtility.CanBeRepairedOnMap(mkv);
				float before = comp.durability;
				for (int i = 0; i < 500; i++)
				{
					Find.TickManager.DebugSetTicksGame(oldTicks + i);
					comp.CompTick();
				}
				float afterOn = comp.durability;

				settings.mkvSelfRepair = false;
				for (int i = 0; i < 500; i++)
				{
					Find.TickManager.DebugSetTicksGame(oldTicks + 500 + i);
					comp.CompTick();
				}
				float afterOff = comp.durability;

				// 普通保姆机器人（Mk-I）不该走这条自修复。
				float otherAfter = 0f;
				if (otherComp != null)
				{
					settings.mkvSelfRepair = true;
					otherComp.durability = 500f;
					float otherBefore = otherComp.durability;
					for (int i = 0; i < 500; i++)
					{
						Find.TickManager.DebugSetTicksGame(oldTicks + 1000 + i);
						otherComp.CompTick();
					}
					otherAfter = otherComp.durability - otherBefore;
				}

				return "selfRepairEnabled=" + oldEnabled
					+ " selfRepairCanRepairOnMap=" + canRepair
					+ " selfRepairDelta=" + (afterOn - before).ToString("0.00")
					+ " selfRepairOffDelta=" + (afterOff - afterOn).ToString("0.00")
					+ " selfRepairOtherDelta=" + otherAfter.ToString("0.00")
					+ " selfRepairTicks=" + (Find.TickManager.TicksGame - oldTicks);
			}
			catch (Exception exception)
			{
				return "selfRepairError=" + exception.GetType().Name + ":" + exception.Message;
			}
			finally
			{
				settings.mkvSelfRepair = oldEnabled;
				settings.mkvSelfRepairRate = oldRate;
				comp.durability = oldDurability;
				if (otherComp != null)
				{
					otherComp.durability = otherDurability;
				}
			}
		}

		/// <summary>
		/// AI 升级的确定性探针：范围武器识别、威胁分排序、敌袭预警、组装/研究/守卫候选的前置。
		/// 只造几个临时假人，探完就销毁，不写回设置。
		/// </summary>
		private static void ProbeMkVThreat(Map map, Pawn mkv, StringBuilder builder)
		{
			Pawn molotov = null;
			Pawn rifle = null;
			Pawn hostile = null;
			Letter letter = null;
			try
			{
				// 守卫候选要在造「殖民者」假人之前测，否则 AnyColonistPresent 会被自己造的假人干扰。
				builder.Append(" aiGuardAlone=").Append(!NannyBotUtility.AnyColonistPresent(map))
					.Append(" aiGuardJob=")
					.Append(new JobGiver_MkVGuard().TryIssueJobPackage(mkv, new JobIssueParams()).Job != null);

				molotov = SpawnArmedPawn(map, mkv, Faction.OfPlayer, "Weapon_GrenadeMolotov");
				rifle = SpawnArmedPawn(map, mkv, Faction.OfPlayer, "Gun_AssaultRifle");
				MkVThreat.Profile molotovProfile = MkVThreat.Describe(molotov);
				MkVThreat.Profile rifleProfile = MkVThreat.Describe(rifle);
				builder.Append(" aiThreatMolotov=").Append(molotovProfile.area)
					.Append(" aiThreatRifle=").Append(rifleProfile.area)
					.Append(" aiThreatScore=")
					.Append(MkVThreat.Score(molotovProfile, 8f) > MkVThreat.Score(rifleProfile, 8f));

				// 敌对派系放一个燃烧瓶敌人，验证排序里它真的排第一。
				Faction enemies = Faction.OfAncientsHostile;
				if (enemies != null)
				{
					hostile = SpawnArmedPawn(map, mkv, enemies, "Weapon_GrenadeMolotov");
				}
				if (hostile != null)
				{
					List<MkVThreat.Threat> ranked = MkVThreat.Rank(mkv, true);
					builder.Append(" aiThreatRankFirst=")
						.Append(ranked.Count > 0 && ranked[0].pawn == hostile)
						.Append(" aiThreatRankLabel=").Append(ranked.Count > 0 ? ranked[0].label : "none");

					// 模型挑了拿步枪的那个时，兜底逻辑要改打燃烧瓶。
					Pawn rifleman = SpawnArmedPawn(map, mkv, enemies, "Gun_AssaultRifle");
					if (rifleman != null)
					{
						string chosen = "attack:" + rifleman.thingIDNumber;
						string fixedAction = MkVAiService.PreferAreaTargetForTest(mkv, chosen);
						builder.Append(" aiAoeOverride=")
							.Append(fixedAction == "attack:" + hostile.thingIDNumber);
						DestroyTestPawn(rifleman);
					}
				}

				// 预警：威胁信 + 地图上的敌人。
				Find.LetterStack.ReceiveLetter("自检威胁", "自检用威胁信", LetterDefOf.ThreatBig);
				foreach (Letter candidate in Find.LetterStack.LettersListForReading)
				{
					if (candidate != null && candidate.def == LetterDefOf.ThreatBig)
					{
						letter = candidate;
						break;
					}
				}
				builder.Append(" aiAlertLetter=").Append(MkVAlert.HasRecentThreatLetter())
					.Append(" aiAlertActive=").Append(MkVAlert.IsActive(map))
					.Append(" aiAlertCell=").Append(MkVAlert.NearestHostileCell(map, mkv.Position).IsValid);

				// 边打边撤 / 躲火：敌人贴近时要能退到更远处；脚下起火要先算作危险。
				if (hostile != null)
				{
					Verb verb = mkv.TryGetAttackVerb(hostile, false);
					IntVec3 kite = MkVCombatTactics.FindFiringPosition(
						mkv, new LocalTargetInfo(hostile), verb);
					builder.Append(" aiKiteCell=")
						.Append(kite.IsValid
							&& kite.DistanceTo(hostile.Position) > mkv.Position.DistanceTo(hostile.Position));
				}
				Thing fire = null;
				try
				{
					fire = ThingMaker.MakeThing(ThingDefOf.Fire);
					GenSpawn.Spawn(fire, mkv.Position, map);
					builder.Append(" aiFireSpawn=").Append(fire.Spawned)
						.Append(" aiFireDanger=").Append(MkVCombatTactics.InDanger(mkv));
					if (hostile != null)
					{
						Verb verb = mkv.TryGetAttackVerb(hostile, false);
						IntVec3 safe = MkVCombatTactics.FindFiringPosition(
							mkv, new LocalTargetInfo(hostile), verb);
						builder.Append(" aiFireAvoid=")
							.Append(safe.IsValid && safe.DistanceTo(mkv.Position) > 1.5f);
					}
				}
				finally
				{
					if (fire != null && !fire.Destroyed)
					{
						fire.Destroy(DestroyMode.Vanish);
					}
				}

				// 自主动作的前置：组装 JobDef、下一个机器人研究。
				builder.Append(" aiAssembleDef=").Append(
						DefDatabase<JobDef>.GetNamedSilentFail("CK_MkVAssemble")?.driverClass != null)
					.Append(" aiResearchProject=").Append(MkVResearch.NextProject()?.defName ?? "none");

				// 候选 JSON：新增的 alert / threat / tag 必须仍是合法 JSON，跑测试时用 ConvertFrom-Json 复核。
				builder.Append(" aiPromptJson=").Append(MkVAiService.SerializePromptForTest(new AiPrompt
				{
					name = mkv.LabelShort,
					alert = true,
					colonists = 0,
					candidates = new[]
					{
						new AiCandidate
						{
							id = "attack:1", kind = "attack", label = "海盗（燃烧瓶·范围）",
							dist = 5, threat = 140, tag = "范围"
						},
						new AiCandidate { id = "guard", kind = "guard", label = "守卫殖民地", dist = 0 }
					}
				}));
			}
			catch (Exception exception)
			{
				builder.Append(" aiThreatError=").Append(exception.Message);
			}
			finally
			{
				if (letter != null)
				{
					Find.LetterStack.RemoveLetter(letter);
				}
				DestroyTestPawn(molotov);
				DestroyTestPawn(rifle);
				DestroyTestPawn(hostile);
			}
		}

		/// <summary>临时造一个带指定武器的假人；武器 Def 不存在时返回 null。</summary>
		private static Pawn SpawnArmedPawn(Map map, Pawn near, Faction faction, string weaponDefName)
		{
			ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
			if (weaponDef == null || faction == null || near?.Map == null)
			{
				return null;
			}
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
				PawnKindDefOf.Colonist, faction, PawnGenerationContext.NonPlayer));
			if (pawn == null)
			{
				return null;
			}
			IntVec3 cell = CellFinder.RandomClosewalkCellNear(near.Position, map, 5);
			GenSpawn.Spawn(pawn, cell, map);
			if (pawn.equipment != null)
			{
				pawn.equipment.DestroyAllEquipment();
				pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(weaponDef));
			}
			return pawn;
		}

		private static void DestroyTestPawn(Pawn pawn)
		{
			if (pawn != null && !pawn.Destroyed)
			{
				pawn.Destroy(DestroyMode.Vanish);
			}
		}

		/// <summary>
		/// 紧急救援探针：把一个殖民者打到倒地 + 严重失血，检查会不会就地铺一张医疗睡眠点并给出抱人 Job。
		/// </summary>
		private static void ProbeMedicalAid(Map map, Pawn mkv, StringBuilder builder)
		{
			Pawn patient = null;
			Building_Bed spot = null;
			try
			{
				patient = SpawnArmedPawn(map, mkv, Faction.OfPlayer, "Gun_Autopistol");
				if (patient == null)
				{
					builder.Append(" aiMed=none");
					return;
				}
				patient.equipment?.DestroyAllEquipment();
				HealthUtility.DamageUntilDowned(patient);
				Hediff bloodLoss = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, patient);
				bloodLoss.Severity = 0.55f;
				patient.health.AddHediff(bloodLoss);

				builder.Append(" aiMedDowned=").Append(patient.Downed)
					.Append(" aiMedCritical=").Append(MkVMedicalAid.IsCritical(patient));

				Job job;
				bool made = MkVMedicalAid.TryEmergencyRescue(mkv, patient, out job);
				spot = job?.GetTarget(TargetIndex.B).Thing as Building_Bed;
				builder.Append(" aiMedJob=").Append(made && job != null)
					.Append(" aiMedSpot=").Append(spot != null && spot.Medical)
					.Append(" aiMedDef=").Append(spot?.def?.defName ?? "none")
					.Append(" aiMedJobTarget=").Append(job?.targetA.Thing == patient);
			}
			catch (Exception exception)
			{
				builder.Append(" aiMedError=").Append(exception.Message);
			}
			finally
			{
				if (spot != null && !spot.Destroyed)
				{
					spot.Destroy(DestroyMode.Vanish);
				}
				DestroyTestPawn(patient);
			}
		}
	}
}
