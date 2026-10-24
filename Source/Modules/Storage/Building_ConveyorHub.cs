using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 2×2 双向转向履带：四边都能进，物品按建筑朝向指定的出口移动。
	/// 每 30 tick 把每格上的物品向出口推一格；到边缘后推到建筑外侧。
	/// 反向 Gizmo 切换出口到对侧，保持“双向”语义。
	/// </summary>
	public class Building_ConveyorHub : Building
	{
		private const int MoveIntervalTicks = 30;

		private bool reversed;

		public Rot4 OutputDirection => reversed ? Rotation.Opposite : Rotation;

		public IntVec3 OutputCell
		{
			get
			{
				switch (OutputDirection.AsInt)
				{
					case 0:
						return Position + new IntVec3(1, 0, 2); // North
					case 1:
						return Position + new IntVec3(2, 0, 1); // East
					case 2:
						return Position + new IntVec3(1, 0, -1); // South
					default:
						return Position + new IntVec3(-1, 0, 1); // West
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref reversed, "reversed", false);
		}

		protected override void Tick()
		{
			base.Tick();
			if (!Spawned || Map == null || !this.IsHashIntervalTick(MoveIntervalTicks))
			{
				return;
			}

			MoveItems();
		}

		private void MoveItems()
		{
			foreach (IntVec3 cell in GenAdj.OccupiedRect(this))
			{
				List<Thing> things = Map.thingGrid.ThingsListAt(cell).ToList();
				foreach (Thing item in things)
				{
					if (!CanMove(item))
					{
						continue;
					}

					if (!TryNextCell(cell, out IntVec3 next, out bool leavesHub))
					{
						continue;
					}

					if (leavesHub && next != OutputCell)
					{
						continue;
					}

					if (!CanPlaceAt(next))
					{
						continue;
					}

					item.Position = next;
					break;
				}
			}
		}

		private static bool CanMove(Thing thing)
		{
			return thing != null
				&& thing.Spawned
				&& thing.def != null
				&& thing.def.category == ThingCategory.Item
				&& !thing.def.IsCorpse
				&& !(thing is MinifiedThing)
				&& thing.stackCount > 0;
		}

		private bool CanPlaceAt(IntVec3 cell)
		{
			if (!cell.InBounds(Map))
			{
				return false;
			}

			Building edifice = cell.GetEdifice(Map);
			// 履带自身的格子也算可走；只在出口/内部移动，不覆盖其它建筑。
			return edifice == null || edifice == this;
		}

		private bool TryNextCell(IntVec3 from, out IntVec3 next, out bool leavesHub)
		{
			leavesHub = false;
			next = from;

			int laneX = Position.x + 1;
			int laneZ = Position.z + 1;
			int dir = OutputDirection.AsInt;

			if (dir == 0 || dir == 2) // North / South
			{
				if (from.x != laneX)
				{
					next = new IntVec3(from.x + (laneX > from.x ? 1 : -1), 0, from.z);
				}
				else
				{
					next = new IntVec3(from.x, 0, from.z + (dir == 0 ? 1 : -1));
				}
			}
			else // East / West
			{
				if (from.z != laneZ)
				{
					next = new IntVec3(from.x, 0, from.z + (laneZ > from.z ? 1 : -1));
				}
				else
				{
					next = new IntVec3(from.x + (dir == 1 ? 1 : -1), 0, from.z);
				}
			}

			leavesHub = !GenAdj.OccupiedRect(this).Contains(next);
			return next != from;
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}

			string suffix = reversed
				? "CK.ConveyorHub.ReversedSuffix".Translate()
				: "";
			text += "CK.ConveyorHub.Status".Translate(OutputDirection.ToString(), suffix);
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
				defaultLabel = "CK.ConveyorHub.Reverse".Translate(),
				defaultDesc = "CK.ConveyorHub.ReverseDesc".Translate(),
				action = () =>
				{
					reversed = !reversed;
					Messages.Message("CK.ConveyorHub.Reversed".Translate(), MessageTypeDefOf.NeutralEvent, false);
				}
			};
		}
	}
}
