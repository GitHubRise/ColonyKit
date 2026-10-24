using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>立体库内容面板：容量、件数、逐堆取出。</summary>
	public class ITab_VerticalWarehouse : ITab
	{
		private static readonly Vector2 WinSize = new Vector2(320f, 420f);

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

			Listing_Standard listing = new Listing_Standard();
			listing.Begin(new Rect(0f, 0f, rect.width, rect.height));

			listing.Label(string.Format(
				"CK.VerticalWarehouse.Inspect".Translate(),
				building.UsedStacks,
				Building_VerticalWarehouse.MaxStacks,
				building.UsedItems));
			listing.GapLine();

			if (!building.HasPower)
			{
				listing.Label("CK.VerticalWarehouse.NoPower".Translate());
			}

			if (building.Container.Count == 0)
			{
				listing.Label("CK.VerticalWarehouse.Empty".Translate());
			}
			else
			{
				foreach (Thing thing in building.Container)
				{
					listing.Label($"{thing.LabelCap} x{thing.stackCount}");
					if (listing.ButtonText("CK.VerticalWarehouse.Withdraw".Translate()))
					{
						building.TryWithdraw(thing);
						break;
					}
				}
			}

			listing.End();
			Widgets.EndGroup();
		}
	}
}
