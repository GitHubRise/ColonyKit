using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_WashingMachine 的移植：把「死人穿过的衣服」洗干净（清掉 WornByCorpse）。
	/// 需要接进水管（抽水）和污水管（排污），通电后按 30 tick 一批转筒；
	/// 洗衣期间小人会围观（JoyGiver_WatchBuilding + JobDriver_WatchWashingMachine）。
	/// 和 DBH 的差异只在管道模型：DBH 用一张网同时供水排污，CK 是水网/污水网两张网，所以这里分别取。
	/// </summary>
	[StaticConstructorOnStartup]
	public class Building_WashingMachine : Building, IStoreSettingsParent, IThingHolder
	{
		/// <summary>转筒时的功耗，和 Def 的 basePowerConsumption 一致（1.6 该字段不公开）。</summary>
		private const float RunningPowerDraw = 420f;

		public ThingOwner innerContainer;

		private int lastTickUsed = -9999;

		public CompPipe waterPipe;

		public CompPipe sewagePipe;

		public CompPowerTrader powerComp;

		public int SpinCycleTicksLeft;

		public int storageCap = 5;

		private StorageSettings storageSettings;

		private Sustainer wickSustainer;

		public bool MustUnload => SpinCycleTicksLeft <= 0 && innerContainer.Count > 0;

		public bool HasSpace => innerContainer.Count < storageCap && !MustUnload;

		public bool StorageTabVisible => true;

		public Building_WashingMachine()
		{
			innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
		}

		public void Notify_SettingsChanged()
		{
		}

		public StorageSettings GetStoreSettings()
		{
			return storageSettings;
		}

		public StorageSettings GetParentStoreSettings()
		{
			return def.building.fixedStorageSettings;
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return innerContainer;
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		private void StartSustainer()
		{
			wickSustainer = SoundDef.Named("CK_WashingMachine").TrySpawnSustainer(
				SoundInfo.InMap(this, MaintenanceType.PerTick));
		}

		public override void PostMake()
		{
			base.PostMake();
			storageSettings = new StorageSettings(this);
			if (def.building.defaultStorageSettings != null)
			{
				storageSettings.CopyFrom(def.building.defaultStorageSettings);
			}
		}

		public AcceptanceReport Working()
		{
			if (waterPipe?.pipeNet == null || waterPipe.pipeNet.WaterStorage <= 10f)
			{
				return "CK.Hygiene.NoWaterSupply".Translate();
			}
			if (sewagePipe?.pipeNet == null
				|| !sewagePipe.pipeNet.Sewers.Any(s => s != null && !s.Blocked))
			{
				return "CK.Hygiene.NoSewage".Translate();
			}
			if (this.IsBrokenDown())
			{
				return false;
			}
			if (powerComp != null && !powerComp.PowerOn)
			{
				return "NoPower".Translate();
			}
			return true;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			foreach (CompPipe pipe in GetComps<CompPipe>())
			{
				if (pipe.mode == PipeType.Water)
				{
					waterPipe = pipe;
				}
				else if (pipe.mode == PipeType.Sewage)
				{
					sewagePipe = pipe;
				}
			}
			powerComp = GetComp<CompPowerTrader>();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref storageSettings, "storageSettings", this);
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			Scribe_Values.Look(ref SpinCycleTicksLeft, "spinCycleTicksLeft", 0);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
			{
				innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
			}
		}

		public void LoadWashing(Thing item)
		{
			innerContainer.TryAddOrTransfer(item, canMergeWithExistingStacks: false);
			SpinCycleTicksLeft += 1500;
		}

		public Thing UnloadWashing()
		{
			SpinCycleTicksLeft = 0;
			foreach (Apparel apparel in innerContainer.OfType<Apparel>())
			{
				apparel.WornByCorpse = false;
			}
			Thing first = innerContainer.FirstOrDefault();
			innerContainer.TryDrop(first, ThingPlaceMode.Near, out Thing lastResultingThing);
			innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near, null, NearPlaceValidator);
			return lastResultingThing;
		}

		private bool NearPlaceValidator(IntVec3 c)
		{
			return c.GetRoom(Map) == this.GetRoom();
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			if ((mode == DestroyMode.Deconstruct || mode == DestroyMode.Refund) && innerContainer != null)
			{
				innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
			}
			base.DeSpawn(mode);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			if (!StorageTabVisible || storageSettings == null)
			{
				yield break;
			}
			foreach (Gizmo gizmo in StorageSettingsClipboard.CopyPasteGizmosFor(storageSettings))
			{
				yield return gizmo;
			}
		}

		protected override void Tick()
		{
			base.Tick();
			if (powerComp != null)
			{
				powerComp.PowerOutput = -10f;
			}
			if (SpinCycleTicksLeft > 0
				&& this.IsHashIntervalTick(30)
				&& Working().Accepted
				&& waterPipe.pipeNet.PullWater(1f, out _))
			{
				sewagePipe.pipeNet.PushSewage(0.5f);
				lastTickUsed = Find.TickManager.TicksGame;
				SpinCycleTicksLeft -= 30;
			}
			if (Find.TickManager.TicksGame < lastTickUsed + 100)
			{
				if (powerComp != null)
				{
					// 1.6 的 CompProperties_Power.basePowerConsumption 是私有的，这里用 Def 里的 420W。
					powerComp.PowerOutput = 0f - RunningPowerDraw;
				}
				if (wickSustainer == null)
				{
					StartSustainer();
				}
				else if (wickSustainer.Ended)
				{
					StartSustainer();
				}
				else
				{
					wickSustainer.Maintain();
				}
			}
		}

		public override string GetInspectString()
		{
			if (ParentHolder is MinifiedThing)
			{
				return base.GetInspectString();
			}
			StringBuilder builder = new StringBuilder(base.GetInspectString());
			if (SpinCycleTicksLeft > 0)
			{
				builder.AppendLine();
				builder.Append("CK.Hygiene.WashingSpinning".Translate());
			}
			else if (MustUnload)
			{
				builder.AppendLine();
				builder.Append("CK.Hygiene.WashingUnload".Translate());
			}
			builder.AppendLine();
			builder.Append("CK.Hygiene.WashingLoad".Translate(innerContainer.Count, storageCap));
			AcceptanceReport report = Working();
			if (!report.Accepted)
			{
				builder.AppendLine();
				builder.Append(report.Reason);
			}
			return builder.ToString().TrimEndNewlines();
		}
	}
}
