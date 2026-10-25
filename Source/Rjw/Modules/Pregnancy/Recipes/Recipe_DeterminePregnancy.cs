#nullable disable

﻿using RimWorld;
using System;
using System.Linq;
using Verse;
using System.Collections.Generic;

namespace rjw
{
	public class Recipe_DeterminePregnancy : RecipeWorker
	{
		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
			/* Males can be impregnated by mechanoids, probably
			if (!xxx.is_female(pawn))
			{
				yield break;
			}
			*/
			BodyPartRecord part = pawn.RaceProps.body.corePart;
			if (recipe.appliedOnFixedBodyParts[0] != null)
				part = pawn.RaceProps.body.AllParts.Find(x => x.def == recipe.appliedOnFixedBodyParts[0]);
			if (part != null && (pawn.ageTracker.CurLifeStage.reproductive)
				|| pawn.IsPregnant(true))
			{
				yield return part;
			}
		}

		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			var p = PregnancyHelper.GetPregnancies(pawn);
			if (p.NullOrEmpty())
			{
				var text = "RJW_Message_NotPregnant".Translate(billDoer, pawn).CapitalizeFirst();
				Messages.Message(text, MessageTypeDefOf.NeutralEvent);
				return;
			}

			foreach (var x in p)
			{
				if (x is Hediff_BasePregnancy)
				{
					var preg = x as Hediff_BasePregnancy;
					preg.CheckPregnancy();
				}
			}
		}
	}
	public class Recipe_DeterminePaternity : RecipeWorker
	{
		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
			BodyPartRecord part = pawn.RaceProps.body.corePart;
			if (recipe.appliedOnFixedBodyParts[0] != null)
				part = pawn.RaceProps.body.AllParts.Find(x => x.def == recipe.appliedOnFixedBodyParts[0]);
			if (part != null && pawn.IsPregnant(true))
			{
				yield return part;
			}
		}

		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			var p = PregnancyHelper.GetPregnancies(pawn);
			if (p.NullOrEmpty())
			{
				var text = "RJW_Message_NotPregnant".Translate(billDoer, pawn).CapitalizeFirst();
				Messages.Message(text, MessageTypeDefOf.NeutralEvent);
				return;
			}

			foreach (var x in p)
			{
				if (x is Hediff_BasePregnancy)
				{
					var preg = x as Hediff_BasePregnancy;
					var text = "RJW_Message_Pregnant".Translate(billDoer, pawn, preg.father).CapitalizeFirst();
					Messages.Message(text, MessageTypeDefOf.NeutralEvent);

					preg.CheckPregnancy();
					preg.is_parent_known = true;
				}
			}
		}
	}
}
