using System.Collections.Generic;
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
		public const float MaxDurability = 1000f;

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
			"Rescue"
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

		public float durability = MaxDurability;
		public int paintIndex;

		/// <summary>组装它的充电桩；桩被拆掉后会自动改用最近的桩。</summary>
		public Building homeStation;

		private Graphic paintedGraphic;
		private Graphic paintedBase;
		private int paintedIndex = -1;
		private bool lowBatteryWarned;

		private Pawn Pawn => parent as Pawn;

		public float Durability => durability;

		public bool HasPaint => paintIndex > 0 && paintIndex < PaintPalette.Length;

		public Color PaintColor => PaintPalette[Mathf.Clamp(paintIndex, 0, PaintPalette.Length - 1)];

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (durability <= 0f)
			{
				durability = MaxDurability;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref sweep, "ck_robotics_sweep", true);
			Scribe_Values.Look(ref scrub, "ck_robotics_scrub", true);
			Scribe_Values.Look(ref haul, "ck_robotics_haul", true);
			Scribe_Values.Look(ref paused, "ck_robotics_paused", false);
			Scribe_Values.Look(ref durability, "ck_robotics_durability", MaxDurability);
			Scribe_Values.Look(ref paintIndex, "ck_robotics_paintIndex", 0);
			Scribe_References.Look(ref homeStation, "ck_robotics_homeStation");
		}

		public override void CompTick()
		{
			Pawn pawn = Pawn;
			if (pawn == null || pawn.Dead || !pawn.Spawned)
			{
				return;
			}

			if (!parent.IsHashIntervalTick(250))
			{
				return;
			}

			if (durability <= 0f)
			{
				Scrap();
				return;
			}

			if (!paused && !pawn.Downed && IsWorkJob(pawn.CurJobDef))
			{
				Wear(0.5f);
				if (durability <= 0f)
				{
					Scrap();
					return;
				}
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

			RoboticsSettings settings = NannyBotUtility.Settings;

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

			yield return Toggle(
				"CK.Robotics.Toggle.Sweep.Label".Translate(),
				"CK.Robotics.Toggle.Sweep.Desc".Translate(),
				"UI/Commands/CK_Sweep",
				() => sweep && settings.sweep,
				() => sweep = !sweep,
				settings.sweep);

			yield return Toggle(
				"CK.Robotics.Toggle.Scrub.Label".Translate(),
				"CK.Robotics.Toggle.Scrub.Desc".Translate(),
				"UI/Commands/CK_Scrub",
				() => scrub && settings.scrub,
				() => scrub = !scrub,
				settings.scrub);

			yield return Toggle(
				"CK.Robotics.Toggle.Haul.Label".Translate(),
				"CK.Robotics.Toggle.Haul.Desc".Translate(),
				"UI/Commands/CK_Haul",
				() => haul && settings.haul,
				() => haul = !haul,
				settings.haul);

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
			lines.Add("CK.Robotics.Inspect.Durability".Translate(
				durability.ToString("0"),
				MaxDurability.ToString("0")));
			lines.Add("CK.Robotics.Inspect.Tasks".Translate(
				State(sweep && NannyBotUtility.Settings.sweep),
				State(scrub && NannyBotUtility.Settings.scrub),
				State(haul && NannyBotUtility.Settings.haul)));

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

		private static string State(bool enabled)
		{
			return enabled
				? "CK.Robotics.On".Translate()
				: "CK.Robotics.Off".Translate();
		}

		private static Command_Toggle Toggle(
			string label,
			string description,
			string iconPath,
			System.Func<bool> active,
			System.Action toggle,
			bool enabled)
		{
			Command_Toggle command = new Command_Toggle
			{
				defaultLabel = label,
				defaultDesc = description,
				icon = Icon(iconPath),
				isActive = active,
				toggleAction = toggle
			};
			if (!enabled)
			{
				command.Disabled = true;
				command.disabledReason = "CK.Robotics.DisabledInSettings".Translate();
			}
			return command;
		}

		private static Texture2D Icon(string path)
		{
			return ContentFinder<Texture2D>.Get(path, false) ?? TexCommand.GatherSpotActive;
		}
	}
}
