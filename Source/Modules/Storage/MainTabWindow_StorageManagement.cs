using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>底部「立体库」主标签页：列出本图全部立体库，可检索、选中/跳转。</summary>
	public class MainTabWindow_StorageManagement : MainTabWindow
	{
		private const float RowHeight = 62f;

		private string search = "";
		private Vector2 scroll;

		public MainTabWindow_StorageManagement()
		{
		}

		public override Vector2 RequestedTabSize => new Vector2(760f, 620f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "CK.StorageManagement.Title".Translate());
			Widgets.Label(new Rect(0f, 30f, inRect.width, 24f), "CK.StorageManagement.Search".Translate());
			search = Widgets.TextField(new Rect(0f, 56f, inRect.width, 30f), search, 60);

			Map map = Find.CurrentMap;
			List<Building_VerticalWarehouse> warehouses = map == null
				? new List<Building_VerticalWarehouse>()
				: map.listerBuildings.AllBuildingsColonistOfClass<Building_VerticalWarehouse>()
					.Where(Matches)
					.OrderBy(w => w.Position.x)
					.ThenBy(w => w.Position.z)
					.ToList();

			Rect outRect = new Rect(0f, 96f, inRect.width, inRect.height - 96f);
			float viewHeight = Mathf.Max(outRect.height, warehouses.Count * RowHeight + 8f);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, viewHeight);

			Widgets.BeginScrollView(outRect, ref scroll, viewRect);
			if (warehouses.Count == 0)
			{
				Widgets.Label(new Rect(0f, 0f, viewRect.width, 24f), "CK.StorageManagement.None".Translate());
			}
			else
			{
				float y = 0f;
				foreach (Building_VerticalWarehouse warehouse in warehouses)
				{
					DrawRow(new Rect(0f, y, viewRect.width, RowHeight - 6f), warehouse);
					y += RowHeight;
				}
			}
			Widgets.EndScrollView();
		}

		private bool Matches(Building_VerticalWarehouse warehouse)
		{
			if (search.NullOrEmpty())
			{
				return true;
			}

			if (warehouse.LabelCap.ToString().IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}

			foreach (Thing thing in warehouse.Container)
			{
				if (thing.LabelCap.ToString().IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		private void DrawRow(Rect row, Building_VerticalWarehouse warehouse)
		{
			Widgets.DrawHighlightIfMouseover(row);
			Rect text = row.LeftPart(0.78f);
			string status = warehouse.HasAnyConveyorConnection
				? "CK.StorageManagement.Connected".Translate()
				: "CK.StorageManagement.Disconnected".Translate();
			Widgets.Label(text, "CK.StorageManagement.Row".Translate(
				warehouse.LabelCap,
				warehouse.Position.x,
				warehouse.Position.z,
				warehouse.UsedStacks,
				Building_VerticalWarehouse.MaxStacks,
				warehouse.UsedItems,
				status));

			if (Widgets.ButtonText(row.RightPart(0.20f), "CK.StorageManagement.Select".Translate()))
			{
				Find.Selector.ClearSelection();
				Find.Selector.Select(warehouse, true);
				if (Find.CameraDriver != null)
				{
					Find.CameraDriver.JumpToCurrentMapLoc(warehouse.Position);
				}
			}
		}
	}
}
