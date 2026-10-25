using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 履带基类：不依赖玩家摆放方向，物品会自动寻找最近的立体库并沿相邻履带前进。
	/// 所有 1×1 履带/转向盘共用这套“最近仓库”路由。
	/// </summary>
	[StaticConstructorOnStartup]
	public abstract class Building_ConveyorBase : Building
	{
		protected const int MoveIntervalTicks = 30;

		private static Graphic movingArrow;

		private static Graphic MovingArrow
		{
			get
			{
				if (movingArrow == null)
				{
					movingArrow = GraphicDatabase.Get<Graphic_Single>(
						"Things/Building/Storage/CK_ConveyorArrow",
						ShaderDatabase.Cutout,
						new Vector2(0.45f, 0.45f),
						Color.white);
				}
				return movingArrow;
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);

			if (Find.TickManager == null)
			{
				return;
			}

			bool selected = Find.Selector != null && Find.Selector.IsSelected(this);
			if (!selected && !HasCargo())
			{
				return;
			}

			Rot4 route = RouteDirection();
			float phase = (Find.TickManager.TicksGame % MoveIntervalTicks) / (float)MoveIntervalTicks;
			IntVec3 facing = route.FacingCell;
			Vector3 direction = new Vector3(facing.x, 0f, facing.z);
			float halfLength = def.size.x > 1 ? 0.8f : 0.35f;

			for (int i = 0; i < 2; i++)
			{
				float t = (phase + i * 0.5f) % 1f;
				Vector3 position = drawLoc
					+ direction * Mathf.Lerp(-halfLength, halfLength, t)
					+ new Vector3(0f, 0.03f, 0f);
				MovingArrow.Draw(position, route, this);
			}
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

		protected virtual void MoveItems()
		{
			foreach (IntVec3 cell in GenAdj.OccupiedRect(this))
			{
				foreach (Thing item in Map.thingGrid.ThingsListAt(cell).ToList())
				{
					if (!CanMove(item))
					{
						continue;
					}

					// 先看四邻是否直接贴着重立体库；任意一边都可以存入。
					if (TryStoreAdjacent(item))
					{
						break;
					}

					if (TryFindNextCell(cell, out IntVec3 next) && CanPlaceAt(next))
					{
						item.Position = next;
						break;
					}
				}
			}
		}

		private bool TryStoreAdjacent(Thing item)
		{
			// 先按占用区找相邻仓库，再用格子 edifice 兜底，避免 lister 注册时序问题。
			foreach (Building_VerticalWarehouse warehouse in Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_VerticalWarehouse>())
			{
				CellRect rect = GenAdj.OccupiedRect(warehouse);
				if (rect.ExpandedBy(1).Contains(Position)
					&& warehouse.TryStoreInNetwork(item))
				{
					return true;
				}
			}

			foreach (Rot4 side in AllDirections())
			{
				IntVec3 cell = Position + side.FacingCell;
				if (!cell.InBounds(Map))
				{
					continue;
				}

				if (cell.GetEdifice(Map) is Building_VerticalWarehouse warehouse
					&& warehouse.TryStoreInNetwork(item))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>在相邻履带网络里做 BFS，找到最近的、可贴靠的立体库，返回第一步。</summary>
		private bool TryFindNextCell(IntVec3 from, out IntVec3 next)
		{
			next = from;
			var queue = new Queue<IntVec3>();
			var visited = new HashSet<IntVec3> { from };
			var firstStep = new Dictionary<IntVec3, IntVec3>();
			queue.Enqueue(from);

			int guard = 0;
			while (queue.Count > 0 && guard++ < 1024)
			{
				IntVec3 current = queue.Dequeue();
				foreach (Rot4 side in AllDirections())
				{
					IntVec3 candidate = current + side.FacingCell;
					if (!candidate.InBounds(Map)
						|| visited.Contains(candidate)
						|| !(candidate.GetEdifice(Map) is Building_ConveyorBase))
					{
						continue;
					}

					visited.Add(candidate);
					IntVec3 step = current == from ? candidate : firstStep[current];
					firstStep[candidate] = step;

					if (HasAdjacentWarehouse(candidate))
					{
						next = step;
						return true;
					}
					queue.Enqueue(candidate);
				}
			}
			return false;
		}

		private bool HasAdjacentWarehouse(IntVec3 cell)
		{
			foreach (Building_VerticalWarehouse warehouse in Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_VerticalWarehouse>())
			{
				if (GenAdj.OccupiedRect(warehouse).ExpandedBy(1).Contains(cell))
				{
					return true;
				}
			}
			return false;
		}

		private Rot4 RouteDirection()
		{
			if (TryFindNextCell(Position, out IntVec3 next))
			{
				IntVec3 delta = next - Position;
				if (delta.x > 0) return Rot4.East;
				if (delta.x < 0) return Rot4.West;
				if (delta.z > 0) return Rot4.North;
				return Rot4.South;
			}
			return Rotation;
		}

		private bool HasCargo()
		{
			foreach (IntVec3 cell in GenAdj.OccupiedRect(this))
			{
				foreach (Thing thing in Map.thingGrid.ThingsListAt(cell))
				{
					if (CanMove(thing))
					{
						return true;
					}
				}
			}
			return false;
		}

		protected static bool CanMove(Thing thing)
		{
			return thing != null
				&& thing.Spawned
				&& thing.def != null
				&& thing.def.category == ThingCategory.Item
				&& !thing.def.IsCorpse
				&& !(thing is MinifiedThing)
				&& thing.stackCount > 0;
		}

		protected bool CanPlaceAt(IntVec3 cell)
		{
			if (!cell.InBounds(Map))
			{
				return false;
			}

			Building edifice = cell.GetEdifice(Map);
			if (edifice != null && edifice != this && !(edifice is Building_ConveyorBase))
			{
				return false;
			}

			int max = cell.GetMaxItemsAllowedInCell(Map);
			return max > 0 && cell.GetItemCount(Map) < max;
		}

		private static IEnumerable<Rot4> AllDirections()
		{
			yield return Rot4.North;
			yield return Rot4.East;
			yield return Rot4.South;
			yield return Rot4.West;
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}

			if (TryFindNextCell(Position, out IntVec3 next))
			{
				text += "CK.ConveyorHub.Target".Translate(next.x, next.z);
			}
			else
			{
				text += "CK.ConveyorHub.NoTarget".Translate();
			}
			return text;
		}
	}
}
