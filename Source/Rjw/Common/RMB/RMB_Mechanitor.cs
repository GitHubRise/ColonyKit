#nullable disable

﻿using RimWorld;
using System.Collections.Generic;
using Verse;
using Multiplayer.API;
using Verse.AI;

namespace rjw.RMB
{
	/// <summary>
	/// Biotech 机械师的 RMB 分类生成器
	/// </summary>
	static class RMB_Mechanitor
	{
		public static IEnumerable<FloatMenuOption> GetOptions(Pawn pawn, Pawn target)
		{
			if (!ModsConfig.BiotechActive)
			{
				yield break;
			}

			FloatMenuOption opt = GenerateCategoryOption(pawn, target);
			if (opt != null)
				yield return opt;
		}

		private static FloatMenuOption GenerateCategoryOption(Pawn pawn, Pawn target)
		{
			if (MechanitorUtility.IsMechanitor(pawn))
			{
				if (target?.IsVisiblyPregnant() == true)
				{
					Hediff_MechanoidPregnancy pregnancy = (Hediff_MechanoidPregnancy)target.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("RJW_pregnancy_mech"));
					if (pregnancy != null && pregnancy.is_checked && !pregnancy.is_hacked)
					{
						return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RJW_RMB_HackMechPregnancy".Translate(), delegate ()
						{
							HackMechPregnancy(pawn, target);
						}, MenuOptionPriority.High), pawn, target);
					}
				}
			}

			return null;
		}

		[SyncMethod] // 不确定是否需要
		static void HackMechPregnancy(Pawn pawn, Pawn target)
		{
			Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("RJW_HackMechPregnancy"), target);
			pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}
	}
}
