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
		/// <summary>工厂等级；只能生产该等级及以下的机器人。</summary>
		public int factoryTier = 1;

		public PawnKindDef productKind;
		public float idlePower = 100f;
		public float workPower = 900f;

		public CompProperties_RobotFactory()
		{
			compClass = typeof(CompRobotFactory);
		}
	}

	/// <summary>一个可生产型号及其固定配方；按等级从低到高排列。</summary>
	public sealed class RobotFactoryProduct
	{
		public int tier;
		public PawnKindDef kind;
		public int workTicks;
		public int steelCount;
		public int componentCount;
		public int advancedComponentCount;
		public int plasteelCount;

		public List<ThingDefCountClass> Ingredients => new List<ThingDefCountClass>
		{
			new ThingDefCountClass(ThingDefOf.Steel, steelCount),
			new ThingDefCountClass(ThingDefOf.ComponentIndustrial, componentCount),
			new ThingDefCountClass(ThingDefOf.ComponentSpacer, advancedComponentCount),
			new ThingDefCountClass(ThingDefOf.Plasteel, plasteelCount)
		};
	}

	internal static class RobotFactoryProducts
	{
		private static List<RobotFactoryProduct> all;

		public static List<RobotFactoryProduct> All
		{
			get
			{
				if (all == null)
				{
					all = new List<RobotFactoryProduct>
					{
						Product(1, "CK_NannyBotMkIKind", 12000, 220, 8, 2, 20),
						Product(2, "CK_NannyBotMkIIKind", 15000, 260, 9, 2, 30),
						Product(3, "CK_NannyBotMkIIIKind", 12000, 140, 4, 1, 15),
						Product(4, "CK_NannyBotMkIVKind", 10000, 200, 7, 2, 30),
						Product(5, "CK_RobotMkVKind", 30000, 420, 14, 4, 80)
					};
				}
				return all;
			}
		}

		public static RobotFactoryProduct ForTier(int tier)
		{
			for (int i = 0; i < All.Count; i++)
			{
				if (All[i].tier == tier)
				{
					return All[i];
				}
			}
			return null;
		}

		private static RobotFactoryProduct Product(
			int tier,
			string kindName,
			int workTicks,
			int steel,
			int components,
			int advanced,
			int plasteel)
		{
			return new RobotFactoryProduct
			{
				tier = tier,
				kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindName),
				workTicks = workTicks,
				steelCount = steel,
				componentCount = components,
				advancedComponentCount = advanced,
				plasteelCount = plasteel
			};
		}
	}

	/// <summary>通用机器人工厂：按工厂等级提供自己的型号及以下型号的清单，独立配方、功耗和节拍。</summary>
	public class CompRobotFactory : ThingComp
	{
		private bool working;
		private int progressTicks;
		private int selectedTier;
		private CompPowerTrader power;

		private CompProperties_RobotFactory Props => (CompProperties_RobotFactory)props;

		private int MaxTier => Mathf.Clamp(Props.factoryTier, 1, 5);

		private RobotFactoryProduct SelectedProduct
		{
			get
			{
				int tier = selectedTier > 0 ? selectedTier : MaxTier;
				return RobotFactoryProducts.ForTier(Mathf.Clamp(tier, 1, MaxTier))
					?? RobotFactoryProducts.ForTier(MaxTier)
					?? RobotFactoryProducts.All[0];
			}
		}

		private PawnKindDef SelectedKind => SelectedProduct?.kind;

		private int TierDelta => SelectedProduct == null ? 0 : Mathf.Max(0, MaxTier - SelectedProduct.tier);

		private float TimeFactor => Mathf.Clamp(1f - 0.15f * TierDelta, 0.4f, 1f);

		private float MaterialFactor => Mathf.Clamp(1f - 0.12f * TierDelta, 0.5f, 1f);

		private float PowerFactor => Mathf.Clamp(1f - 0.10f * TierDelta, 0.5f, 1f);

		private int CurrentWorkTicks => SelectedProduct == null
			? 12000
			: Mathf.Max(1, Mathf.RoundToInt(SelectedProduct.workTicks * TimeFactor));

		private List<ThingDefCountClass> AdjustedIngredients
		{
			get
			{
				if (SelectedProduct == null)
				{
					return new List<ThingDefCountClass>();
				}
				int Scale(int value)
				{
					return Mathf.Max(1, Mathf.RoundToInt(value * MaterialFactor));
				}
				return new List<ThingDefCountClass>
				{
					new ThingDefCountClass(ThingDefOf.Steel, Scale(SelectedProduct.steelCount)),
					new ThingDefCountClass(ThingDefOf.ComponentIndustrial, Scale(SelectedProduct.componentCount)),
					new ThingDefCountClass(ThingDefOf.ComponentSpacer, Scale(SelectedProduct.advancedComponentCount)),
					new ThingDefCountClass(ThingDefOf.Plasteel, Scale(SelectedProduct.plasteelCount))
				};
			}
		}

		public bool IsWorking => working;

		public float Progress => CurrentWorkTicks <= 0 ? 0f : progressTicks / (float)CurrentWorkTicks;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			power = parent.TryGetComp<CompPowerTrader>();
			if (selectedTier <= 0)
			{
				selectedTier = MaxTier;
			}
			selectedTier = Mathf.Clamp(selectedTier, 1, MaxTier);
			ApplyPower();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref working, "ck_robotFactoryWorking", false);
			Scribe_Values.Look(ref progressTicks, "ck_robotFactoryProgress", 0);
			Scribe_Values.Look(ref selectedTier, "ck_robotFactorySelectedTier", 0);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (selectedTier <= 0)
				{
					selectedTier = MaxTier;
				}
				selectedTier = Mathf.Clamp(selectedTier, 1, MaxTier);
			}
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
				if (progressTicks >= CurrentWorkTicks)
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
			float desired = -(working ? Props.workPower * PowerFactor : Props.idlePower);
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
			foreach (ThingDefCountClass ingredient in AdjustedIngredients)
			{
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
			foreach (ThingDefCountClass ingredient in AdjustedIngredients)
			{
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
				reason = "CK.Robotics.Factory.AlreadyWorking".Translate(SelectedKind?.LabelCap ?? "?");
				return false;
			}
			if (SelectedKind == null)
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
				"CK.Robotics.Factory.Started".Translate(SelectedKind.LabelCap),
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
				"CK.Robotics.Factory.Cancelled".Translate(SelectedKind?.LabelCap ?? "?"),
				parent,
				MessageTypeDefOf.NeutralEvent);
		}

		private void CompleteAssembly()
		{
			working = false;
			progressTicks = 0;
			ApplyPower();

			PawnKindDef kind = SelectedKind;
			if (kind == null)
			{
				ModLog.Error("Robotics", "Robot factory has no selected kind; cannot spawn a robot.");
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
				"CK.Robotics.Factory.Completed".Translate(kind.LabelCap, pawn.LabelShort),
				pawn,
				MessageTypeDefOf.PositiveEvent);
		}

		private string DescribeProduct(RobotFactoryProduct product)
		{
			int delta = Mathf.Max(0, MaxTier - product.tier);
			float timeFactor = Mathf.Clamp(1f - 0.15f * delta, 0.4f, 1f);
			float materialFactor = Mathf.Clamp(1f - 0.12f * delta, 0.5f, 1f);
			int workTicks = Mathf.Max(1, Mathf.RoundToInt(product.workTicks * timeFactor));
			int Scale(int value)
			{
				return Mathf.Max(1, Mathf.RoundToInt(value * materialFactor));
			}
			string ingredients = string.Join(" / ", new[]
			{
				$"{ThingDefOf.Steel.label} {Scale(product.steelCount)}",
				$"{ThingDefOf.ComponentIndustrial.label} {Scale(product.componentCount)}",
				$"{ThingDefOf.ComponentSpacer.label} {Scale(product.advancedComponentCount)}",
				$"{ThingDefOf.Plasteel.label} {Scale(product.plasteelCount)}"
			});
			return product.kind.LabelCap + " · " + workTicks.ToStringTicksToPeriod() + " · " + ingredients;
		}

		private void OpenProductMenu()
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			for (int tier = 1; tier <= MaxTier; tier++)
			{
				RobotFactoryProduct product = RobotFactoryProducts.ForTier(tier);
				if (product?.kind == null)
				{
					continue;
				}
				RobotFactoryProduct captured = product;
				options.Add(new FloatMenuOption(DescribeProduct(captured), () =>
				{
					selectedTier = captured.tier;
				}));
			}
			if (options.Count == 0)
			{
				Messages.Message("CK.Robotics.Factory.MissingKind".Translate(),
					MessageTypeDefOf.RejectInput, false);
				return;
			}
			Find.WindowStack.Add(new FloatMenu(options));
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
					defaultDesc = "CK.Robotics.Factory.Cancel.Desc".Translate(SelectedKind?.LabelCap ?? "?"),
					icon = TexCommand.CannotShoot,
					action = CancelAssembly
				};
				yield break;
			}

			yield return new Command_Action
			{
				defaultLabel = "CK.Robotics.Factory.Select".Translate(SelectedKind?.LabelCap ?? "?"),
				defaultDesc = "CK.Robotics.Factory.Select.Desc".Translate(MaxTier),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/CK_Assemble", false)
					?? TexCommand.Install,
				action = OpenProductMenu
			};

			Command_Action start = new Command_Action
			{
				defaultLabel = "CK.Robotics.Factory.Start".Translate(SelectedKind?.LabelCap ?? "?"),
				defaultDesc = "CK.Robotics.Factory.Start.Desc".Translate(
					SelectedKind?.LabelCap ?? "?",
					string.Join(" · ", AdjustedIngredients.Select(i => $"{i.thingDef.label} x{i.count}"))),
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
					SelectedKind?.LabelCap ?? "?",
					Progress.ToStringPercent());
			}
			return "CK.Robotics.Factory.InspectIdle".Translate(SelectedKind?.LabelCap ?? "?")
				+ "\n" + "CK.Robotics.Factory.Available".Translate(DescribeList());
		}

		private string DescribeList()
		{
			List<string> labels = new List<string>();
			for (int tier = 1; tier <= MaxTier; tier++)
			{
				RobotFactoryProduct product = RobotFactoryProducts.ForTier(tier);
				if (product?.kind != null)
				{
					labels.Add(product.kind.LabelCap);
				}
			}
			return string.Join(" / ", labels);
		}
	}
}
