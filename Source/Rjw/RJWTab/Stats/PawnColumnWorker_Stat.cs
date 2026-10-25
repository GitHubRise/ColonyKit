using rjw;
using rjw.MainTab.DefModExtensions;
using rjw.RJWUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace rjw.MainTab.Stats;

public class PawnColumnWorker_Stat : PawnColumnWorker_Text
{
	private const float HueRed = 0f;
	private const float HueGreen = 1f / 3f;
	private const float Sat = 0.85f;
	private const float Val = 0.6f;
	private const float BarVerticalInset = 2f;

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		var ext = def.GetModExtension<RJW_StatColumnExtension>();
		if (ext?.stat == null)
		{
			using (new UiScope(font: GameFont.Small, anchor: TextAnchor.MiddleCenter, wordWrap: false))
			{
				Widgets.Label(rect, "-");
			}
			return;
		}

		float value = pawn.GetStatValue(ext.stat);
		float barMax = ext.barMax > 0f ? ext.barMax : 1f;
		float fillPercent = Mathf.Clamp01(value / barMax);
		Color barColor = barMax > 1f
			? OvershootColor(value, barMax)
			: ClampedColor(value, barMax);

		Rect fillRect = new Rect(
			rect.x,
			rect.y + BarVerticalInset,
			rect.width * fillPercent,
			rect.height - (BarVerticalInset * 2f));
		using (new UiScope(color: barColor))
		{
			GUI.DrawTexture(fillRect, BaseContent.WhiteTex);
		}

		Rect textRect = new Rect(rect.x, rect.y, rect.width, Mathf.Min(rect.height, 30f));
		using (new UiScope(font: GameFont.Small, anchor: TextAnchor.MiddleCenter, wordWrap: false))
		{
			Widgets.Label(textRect, value.ToStringPercent());
		}

		string tip = ext.stat.description;
		if (!tip.NullOrEmpty() && Mouse.IsOver(textRect))
		{
			TooltipHandler.TipRegion(textRect, tip);
		}
	}

	protected override string GetTextFor(Pawn pawn)
	{
		var ext = def.GetModExtension<RJW_StatColumnExtension>();
		return ext?.stat == null ? string.Empty : pawn.GetStatValue(ext.stat).ToStringPercent();
	}

	protected override string GetTip(Pawn pawn)
		=> def.GetModExtension<RJW_StatColumnExtension>()?.stat?.description!;

	public override int Compare(Pawn a, Pawn b)
	{
		var ext = def.GetModExtension<RJW_StatColumnExtension>();
		if (ext?.stat == null) return 0;
		return a.GetStatValue(ext.stat).CompareTo(b.GetStatValue(ext.stat));
	}

	private static Color ClampedColor(float value, float barMax)
	{
		float fillPercent = Mathf.Clamp01(value / barMax);
		return Color.HSVToRGB(fillPercent * (1f / 3f), Sat, Val);
	}

	private static Color OvershootColor(float value, float barMax)
	{
		float overshoot = Mathf.Clamp01((value - barMax) / barMax);
		float hue = Mathf.Lerp(1f / 3f, 2f / 3f, overshoot);
		float val = Mathf.Lerp(Val, 0.45f, overshoot);
		return Color.HSVToRGB(hue, Sat, val);
	}
}
