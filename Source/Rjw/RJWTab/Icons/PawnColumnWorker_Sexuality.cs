#nullable disable

using RimWorld;
using UnityEngine;
using Verse;

namespace rjw.MainTab.Icon;

[StaticConstructorOnStartup]
public class PawnColumnWorker_RJWSexuality : PawnColumnWorker_Icon
{
	public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("RJW-LOGO", true);

	protected override Texture2D GetIconFor(Pawn pawn) => Icon;
	protected override string GetIconTip(Pawn pawn) => "SexcardTooltip".Translate();
	protected override void ClickedIcon(Pawn pawn) => Find.WindowStack.Add(new Dialog_SexcardNG(pawn));
}
