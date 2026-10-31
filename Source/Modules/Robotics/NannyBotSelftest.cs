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
				ProbeMkVDualWield(mkv, builder);
				ProbeMkVBackupWeapon(mkv, builder);
				ProbeArcBladeEffects(mkv, map, builder);
				ProbeCaravanPriority(map, mkv, builder);
				ProbeMkvRandomTraits(mkv, builder);
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
	}
}
