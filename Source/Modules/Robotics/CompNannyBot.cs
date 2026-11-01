using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ColonyKit.Modules.Robotics
{
	public class CompProperties_NannyBot : CompProperties
	{
		public CompProperties_NannyBot()
		{
			compClass = typeof(CompNannyBot);
		}
	}

	/// <summary>
	/// 保姆机器人的状态与指令：耐久、暂停、家务开关、母充电桩、充电与手动指令。
	/// 机器人是玩家阵营的机械族 Pawn，但没有 Work 页，默认自动找活。
	/// </summary>
	public class CompNannyBot : ThingComp
	{
		public const float DefaultMaxDurability = 1000f;

		public const float MkVMaxDurability = 5000f;

		/// <summary>每 1 点实际伤害损耗的耐久；约 250 点伤害报废。</summary>
		public const float DurabilityLossPerDamage = 4f;

		private static readonly HashSet<string> WorkJobs = new HashSet<string>
		{
			"CK_NannyCleanFilth",
			"CK_NannyUnblock",
			"CK_EmptySepticTank",
			"HaulToCell",
			"HaulToContainer",
			"BeatFire",
			"DoBill",
			"Refuel",
			"RefuelAtomic",
			"FeedPatient",
			"TendPatient",
			"Research",
			"Rescue",
			"RopeToPen",
			"RopeRoamerToUnenclosedPen",
			"RopeRoamerToHitchingPost",
			"Unrope"
		};

		private static readonly Color[] PaintPalette =
		{
			Color.white,
			new Color(0.48f, 0.72f, 0.98f),
			new Color(0.55f, 0.88f, 0.56f),
			new Color(0.96f, 0.55f, 0.52f),
			new Color(0.98f, 0.86f, 0.48f),
			new Color(0.76f, 0.60f, 0.96f),
			new Color(0.96f, 0.62f, 0.82f),
			new Color(0.55f, 0.60f, 0.66f)
		};

		public bool sweep = true;
		public bool scrub = true;
		public bool haul = true;
		public bool paused;

		/// <summary>Mk-V 在充电桩上进入低功耗待机，充电转换率额外 ×1.2。</summary>
		public bool lowPowerStandby;

		public float durability = -1f;
		public int paintIndex;

		/// <summary>Mk-V 的技能只在首次生成/技能档案升级时随机一次。</summary>
		private bool skillsInitialized;
		private int skillsProfileVersion;

		/// <summary>Mk-V 的特性只在首次生成时随机一次，之后随 story 一起存档。</summary>
		private bool traitsInitialized;
		private int traitsProfileVersion;

		private bool durabilityMigratedTo5000;

		/// <summary>旧档 Mk-V 首次载入时补发专属电弧刃；补过一次后不再重复发（玩家换掉就是换掉了）。</summary>
		private bool bladeGranted;

		/// <summary>正在被殖民者维修；维修期间不充电、不接新家务。</summary>
		public bool repairing;

		/// <summary>Mk-V 野外自修复进行中（地图上没人能来修，自己在极慢地补耐久）。</summary>
		private bool selfRepairing;

		/// <summary>下一次重新扫描「地图上有没有人能修」的时间与结果；缓存起来，避免每 tick 做寻路。</summary>
		private int selfRepairScanTick;
		private bool selfRepairHasRepairer;

		/// <summary>Mk-V 的 AI 控制开关（每台独立）；总开关在设置页。</summary>
		public bool AiControl = true;

		/// <summary>AI 下一次允许派发的时间，以及最近一次动作/台词/错误。</summary>
		public int AiNextTick;
		public string AiLastAction;
		public string AiLastSay;
		public string AiLastError;

		/// <summary>常用充电桩；桩被拆掉后会自动改用最近的桩。</summary>
		public Building homeStation;

		private Rot4 lastBodyRotation;
		private bool hasLastBodyRotation;

		/// <summary>已经为哪一支远行队打断过手头的活；同一支只打断一次，避免反复中止自己的等待任务。</summary>
		private int caravanDropLordId = -1;

		private Graphic paintedGraphic;
		private Graphic paintedBase;
		private int paintedIndex = -1;
		private bool lowBatteryWarned;

		/// <summary>每项家务的优先级 0（关闭）–8；1 最高，索引对应 RobotChore。</summary>
		private List<int> chorePriorities;

		internal Pawn Pawn => parent as Pawn;

		public float Durability => durability;

		private static void AddMechanicalVagina(Pawn pawn)
		{
			if (!RjwCompat.Loaded)
			{
				return;
			}
			HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("CK_ArtificialVaginaMech");
			if (def == null || pawn?.health?.hediffSet == null || pawn.health.hediffSet.HasHediff(def))
			{
				return;
			}
			BodyPartRecord part = null;
			for (int i = 0; i < pawn.RaceProps.body.AllParts.Count; i++)
			{
				BodyPartRecord candidate = pawn.RaceProps.body.AllParts[i];
				if (candidate.def?.defName == "MechGenitals")
				{
					part = candidate;
					break;
				}
			}
			if (part != null)
			{
				Hediff hediff = HediffMaker.MakeHediff(def, pawn, part);
				pawn.health.AddHediff(hediff, part);
			}
		}

		private static void AddMechanicalBreasts(Pawn pawn)
		{
			if (!RjwCompat.Loaded)
			{
				return;
			}
			HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("CK_ArtificialBreastsMech");
			if (def == null || pawn?.health?.hediffSet == null || pawn.health.hediffSet.HasHediff(def))
			{
				return;
			}
			BodyPartRecord part = null;
			for (int i = 0; i < pawn.RaceProps.body.AllParts.Count; i++)
			{
				BodyPartRecord candidate = pawn.RaceProps.body.AllParts[i];
				if (candidate.def?.defName == "MechChest")
				{
					part = candidate;
					break;
				}
			}
			if (part != null)
			{
				pawn.health.AddHediff(HediffMaker.MakeHediff(def, pawn, part), part);
			}
		}

		private static void SexualizeRjw(Pawn pawn)
		{
			if (!RjwCompat.Loaded)
			{
				return;
			}
			try
			{
				Type sexualizer = Type.GetType("rjw.Sexualizer, RJW");
				MethodInfo sexualize = sexualizer?.GetMethod(
					"sexualize_pawn",
					BindingFlags.Public | BindingFlags.Static);
				sexualize?.Invoke(null, new object[] { pawn });
			}
			catch (Exception exception)
			{
				Log.Warning($"[ColonyKit] RJW could not sexualize {pawn?.LabelShort}: {exception.Message}");
			}
		}

		private static void ApplyRobotSkills(Pawn pawn)
		{
			SetSkill(pawn, SkillDefOf.Shooting, Mathf.Clamp(18 + Rand.RangeInclusive(-5, 5), 16, 20));
			SetSkill(pawn, SkillDefOf.Melee, Mathf.Clamp(18 + Rand.RangeInclusive(-5, 5), 16, 20));
			ApplySkill(pawn, SkillDefOf.Medicine, 18);
			ApplySkill(pawn, SkillDefOf.Cooking, 18);
			ApplySkill(pawn, SkillDefOf.Crafting, 18);
			ApplySkill(pawn, SkillDefOf.Mining, 13);
			ApplySkill(pawn, SkillDefOf.Construction, 8);
			ApplySkill(pawn, SkillDefOf.Animals, 10);
			ApplySkill(pawn, SkillDefOf.Artistic, 1);
			ApplySkill(pawn, SkillDefOf.Social, 1);
			ApplySkill(pawn, SkillDefOf.Intellectual, 15);
		}

		/// <summary>Mk-V 固定特性：坚韧、敏捷、疾步如飞。</summary>
		private static readonly string[] RobotFixedTraitDefNames =
		{
			"Tough",
			"Nimble",
			"SpeedOffset"
		};

		/// <summary>与固定特性一一对应的度数：疾步如飞 2，其余 0。</summary>
		private static readonly int[] RobotFixedTraitDegrees = { 0, 0, 2 };

		/// <summary>旧版固定特性（格斗者、冷枪手）：升到 v3 时从老存档的 Mk-V 身上摘掉，改由随机池抽取。</summary>
		private static readonly string[] RobotLegacyFixedTraitDefNames = { "Brawler", "ShootingAccuracy" };

		/// <summary>
		/// 开启「随机特性」时额外抽取的特性池：格斗者、冷枪手也在池里。
		/// 池子比抽取数多，每次只抽 <see cref="RobotRandomTraitCount"/> 个，不会一次把整池拿全。
		/// </summary>
		private static readonly string[] RobotRandomTraitDefNames =
		{
			"Brawler",
			"ShootingAccuracy",
			"FastLearner",
			"GreatMemory"
		};

		/// <summary>与随机池一一对应的度数：冷枪手是 -1 度（trigger-happy），其余 0。</summary>
		private static readonly int[] RobotRandomTraitDegrees = { 0, -1, 0, 0 };

		/// <summary>随机特性每次最多抽几个；必须小于池子大小，否则等于把随机池发全。</summary>
		private const int RobotRandomTraitCount = 2;

		/// <summary>特性档案版本：3 起固定 3 个特性，格斗者/冷枪手移入随机池。</summary>
		private const int RobotTraitsProfileVersion = 3;

		private static void GrantRobotTrait(Pawn pawn, string defName, int degree)
		{
			TraitSet traits = pawn?.story?.traits;
			TraitDef def = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
			if (traits == null || def == null || traits.HasTrait(def))
			{
				return;
			}
			// 直接写入，不走随机生成的冲突过滤：随机池自己用 CanGrantRandomTrait
			// 挡掉互斥组合（格斗者 ↔ 冷枪手在原版里就互相排斥）。
			traits.GainTrait(new Trait(def, degree));
		}

		/// <summary>把旧版固定特性（格斗者、冷枪手）从老存档的 Mk-V 身上摘掉；新版里它们只从随机池出。</summary>
		private static void StripRobotLegacyTraits(Pawn pawn)
		{
			TraitSet traits = pawn?.story?.traits;
			if (traits == null)
			{
				return;
			}
			for (int i = 0; i < RobotLegacyFixedTraitDefNames.Length; i++)
			{
				TraitDef def = DefDatabase<TraitDef>.GetNamedSilentFail(RobotLegacyFixedTraitDefNames[i]);
				Trait trait = def == null ? null : traits.GetTrait(def);
				if (trait != null)
				{
					traits.RemoveTrait(trait);
				}
			}
		}

		/// <summary>随机池里的某个特性现在能不能给：没拿过，也不和身上已有的特性互斥。</summary>
		private static bool CanGrantRandomTrait(TraitSet traits, string defName)
		{
			TraitDef def = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
			if (def == null || traits.HasTrait(def))
			{
				return false;
			}
			for (int i = 0; i < traits.allTraits.Count; i++)
			{
				TraitDef ownedDef = traits.allTraits[i]?.def;
				if (ownedDef == null)
				{
					continue;
				}
				// 原版两个特性只在一个方向写 conflictingTraits，两个方向都要查。
				if (def.conflictingTraits != null && def.conflictingTraits.Contains(ownedDef))
				{
					return false;
				}
				if (ownedDef.conflictingTraits != null && ownedDef.conflictingTraits.Contains(def))
				{
					return false;
				}
			}
			return true;
		}

		private static void ApplyRobotTraits(Pawn pawn)
		{
			StripRobotLegacyTraits(pawn);
			for (int i = 0; i < RobotFixedTraitDefNames.Length; i++)
			{
				GrantRobotTrait(pawn, RobotFixedTraitDefNames[i], RobotFixedTraitDegrees[i]);
			}
			if (!NannyBotUtility.Settings.mkvRandomTraits || pawn?.story?.traits == null)
			{
				return;
			}
			TraitSet traits = pawn.story.traits;
			List<int> pool = new List<int>();
			for (int i = 0; i < RobotRandomTraitDefNames.Length; i++)
			{
				pool.Add(i);
			}
			for (int picked = 0; picked < RobotRandomTraitCount && pool.Count > 0; picked++)
			{
				pool.RemoveAll(index => !CanGrantRandomTrait(traits, RobotRandomTraitDefNames[index]));
				if (pool.Count == 0)
				{
					break;
				}
				int slot = Rand.Range(0, pool.Count);
				int choice = pool[slot];
				pool.RemoveAt(slot);
				GrantRobotTrait(pawn, RobotRandomTraitDefNames[choice], RobotRandomTraitDegrees[choice]);
			}
		}

		/// <summary>仅供 -colonykit-nannybot-test 自检：按当前设置再结算一次特性。</summary>
		internal static void ApplyRobotTraitsForTest(Pawn pawn)
		{
			ApplyRobotTraits(pawn);
		}

		private static void ApplySkill(Pawn pawn, SkillDef def, int baseLevel)
		{
			SetSkill(pawn, def, Mathf.Clamp(baseLevel + Rand.RangeInclusive(-5, 5), 0, 20));
		}

		private static void SetSkill(Pawn pawn, SkillDef def, int level)
		{
			SkillRecord skill = pawn?.skills?.GetSkill(def);
			if (skill != null)
			{
				skill.Level = level;
				skill.xpSinceLastLevel = 0f;
			}
		}

		public float MaxDurability => NannyBotUtility.IsMkV(Pawn)
			? MkVMaxDurability
			: DefaultMaxDurability;

		public bool HasPaint => paintIndex > 0 && paintIndex < PaintPalette.Length;

		public Color PaintColor => PaintPalette[Mathf.Clamp(paintIndex, 0, PaintPalette.Length - 1)];

		private void EnsurePriorities()
		{
			if (chorePriorities == null)
			{
				chorePriorities = new List<int>();
			}
			while (chorePriorities.Count < RobotChores.Count)
			{
				RobotChore chore = (RobotChore)chorePriorities.Count;
				chorePriorities.Add(RobotChores.DefaultPriority(chore));
			}
			while (chorePriorities.Count > RobotChores.Count)
			{
				chorePriorities.RemoveAt(chorePriorities.Count - 1);
			}
		}

		public int GetPriority(RobotChore chore)
		{
			EnsurePriorities();
			return chorePriorities[(int)chore];
		}

		public void SetPriority(RobotChore chore, int priority)
		{
			EnsurePriorities();
			chorePriorities[(int)chore] = Mathf.Clamp(priority, 0, 8);
		}

		/// <summary>工作页签显示用：设置页总开关关闭时返回 0，但保留用户设定的数字。</summary>
		public int GetEffectivePriority(RobotChore chore)
		{
			int priority = GetPriority(chore);
			return priority > 0 && RobotChores.IsEnabledBySettings(chore) ? priority : 0;
		}

		public int ActiveChoreCount
		{
			get
			{
				int count = 0;
				for (int i = 0; i < RobotChores.Count; i++)
				{
					if (GetEffectivePriority((RobotChore)i) > 0)
					{
						count++;
					}
				}
				return count;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			EnsurePriorities();
			if (durability < 0f)
			{
				durability = MaxDurability;
			}
			Pawn pawn = Pawn;
			if (!durabilityMigratedTo5000 && NannyBotUtility.IsMkV(pawn) && durability > 0f)
			{
				// 旧档 Mk-V 的上限是 1800；按比例迁移到 5000，保留已受的伤。
				durability = Mathf.Clamp(durability / 1800f * MkVMaxDurability, 1f, MkVMaxDurability);
				durabilityMigratedTo5000 = true;
			}
			// 机械保姆（Mk-I ~ Mk-V）统一补口渴/膀胱：先让需求表按 Def 重新评估，再兜底直接补。
			pawn.needs?.AddOrRemoveNeedsAsAppropriate();
			ColonyKit.Modules.Hygiene.HygieneUtility.EnsureRobotNeeds(pawn);
			if (NannyBotUtility.IsNannyBot(pawn))
			{
				LogRobotNeeds(pawn);
			}
			if (NannyBotUtility.IsMkV(pawn) && pawn != null)
			{
				// 商队/工作检查会读取技能，而技能记录会访问 story.traits；机械族默认两者都没有。
				if (pawn.story == null)
				{
					pawn.story = new Pawn_StoryTracker(pawn);
				}
				if (pawn.story.traits == null)
				{
					// 老存档/特殊生成路径可能只建了 story 没有 trait 集合；特性面板与统计都读它。
					pawn.story.traits = new TraitSet(pawn);
				}
				if (pawn.skills == null)
				{
					pawn.skills = new Pawn_SkillTracker(pawn);
				}
				if (pawn.relations == null)
				{
					// 原版恋爱/社交互动会读取关系表；机械族默认没有这个组件。
					pawn.relations = new Pawn_RelationsTracker(pawn);
				}
				if (pawn.equipment == null)
				{
					// 武器拾取/装备 Job 需要装备栏；没有它 JobDriver_Equip 会立即失败并刷任务。
					pawn.equipment = new Pawn_EquipmentTracker(pawn);
				}
				if (!bladeGranted && MkVLoadout.Enabled)
				{
					// 新造的 Mk-V 在这里就拿到主手电弧发射器 + 副手电弧刃；旧档 Mk-V 首次载入补发一次。
					bladeGranted = true;
					MkVLoadout.GiveStartingWeapons(pawn);
				}
				// 需求已在上面对所有机械保姆统一补齐，这里不再重复评估。
				if (!skillsInitialized || skillsProfileVersion < 3)
				{
					ApplyRobotSkills(pawn);
					skillsInitialized = true;
					skillsProfileVersion = 3;
				}
				if (!traitsInitialized || traitsProfileVersion < RobotTraitsProfileVersion)
				{
					// 固定特性始终补齐；随机特性由设置页决定是否追加。
					// 老存档的 Mk-V 在升级到这里时先摘掉旧版的格斗者/冷枪手，避免和新规则叠在一起。
					ApplyRobotTraits(pawn);
					traitsInitialized = true;
					traitsProfileVersion = RobotTraitsProfileVersion;
				}
				AiNextTick = Find.TickManager.TicksGame + 120;
				pawn.gender = Gender.Female;
				SexualizeRjw(pawn);
				AddMechanicalVagina(pawn);
				AddMechanicalBreasts(pawn);
			}
		}

		/// <summary>临时诊断：读档/出厂时列出机械保姆实际拿到的需求，确认口渴/膀胱已注入。</summary>
		private static void LogRobotNeeds(Pawn pawn)
		{
			if (pawn?.needs == null)
			{
				return;
			}
			string list = "";
			foreach (Need need in pawn.needs.AllNeeds)
			{
				if (need?.def == null)
				{
					continue;
				}
				if (list.Length > 0)
				{
					list += ",";
				}
				list += need.def.defName;
			}
			ModLog.Info(
				"Robotics",
				$"{pawn.LabelShort} needs={list} thirst={pawn.needs.TryGetNeed<ColonyKit.Modules.Hygiene.Need_Thirst>() != null} " +
				$"bladder={pawn.needs.TryGetNeed<ColonyKit.Modules.Hygiene.Need_Bladder>() != null}");
		}

		public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			base.PostPostApplyDamage(dinfo, totalDamageDealt);
			if (totalDamageDealt > 0f)
			{
				Wear(totalDamageDealt * DurabilityLossPerDamage);
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			bool hadPriorities = chorePriorities != null && chorePriorities.Count > 0;
			Scribe_Values.Look(ref sweep, "ck_robotics_sweep", true);
			Scribe_Values.Look(ref scrub, "ck_robotics_scrub", true);
			Scribe_Values.Look(ref haul, "ck_robotics_haul", true);
			Scribe_Values.Look(ref paused, "ck_robotics_paused", false);
			Scribe_Values.Look(ref AiControl, "ck_robotics_aiControl", true);
			Scribe_Values.Look(ref lowPowerStandby, "ck_robotics_lowPowerStandby", false);
			Scribe_Values.Look(ref skillsInitialized, "ck_robotics_skillsInitialized", false);
			Scribe_Values.Look(ref skillsProfileVersion, "ck_robotics_skillsProfileVersion", 0);
			Scribe_Values.Look(ref traitsInitialized, "ck_robotics_traitsInitialized", false);
			Scribe_Values.Look(ref traitsProfileVersion, "ck_robotics_traitsProfileVersion", 0);
			Scribe_Values.Look(ref durabilityMigratedTo5000, "ck_robotics_durabilityMigrated5000", false);
			Scribe_Values.Look(ref bladeGranted, "ck_robotics_bladeGranted", false);
			Scribe_Values.Look(ref durability, "ck_robotics_durability", -1f);
			if (durability < 0f)
			{
				durability = MaxDurability;
			}
			Scribe_Values.Look(ref paintIndex, "ck_robotics_paintIndex", 0);
			Scribe_References.Look(ref homeStation, "ck_robotics_homeStation");
			Scribe_Collections.Look(
				ref chorePriorities,
				"ck_robotics_chorePriorities",
				LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				bool migrateOldToggles = !hadPriorities
					|| chorePriorities == null
					|| chorePriorities.Count == 0;
				EnsurePriorities();
				if (migrateOldToggles)
				{
					SetPriority(RobotChore.Sweep, sweep ? 3 : 0);
					SetPriority(RobotChore.Scrub, scrub ? 3 : 0);
					SetPriority(RobotChore.Haul, haul ? 3 : 0);
				}
			}
		}

		public override void CompTick()
		{
			Pawn pawn = Pawn;
			if (pawn == null || pawn.Dead || !pawn.Spawned)
			{
				return;
			}

			if (NannyBotUtility.IsMkV(pawn))
			{
				// Mk-V 是机械体：口渴/膀胱只用于 RJW 与卫生面板的兼容显示，
				// 不应要求它像殖民者一样找水或抢厕所。这里让机体自行循环补充，
				// 避免在缺水/无厕所地图上出现脱水或膀胱崩溃。
				if (parent.IsHashIntervalTick(120))
				{
					Need thirst = pawn.needs?.TryGetNeed<ColonyKit.Modules.Hygiene.Need_Thirst>();
					if (thirst != null && thirst.CurLevel < 0.9f)
					{
						thirst.CurLevel = Mathf.Min(1f, thirst.CurLevel + 0.05f);
					}
					Need bladder = pawn.needs?.TryGetNeed<ColonyKit.Modules.Hygiene.Need_Bladder>();
					if (bladder != null && bladder.CurLevel < 0.9f)
					{
						bladder.CurLevel = Mathf.Min(1f, bladder.CurLevel + 0.05f);
					}
				}
				if (hasLastBodyRotation && lastBodyRotation != pawn.Rotation)
				{
					// 旋转状态先变了但绘制缓存仍停留在上一方向时，强制刷新纹理图集与立绘缓存。
					pawn.Drawer?.renderer?.SetAllGraphicsDirty();
				}
				lastBodyRotation = pawn.Rotation;
				hasLastBodyRotation = true;
			}

			if (durability <= 0f)
			{
				Scrap();
				return;
			}
			if (NannyBotUtility.IsMkV(pawn))
			{
				TickSelfRepair(pawn);
			}
			if (lowPowerStandby && pawn.CurJobDef != NannyBotDefOf.CK_NannyCharge)
			{
				lowPowerStandby = false;
			}

			if (parent.IsHashIntervalTick(60))
			{
				bool beingRepaired = NannyBotUtility.IsBeingRepaired(pawn);
				if (beingRepaired != repairing)
				{
					repairing = beingRepaired;
				}
				if (beingRepaired && pawn.CurJobDef == NannyBotDefOf.CK_NannyCharge)
				{
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
				EnsureCaravanPriority(pawn);
				MkVAiService.Tick(this);
			}

			if (!parent.IsHashIntervalTick(250))
			{
				return;
			}

			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			if (NannyBotUtility.Settings.sounds && energy != null && !paused)
			{
				if (energy.CurLevelPercentage <= NannyBotUtility.Settings.chargeThreshold)
				{
					if (!lowBatteryWarned)
					{
						lowBatteryWarned = true;
						PlayBeep("CK_NannyBotLowBattery");
					}
				}
				else
				{
					lowBatteryWarned = false;
				}

				if (IsWorkJob(pawn.CurJobDef) && parent.IsHashIntervalTick(1000))
				{
					PlayBeep("CK_NannyBotBeep");
				}
			}
		}

		/// <summary>
		/// 加入远行队后商队职责是最高优先任务：把还在做的家务/巡逻/索敌打断一次，
		/// 下一个 tick 的思考就会直接走思考树最前面的 LordDuty（搬物资、去集合点、随队出发）。
		/// 只打断一次，机器人自己派的等待/闲逛任务不会被反复中止。
		/// </summary>
		internal void EnsureCaravanPriority(Pawn pawn)
		{
			Lord lord = pawn.GetLord();
			if (!(lord?.LordJob is LordJob_FormAndSendCaravan))
			{
				caravanDropLordId = -1;
				return;
			}
			if (caravanDropLordId == lord.loadID)
			{
				return;
			}
			caravanDropLordId = lord.loadID;
			Job job = pawn.CurJob;
			if (job == null || job.lord == lord || job.playerForced)
			{
				return;
			}
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}

		public void Wear(float amount)
		{
			if (amount <= 0f)
			{
				return;
			}
			durability = Mathf.Max(0f, durability - amount);
		}

		public void Repair(float amount)
		{
			if (amount <= 0f)
			{
				return;
			}
			durability = Mathf.Min(MaxDurability, durability + amount);
		}

		public void Notify_RepairStarted()
		{
			repairing = true;
		}

		public void Notify_RepairFinished()
		{
			repairing = false;
		}

		/// <summary>
		/// 野外自修复：耐久没满、地图上又没有任何殖民者能来修时，按设置页的
		/// 「每天 X 点」速率慢慢补耐久。有人能修（哪怕还在路上）就让给人修；
		/// 已经报废（耐久归零）的不会再复活。
		/// </summary>
		private void TickSelfRepair(Pawn pawn)
		{
			RoboticsSettings settings = NannyBotUtility.Settings;
			float rate = settings.mkvSelfRepairRate;
			if (!settings.mkvSelfRepair || rate <= 0f
				|| durability <= 0f || durability >= MaxDurability - 0.01f)
			{
				selfRepairing = false;
				return;
			}
			// 每 250 tick 结算一次：查询「有没有人正在修」要遍历预定表，不能每 tick 都做。
			if (!parent.IsHashIntervalTick(250))
			{
				return;
			}
			// 有殖民者正在赶来/正在维修时，自修复让路，避免两边同时生效。
			if (repairing || NannyBotUtility.IsBeingRepaired(pawn))
			{
				selfRepairing = false;
				return;
			}

			int now = Find.TickManager.TicksGame;
			if (now >= selfRepairScanTick)
			{
				selfRepairScanTick = now + 1000;
				selfRepairHasRepairer = NannyBotUtility.CanBeRepairedOnMap(pawn);
			}
			if (selfRepairHasRepairer)
			{
				selfRepairing = false;
				return;
			}

			Repair(rate * 250f / 60000f);
			if (!selfRepairing)
			{
				selfRepairing = true;
				ModLog.Info("Robotics",
					$"{pawn.LabelShort} started self-repair (no repairer on map, {rate:0} durability/day).");
			}
		}

		public void SetPaint(int index)
		{
			paintIndex = Mathf.Clamp(index, 0, PaintPalette.Length - 1);
			paintedGraphic = null;
			paintedBase = null;
			paintedIndex = -1;
		}

		public Graphic GetPaintedGraphic(Graphic baseGraphic)
		{
			if (!HasPaint || baseGraphic == null)
			{
				return baseGraphic;
			}
			if (paintedGraphic != null && ReferenceEquals(paintedBase, baseGraphic)
				&& paintedIndex == paintIndex)
			{
				return paintedGraphic;
			}

			paintedBase = baseGraphic;
			paintedIndex = paintIndex;
			paintedGraphic = baseGraphic.GetColoredVersion(
				baseGraphic.Shader,
				PaintColor,
				baseGraphic.ColorTwo);
			return paintedGraphic;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Pawn pawn = Pawn;
			if (pawn == null || pawn.Dead || !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				yield break;
			}

			Command_Toggle pause = new Command_Toggle
			{
				defaultLabel = paused
					? "CK.Robotics.Command.Resume".Translate()
					: "CK.Robotics.Command.Pause".Translate(),
				defaultDesc = "CK.Robotics.Command.Pause.Desc".Translate(),
				icon = TexCommand.PauseCaravan,
				isActive = () => paused,
				toggleAction = () =>
				{
					paused = !paused;
					if (paused && pawn.CurJob != null)
					{
						pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
					}
				}
			};
			yield return pause;

			if (NannyBotUtility.IsMkV(pawn))
			{
				if (pawn.drafter == null)
				{
					pawn.drafter = new Pawn_DraftController(pawn);
				}
				Command_Toggle draft = new Command_Toggle
				{
					defaultLabel = pawn.Drafted
						? "CK.Robotics.Command.Undraft".Translate()
						: "CK.Robotics.Command.Draft".Translate(),
					defaultDesc = "CK.Robotics.Command.Draft.Desc".Translate(),
					icon = TexCommand.Draft,
					hotKey = KeyBindingDefOf.Command_ColonistDraft,
					isActive = () => pawn.Drafted,
					toggleAction = () =>
					{
						if (pawn.drafter == null)
						{
							pawn.drafter = new Pawn_DraftController(pawn);
						}
						pawn.drafter.Drafted = !pawn.drafter.Drafted;
					}
				};
				yield return draft;

				Command_Toggle ai = new Command_Toggle
				{
					defaultLabel = "CK.Robotics.Command.Ai".Translate(),
					defaultDesc = "CK.Robotics.Command.Ai.Desc".Translate(),
					icon = TexCommand.GatherSpotActive,
					isActive = () => AiControl,
					toggleAction = () =>
					{
						AiControl = !AiControl;
						AiNextTick = Find.TickManager.TicksGame + 120;
					}
				};
				yield return ai;
			}

			yield return CommandAction(
				"CK.Robotics.Command.Recall".Translate(),
				"CK.Robotics.Command.Recall.Desc".Translate(),
				"UI/Commands/CK_Recall",
				() => Recall(pawn));

			yield return CommandAction(
				"CK.Robotics.Command.Goto".Translate(),
				"CK.Robotics.Command.Goto.Desc".Translate(),
				"UI/Commands/CK_Goto",
				() => BeginGoto(pawn));

			yield return CommandAction(
				"CK.Robotics.Command.PrioritizeFilth".Translate(),
				"CK.Robotics.Command.PrioritizeFilth.Desc".Translate(),
				"UI/Commands/CK_Sweep",
				() => BeginCleanFilth(pawn));

			yield return CommandAction(
				"CK.Robotics.Command.Rename".Translate(),
				"CK.Robotics.Command.Rename.Desc".Translate(),
				"UI/Commands/CK_Rename",
				() => Find.WindowStack.Add(new Dialog_RenameNannyBot(pawn)));

			yield return CommandAction(
				"CK.Robotics.Command.Paint".Translate(),
				"CK.Robotics.Command.Paint.Desc".Translate(),
				"UI/Commands/CK_Paint",
				() => OpenPaintMenu(pawn));

			Building station = NannyBotUtility.FindStation(pawn, true);
			Command_Action charge = new Command_Action
			{
				defaultLabel = "CK.Robotics.Command.Charge".Translate(),
				defaultDesc = "CK.Robotics.Command.Charge.Desc".Translate(),
				icon = Icon("UI/Commands/CK_Charge"),
				action = () =>
				{
					Building target = NannyBotUtility.FindStation(pawn, true);
					if (target == null)
					{
						Messages.Message(
							"CK.Robotics.NoStation".Translate(),
							MessageTypeDefOf.RejectInput,
							false);
						return;
					}
					Job job = JobMaker.MakeJob(NannyBotDefOf.CK_NannyCharge, target);
					pawn.jobs.TryTakeOrderedJob(job);
				}
			};
			if (station == null)
			{
				charge.Disabled = true;
				charge.disabledReason = "CK.Robotics.NoStation".Translate();
			}
			yield return charge;
		}

		public override string CompInspectStringExtra()
		{
			Pawn pawn = Pawn;
			if (pawn == null || pawn.Dead)
			{
				return null;
			}

			List<string> lines = new List<string>();
			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			if (energy != null)
			{
				lines.Add("CK.Robotics.Inspect.Battery".Translate(
					energy.CurLevel.ToString("0"),
					energy.MaxLevel.ToString("0")));
			}
			lines.Add("CK.Robotics.Inspect.Task".Translate(NannyBotJobUtility.TaskText(pawn)));
			lines.Add("CK.Robotics.Inspect.Durability".Translate(
				durability.ToString("0"),
				MaxDurability.ToString("0")));
			if (lowPowerStandby)
			{
				lines.Add("CK.Robotics.Inspect.LowPowerStandby".Translate());
			}
			lines.Add("CK.Robotics.Inspect.ActiveChores".Translate(ActiveChoreCount));

			if (NannyBotUtility.IsMkV(pawn) && NannyBotUtility.Settings.aiEnabled)
			{
				string action = string.IsNullOrEmpty(AiLastAction)
					? "CK.Robotics.Inspect.Ai.Waiting".Translate()
					: AiLastAction;
				string aiLine = "CK.Robotics.Inspect.Ai".Translate(action);
				if (!string.IsNullOrEmpty(AiLastSay))
				{
					aiLine += " \"" + AiLastSay + "\"";
				}
				if (!string.IsNullOrEmpty(AiLastError))
				{
					aiLine += " (" + AiLastError + ")";
				}
				lines.Add(aiLine);
			}

			if (repairing)
			{
				lines.Add("CK.Robotics.Inspect.Repairing".Translate());
			}
			else if (selfRepairing)
			{
				lines.Add("CK.Robotics.Inspect.SelfRepairing".Translate(
					NannyBotUtility.Settings.mkvSelfRepairRate.ToString("0")));
			}
			if (paused)
			{
				lines.Add("CK.Robotics.Inspect.Paused".Translate());
			}
			if (pawn.Downed)
			{
				lines.Add("CK.Robotics.Inspect.Downed".Translate());
			}
			return string.Join("\n", lines);
		}

		internal static bool IsWorkJob(JobDef def)
		{
			return def != null && WorkJobs.Contains(def.defName);
		}

		private void PlayBeep(string defName)
		{
			SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail(defName);
			if (sound == null || Pawn?.Map == null)
			{
				return;
			}
			sound.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
		}

		private void Scrap()
		{
			Pawn pawn = Pawn;
			if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map == null)
			{
				return;
			}

			Map map = pawn.Map;
			IntVec3 cell = pawn.Position;
			string label = pawn.LabelShort;

			SpawnPart(map, cell, ThingDefOf.Steel, 40);
			if (ThingDefOf.ComponentIndustrial != null)
			{
				SpawnPart(map, cell, ThingDefOf.ComponentIndustrial, 3);
			}

			Messages.Message(
				"CK.Robotics.Scrapped".Translate(label),
				MessageTypeDefOf.NegativeEvent,
				false);
			pawn.Destroy(DestroyMode.Vanish);
		}

		private static void SpawnPart(Map map, IntVec3 cell, ThingDef def, int count)
		{
			if (def == null || count <= 0)
			{
				return;
			}
			while (count > 0)
			{
				Thing part = ThingMaker.MakeThing(def);
				part.stackCount = Mathf.Min(count, def.stackLimit);
				count -= part.stackCount;
				if (!GenPlace.TryPlaceThing(part, cell, map, ThingPlaceMode.Near))
				{
					part.Destroy();
				}
			}
		}

		public static int PaintColorCount => PaintPalette.Length;

		private static Command_Action CommandAction(
			string label,
			string description,
			string iconPath,
			System.Action action)
		{
			return new Command_Action
			{
				defaultLabel = label,
				defaultDesc = description,
				icon = Icon(iconPath),
				action = action
			};
		}

		private static void Recall(Pawn pawn)
		{
			Building station = NannyBotUtility.FindStation(pawn, false);
			if (station == null)
			{
				Messages.Message(
					"CK.Robotics.NoStation".Translate(),
					MessageTypeDefOf.RejectInput,
					false);
				return;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Goto, station.InteractionCell);
			job.locomotionUrgency = LocomotionUrgency.Jog;
			pawn.jobs.TryTakeOrderedJob(job);
		}

		private static void BeginGoto(Pawn pawn)
		{
			Find.Targeter.BeginTargeting(
				new TargetingParameters
				{
					canTargetLocations = true,
					canTargetPawns = false,
					canTargetBuildings = false,
					canTargetItems = false,
					canTargetFires = false,
					canTargetAnimals = false,
					canTargetHumans = false,
					canTargetMechs = false,
					canTargetPlants = false,
					validator = target => target.Cell.InBounds(pawn.Map)
				},
				target =>
				{
					if (!target.Cell.IsValid
						|| !pawn.CanReach(target.Cell, PathEndMode.OnCell, Danger.Deadly))
					{
						return;
					}
					Job job = JobMaker.MakeJob(JobDefOf.Goto, target.Cell);
					job.locomotionUrgency = LocomotionUrgency.Jog;
					pawn.jobs.TryTakeOrderedJob(job);
				});
		}

		private static void BeginCleanFilth(Pawn pawn)
		{
			Find.Targeter.BeginTargeting(
				new TargetingParameters
				{
					canTargetLocations = false,
					canTargetPawns = false,
					canTargetBuildings = false,
					canTargetItems = true,
					canTargetFires = false,
					canTargetAnimals = false,
					canTargetHumans = false,
					canTargetMechs = false,
					canTargetPlants = false,
					validator = target => target.Thing is Filth
				},
				target =>
				{
					Thing thing = target.Thing;
					if (thing == null || thing.Destroyed
						|| !pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Deadly))
					{
						return;
					}
					Job job = JobMaker.MakeJob(NannyBotDefOf.CK_NannyCleanFilth, thing);
					job.expiryInterval = 5000;
					pawn.jobs.TryTakeOrderedJob(job);
				});
		}

		private static void OpenPaintMenu(Pawn pawn)
		{
			CompNannyBot comp = pawn.TryGetComp<CompNannyBot>();
			if (comp == null)
			{
				return;
			}

			List<FloatMenuOption> options = new List<FloatMenuOption>();
			for (int i = 0; i < PaintColorCount; i++)
			{
				int index = i;
				options.Add(new FloatMenuOption(
					("CK.Robotics.Paint." + index).Translate(),
					() => comp.SetPaint(index)));
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}

		private static Texture2D Icon(string path)
		{
			return ContentFinder<Texture2D>.Get(path, false) ?? TexCommand.GatherSpotActive;
		}
	}
}
