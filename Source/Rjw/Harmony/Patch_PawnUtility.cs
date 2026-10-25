#nullable disable

﻿using HarmonyLib;
using Verse;
using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace rjw
{
	/// <summary>
	/// 修补 PawnUtility，以修复人形子女出生后的问题
	/// </summary>
	[HarmonyPatch(typeof(PawnUtility), "TrySpawnHatchedOrBornPawn")]
	static class Patch_PawnUtility
	{
		//[HarmonyPrefix]
		//static void prefix_TrySpawnHatchedOrBornPawn(ref Pawn pawn, Thing motherOrEgg)
		//{
		//	//ModLog.Message("prefix_TrySpawnHatchedOrBornPawn::");
		//	//ModLog.Message(" " + __result);
		//	//ModLog.Message(" " + xxx.get_pawnname(pawn));
		//	//ModLog.Message(" " + xxx.get_pawnname(motherOrEgg as Pawn));
		//	//ModLog.Message(" " + RJWPregnancySettings.humanlike_pregnancy_enabled);
		//	//ModLog.Message(" " + xxx.is_human(pawn));
		//	//ModLog.Message(" " + !xxx.is_mechanoid(pawn));
		//	//string last_name = NameTriple.FromString(pawn.Name.ToStringFull).Last;
		//	//ModLog.Message(" prefix_TrySpawnHatchedOrBornPawn Baby surname will be " + last_name);
		//}

		//会重置小人／对某些使用 C# 构造函数的种族无效
		[HarmonyPostfix]
		static void Process_Child_HatchOrBirth(ref bool __result, ref Pawn pawn, Thing motherOrEgg)
		{
			//ModLog.Message("postfix_TrySpawnHatchedOrBornPawn::");
			//ModLog.Message(" " + __result);
			//ModLog.Message(" " + xxx.get_pawnname(pawn));
			//ModLog.Message(" " + xxx.get_pawnname(motherOrEgg as Pawn));
			//ModLog.Message(" " + RJWPregnancySettings.humanlike_pregnancy_enabled);
			//ModLog.Message(" " + xxx.is_human(pawn));
			//ModLog.Message(" " + !xxx.is_mechanoid(pawn));
			//string last_name = NameTriple.FromString(pawn.Name.ToStringFull).Last;
			//ModLog.Message(" postfix_TrySpawnHatchedOrBornPawn 1 Baby surname will be " + last_name);
			//var skin_whiteness = pawn.story.melanin;
			//var last_name1 = pawn.story.birthLastName;
			if (__result
				&& RJWPregnancySettings.humanlike_pregnancy_enabled
				&& xxx.is_human(pawn)
				&& !xxx.is_mechanoid(pawn))
			{
				if ((motherOrEgg is Pawn && (motherOrEgg as Pawn).IsPregnant())	//原版 + RJW 怀孕
					|| (motherOrEgg.TryGetComp<CompHatcher>() != null))			//原版 + RJW 卵孕
				{
					changestory(pawn);
					removeimplants(pawn);
					removeskills(pawn);
					grow(pawn);
				}
			}
			//ModLog.Message(" postfix_TrySpawnHatchedOrBornPawn 2 Baby surname will be " + NameTriple.FromString(pawn.Name.ToStringFull).Last);
			//pawn.story.melanin = skin_whiteness;
			//pawn.story.birthLastName = last_name1;
		}

		static void removeimplants(Pawn pawn)
		{
			if (!pawn.health.hediffSet.hediffs.NullOrEmpty())
			{
				var hdlist = pawn.health.hediffSet.hediffs.ToList();
				foreach (Hediff hd in hdlist)
				{
					if (hd == null) continue;

					// 移除 DeathAcidifier
					//if (hd.def.defName == "DeathAcidifier")
					//{
					//	pawn.health.RemoveHediff(hd);
					//	continue;
					//}

					// 移除植入物
					if (hd is Hediff_Implant)
					{
						var part = hd.Part;
						pawn.health.RestorePart(part);
					}

					// 移除不死性
					if (xxx.ImmortalsIsActive && hd.def == xxx.IH_Immortal)
					{
						pawn.health.RemoveHediff(hd);
					}
				}
			}
		}

		//会重置／对某些种族无效
		static void changestory(Pawn pawn)
		{
			pawn.story.Childhood = null;
			pawn.story.Adulthood = null;

			// 把儿童设为部落背景
			try
			{
				if (!ModsConfig.BiotechActive)
					pawn.story.Childhood = DefDatabase<BackstoryDef>.AllDefsListForReading.FirstOrDefault(x => x.defName.Contains("rjw_childT"));
				else
					pawn.story.Childhood = DefDatabase<BackstoryDef>.GetNamed("Newborn79");
			}
			catch (Exception e)
			{
				ModLog.Warning(e.ToString());
			}
		}

		static void grow(Pawn pawn)
		{
			if (!ModsConfig.BiotechActive)
			{
				// 添加成长 
				if (!pawn.Dead)
				{
					pawn.health.AddHediff(xxx.RJW_BabyState, null, null);
					Hediff_SimpleBaby pawnstate = (Hediff_SimpleBaby)pawn.health.hediffSet.GetFirstHediffOfDef(xxx.RJW_BabyState);
					if (pawnstate != null)
					{
						pawnstate.GrowUpTo(0, true);
					}
				}
			}
		}

		static void removeskills(Pawn pawn)
		{
			// 移除技能
			foreach (var skill in pawn.skills?.skills)
				skill.Level = 0;
		}
	}
}
