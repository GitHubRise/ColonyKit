using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 履带基类：处理方向、反向、30 tick 移动、相邻履带互通。
	/// 1×1 双向履带与 1×1 转向盘只实现自己的 <see cref="OutputCell"/> 和 <see cref="TryNextCell"/>。
	/// </summary>
	[StaticConstructorOnStartup]
	public abstract class Building_ConveyorBase : Building
	{
		protected const int MoveIntervalTicks = 30;

		protected bool reversed;

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

		public Rot4 OutputDirection => reversed ? Rotation.Opposite : Rotation;

		public abstract IntVec3 OutputCell { get; }

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);

			if (Find.TickManager == null)
			{
				return;
			}

			// 空闲时不显示方向；选中/有货物时才显示，便于查看当前出口。
			bool selected = Find.Selector != null && Find.Selector.IsSelected(this);
			if (!selected && !HasCargo())
			{
				return;
			}

			// 跑马灯：30 tick 走完一个循环，两个箭头错开半个循环。
			float phase = (Find.TickManager.TicksGame % MoveIntervalTicks) / (float)MoveIntervalTicks;
			IntVec3 facing = OutputDirection.FacingCell;
			Vector3 direction = new Vector3(facing.x, 0f, facing.z);
			float halfLength = def.size.x > 1 ? 0.8f : 0.35f;

			for (int i = 0; i < 2; i++)
			{
				float t = (phase + i * 0.5f) % 1f;
				Vector3 position = drawLoc
					+ direction * Mathf.Lerp(-halfLength, halfLength, t)
					+ new Vector3(0f, 0.03f, 0f);
				MovingArrow.Draw(position, OutputDirection, this);
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

					if (!TryNextCell(cell, out IntVec3 next, out bool leavesConveyor))
					{
						continue;
					}

					if (leavesConveyor && next != OutputCell)
					{
						continue;
					}

					// 履带出口对准立体库端口：直接送入内部库存，而不是把物品放在建筑格上。
					if (next.GetEdifice(Map) is Building_VerticalWarehouse warehouse
						&& warehouse.HasAnyConveyorConnection)
					{
						if (warehouse.TryStoreInNetwork(item))
						{
							break;
						}
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

		protected abstract bool TryNextCell(IntVec3 from, out IntVec3 next, out bool leavesConveyor);

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

			// 每格只放一堆；相邻履带已有一堆时等下一轮，避免重叠。
			int max = cell.GetMaxItemsAllowedInCell(Map);
			return max > 0 && cell.GetItemCount(Map) < max;
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
