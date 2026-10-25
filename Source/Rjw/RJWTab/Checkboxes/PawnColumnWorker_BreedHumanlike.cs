#nullable disable

using RimWorld;
using UnityEngine;
using Verse;

namespace rjw.MainTab.Checkbox;

[StaticConstructorOnStartup]
public class PawnColumnWorker_BreedHumanlike : PawnColumnCheckbox
{
	public new static readonly Texture2D CheckboxOnTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_on");
	public new static readonly Texture2D CheckboxOffTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_off");
	public new static readonly Texture2D CheckboxDisabledTex = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_Refuse");

	protected override bool HasCheckbox(Pawn pawn)
	{
		return true;
	}

	protected override bool GetDisabled(Pawn pawn)
	{
		return !pawn.CanDesignateBreeding() && !pawn.IsDesignatedBreeding();
	}

	protected override bool GetValue(Pawn pawn)
	{
		return pawn.IsDesignatedBreeding() && xxx.is_human(pawn);
	}

	protected override void SetValue(Pawn pawn, bool value)
	{
		if (value == this.GetValue(pawn)) return;
		pawn.ToggleBreeding();
	}

	protected override string GetTip(Pawn pawn)
	{
		if (GetDisabled(pawn))
		{
			if (!RJWSettings.bestiality_enabled)
				return "RJW_BreedHumanlike_SettingDisabledTip".Translate();
			return "RJW_BreedHumanlike_DisabledTip".Translate(pawn.Named("PAWN"));
		}
		if (GetValue(pawn))
			return "RJW_BreedHumanlike_OnTip".Translate(pawn.Named("PAWN"));
		return "RJW_BreedHumanlike_OffTip".Translate(pawn.Named("PAWN"));
	}
}
