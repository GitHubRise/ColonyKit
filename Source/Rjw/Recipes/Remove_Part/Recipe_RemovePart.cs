#nullable disable

﻿using System.Collections.Generic;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace rjw
{
	public class Recipe_RemovePart : Recipe_RemoveBodyPart
	{
		private const int HarvestGoodwillPenalty = -80;

		protected virtual bool ValidFor(Pawn p)
		{
			return !xxx.is_slime(p);//|| xxx.is_demon(p)
		}

		public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
		{
			return base.AvailableOnNow(thing, part) && ValidFor((Pawn)thing);
		}

		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
			return MedicalRecipesUtility.GetFixedPartsToApplyOn(recipe, pawn);
		}

		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			bool isHarvest = SurgeryHelper.IsHarvest(pawn, part);
			bool isViolation = isHarvest && IsViolationOnPawn(pawn, part, billDoer.Faction);

			if (billDoer != null)
			{
				if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
				{
					return;
				}
				OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
			}
			DamagePart(pawn, part);
			pawn.Drawer.renderer.SetAllGraphicsDirty();

			if (isHarvest)
			{
				ApplyThoughts(pawn, billDoer);
			}
			if (isViolation)
			{
				ReportViolation(pawn, billDoer, pawn.HomeFaction, HarvestGoodwillPenalty);
			}
		}

		protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			base.OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
			TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
			SurgeryHelper.RemoveAndSpawnSexParts(billDoer, pawn, part, isReplacement: false);
			MedicalRecipesUtility.SpawnNaturalPartIfClean(pawn, part, billDoer.Position, billDoer.Map);
			MedicalRecipesUtility.SpawnThingsFromHediffs(pawn, part, billDoer.Position, billDoer.Map);
		}

		public override string GetLabelWhenUsedOn(Pawn p, BodyPartRecord part)
		{
			return recipe.label.CapitalizeFirst();
		}

		[SyncMethod]
		public override void DamagePart(Pawn pawn, BodyPartRecord part)
		{
			if (part.IsCorePart)
			{
				float damageAmount = part.def.hitPoints * Rand.Range(.5f, 1.5f) / 20f;
				pawn.TakeDamage(new DamageInfo(DamageDefOf.SurgicalCut, damageAmount, 999f, -1f, null, part));
			}
			else
			{
				base.DamagePart(pawn, part);
			}
		}
	}
}
