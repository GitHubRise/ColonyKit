using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>立体库内容面板：容量、检索、滚动列表、逐堆取出。</summary>
	public class ITab_VerticalWarehouse : ITab
	{
		private static readonly Vector2 WinSize = new Vector2(360f, 460f);

		private string search = "";
		private Vector2 scroll;

		public ITab_VerticalWarehouse()
		{
			size = WinSize;
			labelKey = "CK.VerticalWarehouse.Tab";
		}

		public override bool IsVisible => SelThing is Building_VerticalWarehouse;

		protected override void FillTab()
		{
			Building_VerticalWarehouse building = SelThing as Building_VerticalWarehouse;
			if (building == null)
			{
				return;
			}

			Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
			Widgets.BeginGroup(rect);

			float y = 0f;
			Widgets.Label(new Rect(0f, y, rect.width, 24f), string.Format(
				"CK.VerticalWarehouse.Inspect".Translate(),
				building.UsedStacks,
				Building_VerticalWarehouse.MaxStacks,
				building.UsedItems));
			y += 28f;

			if (!building.HasPower)
			{
				Widgets.Label(new Rect(0f, y, rect.width, 24f), "CK.VerticalWarehouse.NoPower".Translate());
				y += 26f;
			}

			Widgets.Label(new Rect(0f, y, rect.width, 20f), "CK.VerticalWarehouse.Search".Translate());
			y += 22f;
			search = Widgets.TextField(new Rect(0f, y, rect.width, 28f), search, 40);
			y += 34f;

			List<Thing> items = building.Container
				.Where(thing => search.NullOrEmpty()
					|| thing.LabelCap.ToString().IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0
					|| thing.def.label.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
				.ToList();

			Rect outRect = new Rect(0f, y, rect.width, rect.height - y);
			float viewHeight = Mathf.Max(outRect.height, items.Count * 54f + 8f);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, viewHeight);

			Widgets.BeginScrollView(outRect, ref scroll, viewRect);
			if (items.Count == 0)
			{
				Widgets.Label(new Rect(0f, 0f, viewRect.width, 24f),
					building.Container.Count == 0
						? "CK.VerticalWarehouse.Empty".Translate()
						: "CK.VerticalWarehouse.NoMatch".Translate());
			}
			else
			{
				float rowY = 0f;
				foreach (Thing thing in items)
				{
					Rect row = new Rect(0f, rowY, viewRect.width, 50f);
					Widgets.Label(row.LeftPart(0.66f), $"{thing.LabelCap} x{thing.stackCount}");
					if (Widgets.ButtonText(row.RightPart(0.30f), "CK.VerticalWarehouse.Withdraw".Translate()))
					{
						building.TryWithdraw(thing);
						break;
					}
					rowY += 54f;
				}
			}
			Widgets.EndScrollView();
			Widgets.EndGroup();
		}
	}
}
