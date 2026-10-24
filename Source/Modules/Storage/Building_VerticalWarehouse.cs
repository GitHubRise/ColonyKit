using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 3×3 立体库原型：内部 <see cref="ThingOwner{T}"/> 保管物品，面板/Gizmo 直接存取。
	/// 当前不接搬运 Job、不接原版账单；先验证内部库存、容量、存读档和电力。
	/// </summary>
	public class Building_VerticalWarehouse : Building, IThingHolder
	{
		public const int MaxStacks = 24;

		private ThingOwner<Thing> innerContainer;

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

		public bool HasPower => GetComp<CompPowerTrader>()?.PowerOn ?? false;

		public bool IsFull => UsedStacks >= MaxStacks;

		public Building_VerticalWarehouse()
		{
			innerContainer = new ThingOwner<Thing>(this);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			if (innerContainer == null)
			{
				innerContainer = new ThingOwner<Thing>(this);
			}
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
			return true;
		}

		public bool CanStore(Thing thing)
		{
			if (!HasPower || !Accepts(thing))
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

			if (!GenPlace.TryPlaceThing(taken, InteractionCell, Map, ThingPlaceMode.Near, out _))
			{
				Container.TryAdd(taken, false);
				return false;
			}
			return true;
		}

		public void WithdrawAll()
		{
			foreach (Thing thing in Container.ToList())
			{
				TryWithdraw(thing);
			}
		}

		public int CollectNearby(int radius = 8)
		{
			if (!HasPower || Map == null)
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
