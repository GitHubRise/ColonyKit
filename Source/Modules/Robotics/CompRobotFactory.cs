using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	public class CompProperties_RobotFactory : CompProperties
	{
		public PawnKindDef productKind;
		public int workTicks = 12000;
		public float idlePower = 100f;
		public float workPower = 900f;
		public int steelCount = 220;
		public int componentCount = 8;
		public int advancedComponentCount = 2;
		public int plasteelCount = 20;

		public CompProperties_RobotFactory()
		{
			compClass = typeof(CompRobotFactory);
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

	/// <summary>通用机器人工厂：独立配方、功耗和节拍，按 Def 指定产出的 PawnKind。</summary>
	public class CompRobotFactory : ThingComp
	{
		private bool working;
		private int progressTicks;
		private CompPowerTrader power;

		private CompProperties_RobotFactory Props => (CompProperties_RobotFactory)props;

		private PawnKindDef ProductKind => Props.productKind;

		public bool IsWorking => working;

		public float Progress => Props.workTicks <= 0 ? 0f : progressTicks / (float)Props.workTicks;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			power = parent.TryGetComp<CompPowerTrader>();
			ApplyPower();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref working, "ck_robotFactoryWorking", false);
			Scribe_Values.Look(ref progressTicks, "ck_robotFactoryProgress", 0);
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
					return;
				}
				progressTicks += 15;
				if (progressTicks >= Props.workTicks)
				{
					CompleteAssembly();
					return;
				}
			}
			ApplyPower();
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

		private bool IsPowered => power != null && power.PowerOn;

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
				reason = "CK.Robotics.Factory.AlreadyWorking".Translate(ProductKind?.LabelCap ?? "?");
				return false;
			}
			if (ProductKind == null)
			{
				reason = "CK.Robotics.Factory.MissingKind".Translate();
				return false;
			}
			if (!IsPowered)
			{
				reason = "CK.Robotics.Factory.NoPower".Translate();
				return false;
			}
			if (!HasIngredients(parent.Map))
			{
				reason = "CK.Robotics.Factory.MissingIngredients".Translate();
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
			ConsumeIngredients(parent.Map);
			working = true;
			progressTicks = 0;
			ApplyPower();
			Messages.Message(
				"CK.Robotics.Factory.Started".Translate(ProductKind.LabelCap),
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
				"CK.Robotics.Factory.Cancelled".Translate(ProductKind?.LabelCap ?? "?"),
				parent,
				MessageTypeDefOf.NeutralEvent);
		}

		private void CompleteAssembly()
		{
			working = false;
			progressTicks = 0;
			ApplyPower();

			PawnKindDef kind = ProductKind;
			if (kind == null)
			{
				ModLog.Error("Robotics", "Robot factory has no productKind; cannot spawn a robot.");
				return;
			}

			Pawn pawn = PawnGenerator.GeneratePawn(kind, Faction.OfPlayer);
			if (pawn == null)
			{
				ModLog.Error("Robotics", $"PawnGenerator returned null for {kind.defName}.");
				return;
			}

			Map map = parent.Map;
			IntVec3 cell = CellFinder.RandomClosewalkCellNear(parent.Position, map, 4);
			if (!cell.IsValid || !cell.InBounds(map))
			{
				cell = parent.InteractionCell;
			}
			if (!cell.IsValid || !cell.InBounds(map))
			{
				cell = parent.Position;
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
				"CK.Robotics.FactoryName".Translate(kind.LabelCap, number).ToString(),
				true);
			if (pawn.playerSettings == null)
			{
				pawn.playerSettings = new Pawn_PlayerSettings(pawn);
			}

			GenSpawn.Spawn(pawn, cell, map, Rot4.South);
			if (!pawn.Spawned)
			{
				ModLog.Error("Robotics", "Robot factory could not spawn its robot.");
				return;
			}

			if (NannyBotDefOf.CK_ArcEmitter != null && pawn.equipment != null
				&& pawn.equipment.Primary == null && NannyBotUtility.IsMkV(pawn))
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
				comp.homeStation = NannyBotUtility.FindStation(pawn, false);
			}
			CompMkVCombat combat = pawn.TryGetComp<CompMkVCombat>();
			if (combat != null)
			{
				combat.homeBay = parent as Building;
			}

			Messages.Message(
				"CK.Robotics.Factory.Completed".Translate(ProductKind.LabelCap, pawn.LabelShort),
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
					defaultLabel = "CK.Robotics.Factory.Cancel".Translate(),
					defaultDesc = "CK.Robotics.Factory.Cancel.Desc".Translate(ProductKind?.LabelCap ?? "?"),
					icon = TexCommand.CannotShoot,
					action = CancelAssembly
				};
				yield break;
			}

			Command_Action start = new Command_Action
			{
				defaultLabel = "CK.Robotics.Factory.Start".Translate(ProductKind?.LabelCap ?? "?"),
				defaultDesc = "CK.Robotics.Factory.Start.Desc".Translate(
					ProductKind?.LabelCap ?? "?",
					string.Join(" · ", Props.GetIngredients().Select(i => $"{i.thingDef.label} x{i.count}"))),
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
				return "CK.Robotics.Factory.InspectWorking".Translate(
					ProductKind?.LabelCap ?? "?",
					Progress.ToStringPercent());
			}
			return "CK.Robotics.Factory.InspectIdle".Translate(ProductKind?.LabelCap ?? "?");
		}
	}
}
