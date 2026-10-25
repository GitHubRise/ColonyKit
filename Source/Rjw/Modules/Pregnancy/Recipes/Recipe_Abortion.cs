#nullable disable

﻿using RimWorld;
using System;
using System.Linq;
using Verse;
using System.Collections.Generic;

namespace rjw
{
	public class Recipe_Abortion : Recipe_RemoveHediff
	{
		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
			BodyPartRecord part = pawn.RaceProps.body.corePart;
			if (recipe.appliedOnFixedBodyParts[0] != null)
				part = pawn.RaceProps.body.AllParts.Find(x => x.def == recipe.appliedOnFixedBodyParts[0]);
			if (part != null)
			{
				bool isMatch = Hediff_BasePregnancy.KnownPregnancies() // 遍历所有已知怀孕
					.Where(x => pawn.health.hediffSet.HasHediff(HediffDef.Named(x), true) && recipe.removesHediff == HediffDef.Named(x)) // 查找匹配的身体部位 
					.Select(x => (Hediff_BasePregnancy)pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(x))) // 获取怀孕状态
					.Where(pregnancy => pregnancy.is_discovered &&
						(!pregnancy.is_checked || !(pregnancy is Hediff_MechanoidPregnancy))) // 查找可见的怀孕或未检查的机械怀孕
					.Any(); // 若找到内容则返回 true
				if (isMatch)
				{
					yield return part;
				}
			}
		}
		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			if (this is not Recipe_PregnancyAbortMech)
				foreach (Hediff x in pawn.health.hediffSet.hediffs.Where(x => x is Hediff_MechanoidPregnancy))
				{
					(x as Hediff_MechanoidPregnancy).GiveBirth();
					return;
				}
			base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
		}
	}
	public class Recipe_PregnancyAbortMech : Recipe_Abortion
	{

		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
			BodyPartRecord part = pawn.RaceProps.body.corePart;
			if (recipe.appliedOnFixedBodyParts[0] != null)
				part = pawn.RaceProps.body.AllParts.Find(x => x.def == recipe.appliedOnFixedBodyParts[0]);
			if (part != null)
			{
				bool isMatch = Hediff_BasePregnancy.KnownPregnancies() // 遍历所有已知怀孕
					.Where(x => pawn.health.hediffSet.HasHediff(HediffDef.Named(x), true) && recipe.removesHediff == HediffDef.Named(x)) // 查找匹配的身体部位 
					.Select(x => (Hediff_BasePregnancy)pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(x))) // 获取怀孕状态
					.Where(pregnancy => pregnancy.is_checked && pregnancy is Hediff_MechanoidPregnancy) // 查找已检查/可见的怀孕
					.Any(); // 若找到内容则返回 true
				if (isMatch)
				{
					yield return part;
				}
			}
		}
	}
}
