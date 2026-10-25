#nullable disable

﻿using RimWorld;
using UnityEngine;
using Verse;

namespace rjw.MainTab.Checkbox;

[StaticConstructorOnStartup]
public class PawnColumnWorker_Comfort : PawnColumnCheckbox
{
	public new static readonly Texture2D CheckboxOnTex = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_on");
	public new static readonly Texture2D CheckboxOffTex = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_off");
	public new static readonly Texture2D CheckboxDisabledTex = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_Refuse");

	protected override bool HasCheckbox(Pawn pawn)
	{
		return true;
	}

	protected override bool GetDisabled(Pawn pawn)
	{
		return !pawn.CanDesignateComfort() && !pawn.IsDesignatedComfort();
	}

	protected override bool GetValue(Pawn pawn)
	{
		return pawn.IsDesignatedComfort();
	}

	protected override void SetValue(Pawn pawn, bool value)
	{
		pawn.ToggleComfort();
	}

	protected override string GetTip(Pawn pawn)
	{
		if (GetDisabled(pawn))
		{
			if (!RJWSettings.rape_enabled)
				return "RJW_Comfort_SettingDisabledTip".Translate();
			return "RJW_Comfort_DisabledTip".Translate(pawn.Named("PAWN"));
		}
		if (GetValue(pawn))
			return "RJW_Comfort_OnTip".Translate(pawn.Named("PAWN"));
		return "RJW_Comfort_OffTip".Translate(pawn.Named("PAWN"));
	}
}

