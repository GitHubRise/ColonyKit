#nullable disable

﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

using Sex = rjw.GenderHelper.Sex;

namespace rjw
{
	// 不像移除部位那样直接，因为该配方有可能在小人生成时被应用
	public class Recipe_InstallPart : Recipe_InstallArtificialBodyPart
	{
		protected virtual bool CanApplyOnPart(Pawn pawn, BodyPartRecord record)
		{
			if (record.parent != null && !pawn.health.hediffSet.GetNotMissingParts().Contains(record.parent))
			{
				return false;
			}
			return true;
		}

		public virtual bool ValidFor(Pawn pawn) => !xxx.is_slime(pawn);

		public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
		{
			return base.AvailableOnNow(thing, part) && ValidFor((Pawn) thing);
		}

		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
			return MedicalRecipesUtility.GetFixedPartsToApplyOn(recipe, pawn, (BodyPartRecord record) => CanApplyOnPart(pawn, record));
		}

		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			Sex before = GenderHelper.GetSex(pawn);

			if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
			{
				return;
			}
			OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);

			Hediff addedPartHediff = SexPartAdder.recipePartAdder(recipe, pawn, part, ingredients);
			pawn.health.AddHediff(addedPartHediff, part);

			if (billDoer != null)
			{
				if (IsViolationOnPawn(pawn, part, billDoer.Faction))
				{
					ReportViolation(pawn, billDoer, pawn.Faction, -80);
				}

				if (ModsConfig.IdeologyActive && !addedPartHediff.def.IsArtificialSexPart())
				{
					Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InstalledProsthetic, billDoer.Named(HistoryEventArgsNames.Doer)));
				}
			}

			if (!PawnGenerator.IsBeingGenerated(pawn))
			{
				// TODO：将性别变更纳入违规判定逻辑
				Sex after = GenderHelper.GetSex(pawn);
				GenderHelper.ChangeSex(pawn, before, after);
			}
		}

		protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			if (billDoer != null)
			{
				TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
				SurgeryHelper.RemoveAndSpawnSexParts(billDoer, pawn, part, isReplacement: true);
				MedicalRecipesUtility.SpawnNaturalPartIfClean(pawn, part, billDoer.Position, billDoer.Map);
				MedicalRecipesUtility.SpawnThingsFromHediffs(pawn, part, billDoer.Position, billDoer.Map);
			}
			pawn.health.RestorePart(part);
		}
	}

	public class Recipe_InstallGenitals : Recipe_InstallPart
	{
		public override bool ValidFor(Pawn p)
		{
			return base.ValidFor(p) && !Genital_Helper.genitals_blocked(p);
		}
	}

	public class Recipe_InstallBreasts : Recipe_InstallPart
	{
		public override bool ValidFor(Pawn p)
		{
			return base.ValidFor(p) && !Genital_Helper.breasts_blocked(p);
		}
	}

	public class Recipe_InstallAnus : Recipe_InstallPart
	{
		public override bool ValidFor(Pawn p)
		{
			return base.ValidFor(p) && !Genital_Helper.anus_blocked(p);
		}
	}
}
