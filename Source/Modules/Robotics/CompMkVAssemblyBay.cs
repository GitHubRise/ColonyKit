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

		/// <summary>一次 Mk-V 组装消耗的独立配方材料。</summary>
		public List<ThingDefCountClass> ingredients = new List<ThingDefCountClass>();

		public CompProperties_MkVAssemblyBay()
		{
			compClass = typeof(CompMkVAssemblyBay);
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
				if (power != null && !power.PowerOn)
				{
					ApplyPower();
					glower?.UpdateLit(parent.Map);
					return;
				}

				progressTicks += 15;
				if (progressTicks >= Props.workTicks)
				{
					CompleteAssembly();
				}
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
			if (power.PowerOutput != desired)
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
			foreach (ThingDefCountClass ingredient in Props.ingredients)
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
			foreach (ThingDefCountClass ingredient in Props.ingredients)
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
			if (power == null || !power.PowerOn)
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

			IntVec3 cell = CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, 4);
			if (!cell.IsValid || !cell.InBounds(parent.Map))
			{
				cell = parent.InteractionCell;
			}
			if (!cell.IsValid || !cell.InBounds(parent.Map))
			{
				cell = parent.Position;
			}

			int number = 1;
			foreach (Pawn other in parent.Map.mapPawns.AllPawnsSpawned)
			{
				if (NannyBotUtility.IsNannyBot(other))
				{
					number++;
				}
			}
			pawn.Name = new NameSingle(
				"CK.Robotics.RobotName".Translate(number).ToString(),
				true);
			if (pawn.playerSettings == null)
			{
				pawn.playerSettings = new Pawn_PlayerSettings(pawn);
			}

			GenSpawn.Spawn(pawn, cell, parent.Map, Rot4.South);
			CompNannyBot comp = pawn.TryGetComp<CompNannyBot>();
			if (comp != null)
			{
				comp.homeStation = parent as Building;
			}

			Messages.Message(
				"CK.Robotics.MkVBay.Completed".Translate(pawn.LabelShort),
				pawn,
				MessageTypeDefOf.PositiveEvent);
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
				defaultDesc = "CK.Robotics.MkVBay.Start.Desc".Translate(Props.ingredients
					.Select(i => $"{i.thingDef.label} x{i.count}")
					.ToLineList(" · ")),
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
			if (working)
			{
				return "CK.Robotics.MkVBay.InspectWorking".Translate(Progress.ToStringPercent());
			}
			if (!ResearchFinished)
			{
				return "CK.Robotics.MkVBay.InspectResearch".Translate();
			}
			return "CK.Robotics.MkVBay.InspectIdle".Translate();
		}
	}
}
