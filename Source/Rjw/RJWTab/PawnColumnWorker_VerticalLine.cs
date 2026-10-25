using rjw.RJWUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace rjw.MainTab;

public class PawnColumnWorker_VerticalLine : PawnColumnWorker
{
	protected virtual int Width => def.gap;

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		float x = rect.x + rect.width / 2f;
		using (new UiScope(color: new Color(1f, 1f, 1f, 0.2f))) {
			Widgets.DrawLineVertical(x, rect.y, rect.height);
		}
	}

	public override int GetMinWidth(PawnTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), Width);
	}

	public override int GetMaxWidth(PawnTable table)
	{
		return Mathf.Min(base.GetMaxWidth(table), Width);
	}

	public override int GetMinCellHeight(Pawn pawn)
	{
		return 0;
	}
}
