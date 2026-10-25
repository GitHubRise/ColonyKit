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
				EnsureWarehouseOwner();
				return innerContainer;
			}
		}

		public int UsedStacks => Container.Count;

		public int UsedItems => Container.TotalStackCount;

		/// <summary>
		/// 旧存档里的 innerContainer 是普通 ThingOwner；读档后原地升级成
		/// <see cref="WarehouseThingOwner"/>，保留所有堆（包括超过全局上限的堆）。
		/// </summary>
		private void EnsureWarehouseOwner()
		{
			if (innerContainer is WarehouseThingOwner)
			{
				return;
			}

			WarehouseThingOwner upgraded = new WarehouseThingOwner(this);
			if (innerContainer != null)
			{
				foreach (Thing thing in innerContainer.ToList())
				{
					innerContainer.Remove(thing);
					upgraded.AddRestored(thing);
				}
			}
			innerContainer = upgraded;
		}

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
			innerContainer = new WarehouseThingOwner(this);
			settingsVersion = 5;
			EnsureStoreSettings();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			Scribe_Deep.Look(ref storeSettings, "storeSettings", this);
			EnsureWarehouseOwner();
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

			// CanAcceptAnyOf 会按库内倍率计算：能并进现有大堆，或还有空堆位。
			return Container.CanAcceptAnyOf(thing);
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

			// 库内堆可能超过全局上限；取出时按全局上限切成原版大小的堆，
			// 避免把超限堆直接丢到地图上导致截断/丢失。
			int count = Mathf.Max(1, Mathf.Min(thing.stackCount, thing.def.stackLimit));
			Thing taken = Container.Take(thing, count);
			if (taken == null)
			{
				return false;
			}

			if (!GenPlace.TryPlaceThing(taken, drop, Map, ThingPlaceMode.Direct, out _)
				&& !GenPlace.TryPlaceThing(taken, drop, Map, ThingPlaceMode.Near, out _))
			{
				if (Container is WarehouseThingOwner warehouseOwner)
				{
					warehouseOwner.TryAddForced(taken);
				}
				else
				{
					Container.TryAdd(taken, false);
				}
				return false;
			}
			Map?.resourceCounter?.UpdateResourceCounts();
			return true;
		}

		public void WithdrawAll()
		{
			int guard = Mathf.Max(1000, Container.TotalStackCount + Container.Count);
			while (Container.Count > 0 && guard-- > 0)
			{
				Thing first = Container[0];
				if (first == null || !TryWithdraw(first))
				{
					break;
				}
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

		/// <summary>
		/// 在建筑贴图中央圆环的正下方显示实时温度。
		/// 贴图里圆环底缘约在中心下方 0.293 格；这里从 0.27 格处开始画，
		/// 留出一小段间隔，同时让文字落在下方深色面板内、不压圆环和底边。
		/// </summary>
		public override void DrawGUIOverlay()
		{
			if (!Spawned || Map == null || Find.CameraDriver == null)
			{
				return;
			}

			// 常规/近景视角常驻；拉远后只在选中时显示，避免小图上的文字互相压住。
			if (Find.CameraDriver.CurrentZoom > CameraZoomRange.Middle && !Find.Selector.IsSelected(this))
			{
				return;
			}

			float temperature = CurrentRoomTemperature();
			string label = temperature.ToStringTemperature("F1");

			GameFont oldFont = Text.Font;
			TextAnchor oldAnchor = Text.Anchor;
			bool oldWrap = Text.WordWrap;
			Color oldColor = GUI.color;

			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperCenter;
			Text.WordWrap = false;
			float width = Text.CalcSize(label).x;
			Vector2 pos = GenMapUI.LabelDrawPosFor(this, -0.27f);
			Rect rect = new Rect(pos.x - width / 2f, pos.y, width, 20f);

			// 深色描边保证在深色面板和浅色地面上都能看清。
			GUI.color = new Color(0f, 0f, 0f, 0.7f);
			Widgets.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), label);
			GUI.color = TemperatureLabelColor(temperature);
			Widgets.Label(rect, label);

			GUI.color = oldColor;
			Text.WordWrap = oldWrap;
			Text.Anchor = oldAnchor;
			Text.Font = oldFont;
		}

		private static Color TemperatureLabelColor(float temperature)
		{
			if (temperature <= 0f)
			{
				return new Color(0.58f, 0.82f, 1f);
			}
			if (temperature >= 25f)
			{
				return new Color(1f, 0.66f, 0.38f);
			}
			return new Color(0.82f, 0.96f, 1f);
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

			int internalMultiplier = WarehouseThingOwner.EffectiveMultiplier;
			if (internalMultiplier > 1)
			{
				text += "\n" + string.Format(
					"CK.VerticalWarehouse.InternalStacking".Translate(),
					internalMultiplier);
			}

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
