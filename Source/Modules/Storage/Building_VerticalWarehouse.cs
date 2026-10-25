using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 2×2 立体库：内部 <see cref="ThingOwner{T}"/> 保管物品，面板直接存取。
	/// 默认收食物/草药/干草/木头/植物类物资/麦芽（Wort）；温控默认 4°C。
	/// </summary>
	public class Building_VerticalWarehouse : Building, IThingHolder, IStoreSettingsParent
	{
		/// <summary>每座立体库可存的堆数；从模块设置读取，默认 24，设置页可调。</summary>
		public static int MaxStacks
		{
			get
			{
				ColonyKit.Modules.Storage.StorageSettings data =
					ColonyKitMod.Settings?.GetModuleSettings<ColonyKit.Modules.Storage.StorageSettings>();
				int value = data?.warehouseStackCapacity ?? StorageSettings.DefaultWarehouseStacks;
				return Mathf.Clamp(value, StorageSettings.MinWarehouseStacks, StorageSettings.MaxWarehouseStacks);
			}
		}

		private ThingOwner<Thing> innerContainer;
		private RimWorld.StorageSettings storeSettings;
		private CompTempControl compTempControl;
		private CompPowerTrader compPowerTrader;
		private int settingsVersion;

		public ThingOwner<Thing> Container
		{
			get
			{
				if (innerContainer == null)
				{
					innerContainer = new ThingOwner<Thing>(this);
				}
				return innerContainer;
			}
		}

		public int UsedStacks => Container.Count;

		public int UsedItems => Container.TotalStackCount;

		public new IThingHolder ParentHolder => base.ParentHolder;

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return Container;
		}

		public bool HasPower => compPowerTrader?.PowerOn ?? GetComp<CompPowerTrader>()?.PowerOn ?? false;

		public bool IsFull => UsedStacks >= MaxStacks;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			compTempControl = GetComp<CompTempControl>();
			compPowerTrader = GetComp<CompPowerTrader>();
			if (!respawningAfterLoad && compTempControl != null)
			{
				compTempControl.TargetTemperature = 4f;
			}
		}

		public Building_VerticalWarehouse()
		{
			innerContainer = new ThingOwner<Thing>(this);
			settingsVersion = 5;
			EnsureStoreSettings();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			Scribe_Deep.Look(ref storeSettings, "storeSettings", this);
			if (innerContainer == null)
			{
				innerContainer = new ThingOwner<Thing>(this);
			}
			EnsureStoreSettings();

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (settingsVersion < 4)
				{
					MigrateLegacyDefaults();
				}
				else if (settingsVersion < 5)
				{
					// v4 仓库保留玩家自定义过滤，只追加本次新增的默认物资。
					AddExtendedDefaultItems();
					settingsVersion = 5;
				}
			}
		}

		private void MigrateLegacyDefaults()
		{
			// 0.0.2 早期默认是 Low/21°C，且允许腐烂/尸体/堆块；旧存档首次加载时迁移。
			if (storeSettings != null && storeSettings.Priority == StoragePriority.Low)
			{
				storeSettings.Priority = StoragePriority.Normal;
			}

			compTempControl ??= GetComp<CompTempControl>();
			if (compTempControl != null && Mathf.Approximately(compTempControl.TargetTemperature, 21f))
			{
				compTempControl.TargetTemperature = 4f;
			}

			ApplyDefaultFilterRules();
			settingsVersion = 5;
		}

		private void EnsureStoreSettings()
		{
			if (storeSettings != null)
			{
				return;
			}

			storeSettings = new RimWorld.StorageSettings(this);
			// 默认普通优先级，和原版库存区一致。
			storeSettings.Priority = StoragePriority.Normal;
			ApplyDefaultFilterRules();
		}

		private void ApplyDefaultFilterRules()
		{
			if (storeSettings?.filter == null)
			{
				return;
			}

			storeSettings.filter.SetDisallowAll();
			if (ThingCategoryDefOf.Foods != null)
			{
				storeSettings.filter.SetAllow(ThingCategoryDefOf.Foods, true);
			}
			if (ThingDefOf.MedicineHerbal != null)
			{
				storeSettings.filter.SetAllow(ThingDefOf.MedicineHerbal, true);
			}
			if (ThingDefOf.Hay != null)
			{
				storeSettings.filter.SetAllow(ThingDefOf.Hay, true);
			}
			AddExtendedDefaultItems();
			SpecialThingFilterDef rotten = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowRotten");
			if (rotten != null)
			{
				storeSettings.filter.SetAllow(rotten, false);
			}
		}

		/// <summary>本次扩展的默认物资：木头、植物类、麦芽（Wort）。</summary>
		private void AddExtendedDefaultItems()
		{
			if (storeSettings?.filter == null)
			{
				return;
			}
			if (ThingDefOf.WoodLog != null)
			{
				storeSettings.filter.SetAllow(ThingDefOf.WoodLog, true);
			}
			if (ThingCategoryDefOf.PlantMatter != null)
			{
				storeSettings.filter.SetAllow(ThingCategoryDefOf.PlantMatter, true);
			}
			if (ThingDefOf.Wort != null)
			{
				storeSettings.filter.SetAllow(ThingDefOf.Wort, true);
			}
		}

		public bool StorageTabVisible => true;

		public RimWorld.StorageSettings GetStoreSettings()
		{
			EnsureStoreSettings();
			return storeSettings;
		}

		public RimWorld.StorageSettings GetParentStoreSettings()
		{
			return null;
		}

		public void Notify_SettingsChanged()
		{
			// 过滤器/优先级变化由 ITab_Storage 直接写进 storeSettings；这里不需要额外缓存。
		}

		public bool Accepts(Thing thing)
		{
			if (thing == null || thing.def == null || thing.stackCount <= 0)
			{
				return false;
			}
			if (thing.def.category != ThingCategory.Item || thing.def.IsCorpse)
			{
				return false;
			}
			if (thing is MinifiedThing)
			{
				return false;
			}

			EnsureStoreSettings();
			return storeSettings.AllowedToAccept(thing);
		}

		public bool CanStore(Thing thing)
		{
			if (!Accepts(thing))
			{
				return false;
			}
			if (UsedStacks < MaxStacks)
			{
				return true;
			}

			foreach (Thing stored in Container)
			{
				if (stored.CanStackWith(thing) && stored.stackCount < stored.def.stackLimit)
				{
					return true;
				}
			}
			return false;
		}

		public bool TryStore(Thing thing)
		{
			if (thing == null || !CanStore(thing))
			{
				return false;
			}

			if (thing.Spawned)
			{
				thing.DeSpawn();
			}

			if (!Container.TryAdd(thing, true))
			{
				if (!thing.Spawned && Map != null)
				{
					GenPlace.TryPlaceThing(thing, InteractionCell, Map, ThingPlaceMode.Near, out _);
				}
				return false;
			}
			Map?.resourceCounter?.UpdateResourceCounts();
			return true;
		}

		/// <summary>兼容已暂停开发的履带代码：本地存入即可。</summary>
		public bool TryStoreInNetwork(Thing thing)
		{
			return TryStore(thing);
		}

		public bool TryWithdraw(Thing thing)
		{
			// 取货由玩家/小人处理，不再自动放到履带上。
			return TryWithdrawAt(thing, InteractionCell);
		}

		public bool TryWithdrawAt(Thing thing, IntVec3 drop)
		{
			if (thing == null || !Container.Contains(thing))
			{
				return false;
			}

			Thing taken = Container.Take(thing);
			if (taken == null)
			{
				return false;
			}

			if (!GenPlace.TryPlaceThing(taken, drop, Map, ThingPlaceMode.Direct, out _)
				&& !GenPlace.TryPlaceThing(taken, drop, Map, ThingPlaceMode.Near, out _))
			{
				Container.TryAdd(taken, false);
				return false;
			}
			Map?.resourceCounter?.UpdateResourceCounts();
			return true;
		}

		public void WithdrawAll()
		{
			foreach (Thing thing in Container.ToList())
			{
				TryWithdraw(thing);
			}
		}

		/// <summary>
		/// 像原版空调一样把所在房间的温度调向 TargetTemperature。
		/// 默认 4°C；CompTempControl 自带 +/- 10°、+/-1°、重置按钮。
		/// </summary>
		public override void TickRare()
		{
			base.TickRare();
			if (!Spawned || Map == null)
			{
				return;
			}

			// 存储与自动输出不依赖电力；断电只停止温控。
			compTempControl ??= GetComp<CompTempControl>();
			compPowerTrader ??= GetComp<CompPowerTrader>();

			if (compTempControl == null || compPowerTrader == null)
			{
				return;
			}

			if (!compPowerTrader.PowerOn)
			{
				compTempControl.operatingAtHighPower = false;
				return;
			}

			Room room = FindRoom();
			if (room == null || room.UsesOutdoorTemperature)
			{
				compTempControl.operatingAtHighPower = false;
				SetIdlePower();
				return;
			}

			IntVec3 roomCell = IntVec3.Invalid;
			foreach (IntVec3 cell in room.Cells)
			{
				roomCell = cell;
				break;
			}
			if (!roomCell.IsValid)
			{
				SetIdlePower();
				return;
			}

			float energy = compTempControl.Props.energyPerSecond * 4.1666665f;
			if (!StorageResearch.Tier3Finished)
			{
				compTempControl.TargetTemperature = 4f;
			}
			else
			{
				compTempControl.TargetTemperature = Mathf.Clamp(
					compTempControl.TargetTemperature, -20f, 30f);
			}
			float change = GenTemperature.ControlTemperatureTempChange(
				roomCell,
				Map,
				energy,
				compTempControl.TargetTemperature);
			bool active = !Mathf.Approximately(change, 0f);
			if (active)
			{
				room.Temperature += change;
			}

			compTempControl.operatingAtHighPower = active;
			float power = compPowerTrader.Props.PowerConsumption;
			compPowerTrader.PowerOutput = active
				? -power
				: -power * compTempControl.Props.lowPowerConsumptionFactor;
		}

		private void SetIdlePower()
		{
			if (compTempControl == null || compPowerTrader == null)
			{
				return;
			}
			compPowerTrader.PowerOutput =
				-compPowerTrader.Props.PowerConsumption * compTempControl.Props.lowPowerConsumptionFactor;
		}

		private Room FindRoom()
		{
			Room room = this.GetRoom();
			if (room != null && !room.UsesOutdoorTemperature)
			{
				return room;
			}

			Room best = null;
			foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this))
			{
				if (!cell.InBounds(Map))
				{
					continue;
				}
				Room adjacent = cell.GetRoom(Map);
				if (adjacent == null || adjacent.UsesOutdoorTemperature)
				{
					continue;
				}
				if (best == null || adjacent.CellCount > best.CellCount)
				{
					best = adjacent;
				}
			}
			return best;
		}

		private float CurrentRoomTemperature()
		{
			Room room = FindRoom();
			return room != null && !room.UsesOutdoorTemperature
				? room.Temperature
				: Map.mapTemperature.OutdoorTemp;
		}

		public int CollectNearby(int radius = 8)
		{
			if (Map == null)
			{
				return 0;
			}

			int stored = 0;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, radius, true))
			{
				if (!cell.InBounds(Map))
				{
					continue;
				}

				foreach (Thing thing in Map.thingGrid.ThingsListAt(cell).ToList())
				{
					if (thing == this || thing.holdingOwner != null || !thing.Spawned)
					{
						continue;
					}
					if (thing.IsForbidden(Faction.OfPlayer))
					{
						continue;
					}
					if (TryStore(thing))
					{
						stored++;
					}
				}
			}
			return stored;
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			if (Map != null)
			{
				WithdrawAll();
			}
			base.Destroy(mode);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}

			text += string.Format(
				"CK.VerticalWarehouse.Inspect".Translate(),
				UsedStacks,
				MaxStacks,
				UsedItems);

			text += "\n" + string.Format(
				"CK.VerticalWarehouse.CurrentTemperature".Translate(),
				CurrentRoomTemperature().ToStringTemperature("F1"));

			if (!HasPower)
			{
				text += "\n" + "CK.VerticalWarehouse.NoPower".Translate();
			}
			return text;
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}

			yield return new Command_Action
			{
				defaultLabel = "CK.VerticalWarehouse.Collect".Translate(),
				defaultDesc = "CK.VerticalWarehouse.CollectDesc".Translate(),
				action = () =>
				{
					int count = CollectNearby();
					Messages.Message(
						string.Format("CK.VerticalWarehouse.Collected".Translate(), count),
						MessageTypeDefOf.NeutralEvent,
						false);
				}
			};

			yield return new Command_Action
			{
				defaultLabel = "CK.VerticalWarehouse.WithdrawAll".Translate(),
				defaultDesc = "CK.VerticalWarehouse.WithdrawAllDesc".Translate(),
				action = () =>
				{
					WithdrawAll();
					Messages.Message("CK.VerticalWarehouse.Withdrawn".Translate(), MessageTypeDefOf.NeutralEvent, false);
				}
			};
		}
	}
}
