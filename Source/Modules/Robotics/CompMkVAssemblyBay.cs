using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	public class CompProperties_MkVAssemblyBay : CompProperties
	{
		/// <summary>完整组装一次所需的 tick；30000 tick 约 12 小时。</summary>
		public int workTicks = 30000;

		/// <summary>待机功耗（W）。</summary>
		public float idlePower = 100f;

		/// <summary>组装时功耗（W）。</summary>
		public float workPower = 1500f;

		/// <summary>独立配方材料数量；固定四种材料，避免在 XML 中解析复杂列表。</summary>
		public int steelCount = 350;
		public int componentCount = 12;
		public int advancedComponentCount = 8;
		public int plasteelCount = 40;

		public CompProperties_MkVAssemblyBay()
		{
			compClass = typeof(CompMkVAssemblyBay);
		}

		public List<ThingDefCountClass> GetIngredients()
		{
			return new List<ThingDefCountClass>
			{
				new ThingDefCountClass(ThingDefOf.Steel, steelCount),
				new ThingDefCountClass(ThingDefOf.ComponentIndustrial, componentCount),
				new ThingDefCountClass(ThingDefOf.ComponentSpacer, advancedComponentCount),
				new ThingDefCountClass(ThingDefOf.Plasteel, plasteelCount)
			};
		}
	}

	/// <summary>
	/// Mk-V 专用组装舱：独立建筑、独立配方、独立功耗与生产节拍。
	/// 不影响保姆机器人工作站的 Mk-I 生产。
	/// </summary>
	public class CompMkVAssemblyBay : ThingComp
	{
		private bool working;
		private int progressTicks;
		private CompPowerTrader power;
		private CompGlower glower;

		private CompProperties_MkVAssemblyBay Props =>
			(CompProperties_MkVAssemblyBay)props;

		private bool ResearchFinished
		{
			get
			{
				ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("CK_RobotMkV");
				return project != null && project.IsFinished;
			}
		}

		public bool IsWorking => working;

		public bool IsPowered => power != null && power.PowerOn;

		public float Progress => Props.workTicks <= 0
			? 0f
			: progressTicks / (float)Props.workTicks;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			power = parent.TryGetComp<CompPowerTrader>();
			glower = parent.TryGetComp<CompGlower>();
			ApplyPower();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref working, "ck_mkvBayWorking", false);
			Scribe_Values.Look(ref progressTicks, "ck_mkvBayProgress", 0);
		}

		public override void CompTick()
		{
			if (!parent.IsHashIntervalTick(15))
			{
				return;
			}

			if (working)
			{
				if (!IsPowered)
				{
					ApplyPower();
					glower?.UpdateLit(parent.Map);
					return;
				}

				progressTicks += 15;
				if (progressTicks >= Props.workTicks)
				{
					CompleteAssembly();
					return;
				}
			}

			if (!working && NannyBotUtility.Settings.mkvBayAutoStart
				&& parent.IsHashIntervalTick(600) && CanStart(out _))
			{
				StartAssembly();
			}

			ApplyPower();
			glower?.UpdateLit(parent.Map);
		}

		private void ApplyPower()
		{
			if (power == null)
			{
				return;
			}
			float desired = -(working ? Props.workPower : Props.idlePower);
			if (!Mathf.Approximately(power.PowerOutput, desired))
			{
				power.PowerOutput = desired;
			}
		}

		private bool HasIngredients(Map map)
		{
			if (map == null)
			{
				return false;
			}
			foreach (ThingDefCountClass ingredient in Props.GetIngredients())
			{
				if (ingredient?.thingDef == null || ingredient.count <= 0)
				{
					continue;
				}
				if (map.resourceCounter.GetCount(ingredient.thingDef) < ingredient.count)
				{
					return false;
				}
			}
			return true;
		}

		private void ConsumeIngredients(Map map)
		{
			if (map == null)
			{
				return;
			}
			foreach (ThingDefCountClass ingredient in Props.GetIngredients())
			{
				if (ingredient?.thingDef == null || ingredient.count <= 0)
				{
					continue;
				}
				int remaining = ingredient.count;
				foreach (Thing thing in map.listerThings.ThingsOfDef(ingredient.thingDef).ToList())
				{
					if (remaining <= 0)
					{
						break;
					}
					if (thing == null || thing.Destroyed || !thing.Spawned || thing.stackCount <= 0)
					{
						continue;
					}
					int take = System.Math.Min(remaining, thing.stackCount);
					Thing split = thing.SplitOff(take);
					if (split != null && !split.Destroyed)
					{
						split.Destroy(DestroyMode.Vanish);
					}
					remaining -= take;
				}
			}
		}

		public bool CanStart(out string reason)
		{
			if (working)
			{
				reason = "CK.Robotics.MkVBay.AlreadyWorking".Translate();
				return false;
			}
			if (!ResearchFinished)
			{
				reason = "CK.Robotics.MkVBay.MissingResearch".Translate();
				return false;
			}
			if (!IsPowered)
			{
				reason = "CK.Robotics.MkVBay.NoPower".Translate();
				return false;
			}
			if (NannyBotDefOf.CK_RobotMkVKind == null)
			{
				reason = "CK.Robotics.MkVBay.MissingKind".Translate();
				return false;
			}
			if (!HasIngredients(parent.Map))
			{
				reason = "CK.Robotics.MkVBay.MissingIngredients".Translate();
				return false;
			}
			reason = null;
			return true;
		}

		public bool StartAssembly()
		{
			if (!CanStart(out string reason))
			{
				Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
				return false;
			}
			if (!HasIngredients(parent.Map))
			{
				Messages.Message("CK.Robotics.MkVBay.MissingIngredients".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}

			ConsumeIngredients(parent.Map);
			working = true;
			progressTicks = 0;
			ApplyPower();
			glower?.UpdateLit(parent.Map);
			Messages.Message(
				"CK.Robotics.MkVBay.Started".Translate(),
				parent,
				MessageTypeDefOf.PositiveEvent);
			return true;
		}

		public void CancelAssembly()
		{
			if (!working)
			{
				return;
			}
			working = false;
			progressTicks = 0;
			ApplyPower();
			glower?.UpdateLit(parent.Map);
			Messages.Message(
				"CK.Robotics.MkVBay.Cancelled".Translate(),
				parent,
				MessageTypeDefOf.NeutralEvent);
		}

		private void CompleteAssembly()
		{
			working = false;
			progressTicks = 0;
			ApplyPower();
			glower?.UpdateLit(parent.Map);

			if (NannyBotDefOf.CK_RobotMkVKind == null)
			{
				ModLog.Error("Robotics", "CK_RobotMkVKind missing; Mk-V assembly bay cannot spawn the robot.");
				return;
			}

			Pawn pawn = PawnGenerator.GeneratePawn(NannyBotDefOf.CK_RobotMkVKind, Faction.OfPlayer);
			if (pawn == null)
			{
				ModLog.Error("Robotics", "PawnGenerator returned null for CK_RobotMkVKind.");
				return;
			}

			Map map = parent.Map;
			IntVec3 cell = CellFinder.RandomClosewalkCellNear(
				parent.Position,
				map,
				4,
				c => c.Standable(map) && c.GetEdifice(map) == null && !c.Fogged(map));
			if (!IsUsableSpawnCell(cell, map))
			{
				cell = parent.InteractionCell;
			}
			if (!IsUsableSpawnCell(cell, map))
			{
				ModLog.Error("Robotics", "No standable cell near the Mk-V assembly bay; robot was not spawned.");
				return;
			}

			int number = 1;
			foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
			{
				if (NannyBotUtility.IsNannyBot(other))
				{
					number++;
				}
			}
			pawn.Name = new NameSingle(
				"CK.Robotics.MkVName".Translate(number).ToString(),
				true);
			if (pawn.playerSettings == null)
			{
				pawn.playerSettings = new Pawn_PlayerSettings(pawn);
			}

			GenSpawn.Spawn(pawn, cell, map, Rot4.South);
			if (!pawn.Spawned)
			{
				ModLog.Error("Robotics", "Mk-V pawn could not be spawned next to the assembly bay.");
				return;
			}
			if (NannyBotDefOf.CK_ArcEmitter != null && pawn.equipment != null && pawn.equipment.Primary == null)
			{
				ThingWithComps weapon = ThingMaker.MakeThing(NannyBotDefOf.CK_ArcEmitter) as ThingWithComps;
				if (weapon != null)
				{
					pawn.equipment.AddEquipment(weapon);
				}
			}
			CompNannyBot comp = pawn.TryGetComp<CompNannyBot>();
			if (comp != null)
			{
				// 组装舱只负责生产，不兼作充电桩；让 Mk-V 使用最近的保姆机器人工作站。
				comp.homeStation = NannyBotUtility.FindStation(pawn, false);
			}
			CompMkVCombat combat = pawn.TryGetComp<CompMkVCombat>();
			if (combat != null)
			{
				combat.homeBay = parent as Building;
			}

			Messages.Message(
				"CK.Robotics.MkVBay.Completed".Translate(pawn.LabelShort),
				pawn,
				MessageTypeDefOf.PositiveEvent);
		}

		private static bool IsUsableSpawnCell(IntVec3 cell, Map map)
		{
			return cell.IsValid
				&& cell.InBounds(map)
				&& cell.Standable(map)
				&& cell.GetEdifice(map) == null;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (!ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId) || !parent.Spawned)
			{
				yield break;
			}

			if (working)
			{
				yield return new Command_Action
				{
					defaultLabel = "CK.Robotics.MkVBay.Cancel".Translate(),
					defaultDesc = "CK.Robotics.MkVBay.Cancel.Desc".Translate(),
					icon = TexCommand.CannotShoot,
					action = CancelAssembly
				};
				yield break;
			}

			Command_Action start = new Command_Action
			{
				defaultLabel = "CK.Robotics.MkVBay.Start".Translate(),
				defaultDesc = "CK.Robotics.MkVBay.Start.Desc".Translate(string.Join(" · ",
					Props.GetIngredients().Select(i => $"{i.thingDef.label} x{i.count}"))),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/CK_Assemble", false)
					?? TexCommand.Install,
				action = () => StartAssembly()
			};

			if (!CanStart(out string reason))
			{
				start.Disabled = true;
				start.disabledReason = reason;
			}
			yield return start;
		}

		public override string CompInspectStringExtra()
		{
			List<string> lines = new List<string>();
			if (working)
			{
				lines.Add("CK.Robotics.MkVBay.InspectWorking".Translate(Progress.ToStringPercent()));
				if (!IsPowered)
				{
					lines.Add("CK.Robotics.MkVBay.NoPower".Translate());
				}
				return string.Join("\n", lines);
			}
			if (!ResearchFinished)
			{
				return "CK.Robotics.MkVBay.InspectResearch".Translate();
			}
			lines.Add("CK.Robotics.MkVBay.InspectIdle".Translate());
			if (!IsPowered)
			{
				lines.Add("CK.Robotics.MkVBay.NoPower".Translate());
			}
			return string.Join("\n", lines);
		}
	}
}
