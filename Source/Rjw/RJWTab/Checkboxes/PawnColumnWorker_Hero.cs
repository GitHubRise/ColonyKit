#nullable disable

using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace rjw.MainTab.Checkbox;

[StaticConstructorOnStartup]
public class PawnColumnWorker_Hero : PawnColumnCheckbox
{
	public new static readonly Texture2D CheckboxOnTex = ContentFinder<Texture2D>.Get("UI/Commands/Hero_on");
	public new static readonly Texture2D CheckboxOffTex = ContentFinder<Texture2D>.Get("UI/Commands/Hero_off");
	public new static readonly Texture2D CheckboxDisabledTex = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_Refuse");

	protected override bool HasCheckbox(Pawn pawn)
	{
		return true;
	}

	protected override bool GetDisabled(Pawn pawn)
	{
		return !pawn.CanDesignateHero() && !pawn.IsDesignatedHero();
	}

	protected override bool GetValue(Pawn pawn)
	{
		return pawn.IsDesignatedHero();
	}

	protected override void SetValue(Pawn pawn, bool value)
	{
		if (pawn.IsDesignatedHero()) return;
		if (pawn.CanDesignateHero() && value)
		{
			Find.WindowStack.Add(new Dialog_DesignateHero(pawn));
		}
	}

	protected override string GetTip(Pawn pawn)
	{
		if (GetDisabled(pawn))
		{
			if (!RJWSettings.RPG_hero_control)
				return "RJW_Hero_DisabledTip".Translate();
			if (DesignatorsData.rjwHero.Any(h => h.IsHeroOwner()))
				return "RJW_Hero_AlreadyExistsTip".Translate();
			return null;
		}
		if (!pawn.IsDesignatedHero())
			return "RJW_Hero_DesignateTip".Translate(pawn.Named("PAWN"));
		if (RJWSettings.RPG_hero_control_Ironman)
			return "RJW_Hero_Ironman_AlreadyHeroTip".Translate(pawn.Named("PAWN"));
		return "RJW_Hero_AlreadyHeroTip".Translate(pawn.Named("PAWN"));
	}
}
