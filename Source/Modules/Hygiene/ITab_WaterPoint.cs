using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>取水点的检视页：设置取水次数、查看水箱/水泵/水井状态。</summary>
	public class ITab_WaterPoint : ITab
	{
		private static readonly Vector2 WinSize = new Vector2(390f, 270f);

		public ITab_WaterPoint()
		{
			size = WinSize;
			labelKey = "CK.Hygiene.WaterPoint.Tab";
		}

		protected Building_WaterPoint Point => SelThing as Building_WaterPoint;

		protected override void FillTab()
		{
			Building_WaterPoint point = Point;
			if (point == null)
			{
				return;
			}
			Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
			Widgets.BeginGroup(rect);
			Text.Font = GameFont.Small;
			float y = 0f;

			Widgets.Label(new Rect(0f, y, rect.width, 24f), "CK.Hygiene.WaterPoint.TabTitle".Translate());
			y += 28f;
			Widgets.Label(
				new Rect(0f, y, rect.width, 24f),
				"CK.Hygiene.WaterPoint.DrawProgress".Translate(point.deliveredDraws, point.targetDraws));
			y += 30f;

			if (Widgets.ButtonText(new Rect(0f, y, 56f, 28f), "-1"))
			{
				point.SetDrawOrder(point.targetDraws - 1);
			}
			if (Widgets.ButtonText(new Rect(62f, y, 56f, 28f), "+1"))
			{
				point.SetDrawOrder(point.targetDraws + 1);
			}
			if (Widgets.ButtonText(new Rect(124f, y, 56f, 28f), "+5"))
			{
				point.SetDrawOrder(point.targetDraws + 5);
			}
			if (Widgets.ButtonText(new Rect(186f, y, 110f, 28f), "CK.Hygiene.WaterPoint.Clear".Translate()))
			{
				point.CancelDrawOrder();
			}
			y += 38f;

			Widgets.Label(
				new Rect(0f, y, rect.width, 24f),
				"CK.Hygiene.WaterPoint.TankLine".Translate(
					point.StoredWater.ToString("F0"),
					point.Capacity.ToString("F0")));
			y += 26f;
			Widgets.Label(
				new Rect(0f, y, rect.width, 24f),
				point.Pumped
					? "CK.Hygiene.WaterPoint.FlowFast".Translate()
					: "CK.Hygiene.WaterPoint.FlowSlow".Translate());
			y += 26f;
			Widgets.Label(
				new Rect(0f, y, rect.width, 24f),
				point.FindWellForDraw() != null
					? "CK.Hygiene.WaterPoint.WellReady".Translate()
					: "CK.Hygiene.WaterPoint.NoWell".Translate());
			y += 30f;

			GUI.color = ColoredText.SubtleGrayColor;
			Widgets.Label(new Rect(0f, y, rect.width, 60f), "CK.Hygiene.WaterPoint.TabHint".Translate());
			GUI.color = Color.white;
			Widgets.EndGroup();
		}
	}
}
