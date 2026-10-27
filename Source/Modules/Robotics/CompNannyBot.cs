using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
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

		public const float MkVMaxDurability = 1800f;

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

		/// <summary>Mk-V 的技能只在首次生成/旧档升级时随机一次。</summary>
		private bool skillsInitialized;

		/// <summary>正在被殖民者维修；维修期间不充电、不接新家务。</summary>
		public bool repairing;

		/// <summary>常用充电桩；桩被拆掉后会自动改用最近的桩。</summary>
		public Building homeStation;

		private Graphic paintedGraphic;
		private Graphic paintedBase;
		private int paintedIndex = -1;
		private bool lowBatteryWarned;

		/// <summary>每项家务的优先级 0（关闭）–8；1 最高，索引对应 RobotChore。</summary>
		private List<int> chorePriorities;

		private Pawn Pawn => parent as Pawn;

		public float Durability => durability;

		private static void SexualizeRjw(Pawn pawn)
		{
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
			SetSkill(pawn, SkillDefOf.Shooting, 18);
			SetSkill(pawn, SkillDefOf.Melee, 18);
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

		private static void ApplySkill(Pawn pawn, SkillDef def, int baseLevel)
		{
			SetSkill(pawn, def, Mathf.Clamp(baseLevel + Rand.RangeInclusive(-1, 1), 0, 20));
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
			if (NannyBotUtility.IsMkV(pawn) && pawn != null)
			{
				// 商队/工作检查会读取技能，而技能记录会访问 story.traits；机械族默认两者都没有。
				if (pawn.story == null)
				{
					pawn.story = new Pawn_StoryTracker(pawn);
				}
				if (pawn.skills == null)
				{
					pawn.skills = new Pawn_SkillTracker(pawn);
				}
				if (!skillsInitialized)
				{
					ApplyRobotSkills(pawn);
					skillsInitialized = true;
				}
				pawn.gender = Gender.Female;
				SexualizeRjw(pawn);
			}
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
			Scribe_Values.Look(ref lowPowerStandby, "ck_robotics_lowPowerStandby", false);
			Scribe_Values.Look(ref skillsInitialized, "ck_robotics_skillsInitialized", false);
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

			if (durability <= 0f)
			{
				Scrap();
				return;
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

			if (NannyBotUtility.IsMkV(pawn) && !pawn.IsColonyMech)
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

			if (repairing)
			{
				lines.Add("CK.Robotics.Inspect.Repairing".Translate());
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

		private static bool IsWorkJob(JobDef def)
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
