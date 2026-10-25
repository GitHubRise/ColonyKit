using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 4×4 立体库原型：内部 <see cref="ThingOwner{T}"/> 保管物品，面板/Gizmo 直接存取。
	/// 四个方向各有一个履带接口；自动入/出库要求至少接一条履带。
	/// 自带可调温控（CompTempControl），默认 4°C，像原版空调一样用 Gizmo 调温。
	/// </summary>
	[StaticConstructorOnStartup]
	public class Building_VerticalWarehouse : Building, IThingHolder, IStoreSettingsParent
	{
		public const int MaxStacks = 24;

		private ThingOwner<Thing> innerContainer;
		private RimWorld.StorageSettings storeSettings;
		private CompTempControl compTempControl;
		private CompPowerTrader compPowerTrader;
		private int networkCacheTick = -1;
		private bool networkCache;
		private int settingsVersion;
		private bool autoOutputToConveyor = true;
		private static Graphic conveyorWarning;

		private static Graphic ConveyorWarning
		{
			get
			{
				if (conveyorWarning == null)
				{
					conveyorWarning = GraphicDatabase.Get<Graphic_Single>(
						"Things/Building/Storage/CK_ConveyorWarning",
						ShaderDatabase.Cutout,
						new Vector2(0.7f, 0.7f),
						Color.white);
				}
				return conveyorWarning;
			}
		}

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

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);

			if (HasAnyConveyorConnection || Find.TickManager == null)
			{
				return;
			}

			// 没接履带时，像断电图标一样闪烁；网络里任意一台接了履带就不会闪。
			if ((Find.TickManager.TicksGame / 30) % 2 != 0)
			{
				return;
			}

			Vector3 position = drawLoc + new Vector3(0f, 0.55f, 0f);
			ConveyorWarning.Draw(position, Rot4.North, this);
		}

		/// <summary>每条边中央的接口格（建筑外侧）；按 Def 实际尺寸计算。</summary>
		public IntVec3 PortCell(Rot4 side)
		{
			int width = def.size.x;
			int height = def.size.z;
			switch (side.AsInt)
			{
				case 0:
					return Position + new IntVec3(width / 2, 0, height); // North
				case 1:
					return Position + new IntVec3(width, 0, height / 2); // East
				case 2:
					return Position + new IntVec3(width / 2, 0, -1); // South
				default:
					return Position + new IntVec3(-1, 0, height / 2); // West
			}
		}

		public bool HasConveyorConnection(Rot4 side)
		{
			return TryFindSideConveyor(side, out _);
		}

		/// <summary>检查 4×4 某一条边的所有外侧格（不只是中央端口）。</summary>
		private bool TryFindSideConveyor(Rot4 side, out IntVec3 cell)
		{
			cell = IntVec3.Invalid;
			if (!Spawned || Map == null)
			{
				return false;
			}

			int width = def.size.x;
			int height = def.size.z;
			for (int i = 0; i < width; i++)
			{
				IntVec3 candidate;
				switch (side.AsInt)
				{
					case 0:
						candidate = Position + new IntVec3(i, 0, height);
						break;
					case 1:
						if (i >= height)
						{
							continue;
						}
						candidate = Position + new IntVec3(width, 0, i);
						break;
					case 2:
						candidate = Position + new IntVec3(i, 0, -1);
						break;
					default:
						if (i >= height)
						{
							continue;
						}
						candidate = Position + new IntVec3(-1, 0, i);
						break;
				}

				if (candidate.InBounds(Map) && candidate.GetEdifice(Map) is Building_ConveyorBase)
				{
					cell = candidate;
					return true;
				}
			}
			return false;
		}

		public bool HasAnyConveyorConnection
		{
			get
			{
				if (!Spawned || Map == null)
				{
					return false;
				}

				int tick = Find.TickManager?.TicksGame ?? -1;
				if (tick == networkCacheTick)
				{
					return networkCache;
				}

				networkCache = ComputeNetworkConnection();
				networkCacheTick = tick;
				return networkCache;
			}
		}

		/// <summary>立体库互联：8 向相邻的仓库组成一张网络，任一仓库有履带即全网络可用。</summary>
		private bool ComputeNetworkConnection()
		{
			var visited = new HashSet<Building_VerticalWarehouse>();
			var stack = new Stack<Building_VerticalWarehouse>();
			stack.Push(this);

			while (stack.Count > 0)
			{
				Building_VerticalWarehouse current = stack.Pop();
				if (current == null || !visited.Add(current))
				{
					continue;
				}

				if (current.HasDirectConveyorConnection())
				{
					return true;
				}

				foreach (Building_VerticalWarehouse neighbor in current.AdjacentWarehouses())
				{
					if (!visited.Contains(neighbor))
					{
						stack.Push(neighbor);
					}
				}
			}

			return false;
		}

		private IEnumerable<Building_VerticalWarehouse> AdjacentWarehouses()
		{
			foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this))
			{
				if (!cell.InBounds(Map))
				{
					continue;
				}
				foreach (Thing thing in Map.thingGrid.ThingsListAt(cell))
				{
					if (thing is Building_VerticalWarehouse warehouse && warehouse != this)
					{
						yield return warehouse;
					}
				}
			}
		}

		private bool HasDirectConveyorConnection()
		{
			return HasConveyorConnection(Rot4.North)
				|| HasConveyorConnection(Rot4.East)
				|| HasConveyorConnection(Rot4.South)
				|| HasConveyorConnection(Rot4.West);
		}

		public bool TryFindConnectedPort(out IntVec3 port)
		{
			var visited = new HashSet<Building_VerticalWarehouse>();
			var stack = new Stack<Building_VerticalWarehouse>();
			stack.Push(this);

			while (stack.Count > 0)
			{
				Building_VerticalWarehouse current = stack.Pop();
				if (current == null || !visited.Add(current))
				{
					continue;
				}

				if (current.TryFindDirectPort(out port))
				{
					return true;
				}

				foreach (Building_VerticalWarehouse neighbor in current.AdjacentWarehouses())
				{
					if (!visited.Contains(neighbor))
					{
						stack.Push(neighbor);
					}
				}
			}

			port = IntVec3.Invalid;
			return false;
		}

		private bool TryFindDirectPort(out IntVec3 port)
		{
			Rot4[] sides = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
			for (int i = 0; i < sides.Length; i++)
			{
				if (TryFindSideConveyor(sides[i], out port))
				{
					return true;
				}
			}
			port = IntVec3.Invalid;
			return false;
		}

		public Building_VerticalWarehouse()
		{
			innerContainer = new ThingOwner<Thing>(this);
			settingsVersion = 3;
			EnsureStoreSettings();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);
			Scribe_Values.Look(ref autoOutputToConveyor, "autoOutputToConveyor", true);
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			Scribe_Deep.Look(ref storeSettings, "storeSettings", this);
			if (innerContainer == null)
			{
				innerContainer = new ThingOwner<Thing>(this);
			}
			EnsureStoreSettings();

			if (Scribe.mode == LoadSaveMode.PostLoadInit && settingsVersion < 3)
			{
				MigrateLegacyDefaults();
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
			settingsVersion = 3;
		}

		private void EnsureStoreSettings()
		{
			if (storeSettings != null)
			{
				return;
			}

			storeSettings = new RimWorld.StorageSettings(this);
			storeSettings.filter.SetAllowAll(null);
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

			SpecialThingFilterDef rotten = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowRotten");
			if (rotten != null)
			{
				storeSettings.filter.SetAllow(rotten, false);
			}
			if (ThingCategoryDefOf.Corpses != null)
			{
				storeSettings.filter.SetAllow(ThingCategoryDefOf.Corpses, false);
			}
			if (ThingCategoryDefOf.Chunks != null)
			{
				storeSettings.filter.SetAllow(ThingCategoryDefOf.Chunks, false);
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
			if (!HasPower || !HasAnyConveyorConnection || !Accepts(thing))
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
					IntVec3 drop = TryFindConnectedPort(out IntVec3 port) ? port : InteractionCell;
					GenPlace.TryPlaceThing(thing, drop, Map, ThingPlaceMode.Near, out _);
				}
				return false;
			}
			Map?.resourceCounter?.UpdateResourceCounts();
			return true;
		}

		public bool TryWithdraw(Thing thing)
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

			IntVec3 drop = TryFindConnectedPort(out IntVec3 port) ? port : InteractionCell;
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

			TryAutoOutputToConveyor();

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

		private void TryAutoOutputToConveyor()
		{
			if (!autoOutputToConveyor || Container.Count == 0 || !HasAnyConveyorConnection)
			{
				return;
			}

			Thing item = Container[0];
			if (item != null)
			{
				TryWithdraw(item);
			}
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
			Room best = null;
			foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this))
			{
				if (!cell.InBounds(Map))
				{
					continue;
				}
				Room room = cell.GetRoom(Map);
				if (room == null || room.UsesOutdoorTemperature)
				{
					continue;
				}
				if (best == null || room.CellCount > best.CellCount)
				{
					best = room;
				}
			}
			return best;
		}

		public int CollectNearby(int radius = 8)
		{
			if (!HasPower || !HasAnyConveyorConnection || Map == null)
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

			if (!HasPower)
			{
				text += "\n" + "CK.VerticalWarehouse.NoPower".Translate();
			}
			if (!HasAnyConveyorConnection)
			{
				text += "\n" + "CK.VerticalWarehouse.NoConveyor".Translate();
			}
			return text;
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}

			yield return new Command_Toggle
			{
				defaultLabel = "CK.VerticalWarehouse.AutoConveyor".Translate(),
				defaultDesc = "CK.VerticalWarehouse.AutoConveyorDesc".Translate(),
				isActive = () => autoOutputToConveyor,
				toggleAction = () =>
				{
					autoOutputToConveyor = !autoOutputToConveyor;
				}
			};

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
