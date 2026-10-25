#nullable disable


using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rjw
{
	///<summary>
	///从干尸上移除 RJW 部件
	///</summary>
	[HarmonyPatch(typeof(Corpse), nameof(Corpse.RotStageChanged))]
	[StaticConstructorOnStartup]
	static class Corpse_RotStageChanged
	{
		[HarmonyPostfix]
		private static void RemoveParts(ref Corpse __instance)
		{
			var rottable = __instance?.GetComp<CompRottable>();
			if (rottable?.Stage is not RotStage.Dessicated) return;

			Pawn pawn = __instance.InnerPawn;
			if (pawn?.GetCompRJW() is null) return;

			//if (set.Any()) 
			//{
			//	List<Hediff> hediffsToRemove = set.Where(h => h is not Hediff_ArtificialSexPart).ToList();

			//	// 移除每个状态
			//	foreach (Hediff hediff in hediffsToRemove)
			//	{
			//		pawn.health.RemoveHediff(hediff);
			//	}
			//}
			//if (pawn.GetTorsoList().NullOrEmpty())
			//	pawn.health.AddHediff(HediffDefOf.MissingBodyPart, partBPR);

			List<Hediff> sexParts = pawn.GetTorsoList();
			if (sexParts?.Count > 0)
			{
				int partCount = sexParts.Count;
				for (int i = partCount - 1; i >= 0; i--)
				{
					pawn.health.RemoveHediff(sexParts[i]);
				}
			}

			AddPartsMissing(pawn, Genital_Helper.get_anusBPR(pawn));
			AddPartsMissing(pawn, Genital_Helper.get_breastsBPR(pawn));
			AddPartsMissing(pawn, Genital_Helper.get_genitalsBPR(pawn));
		}

		public static void AddPartsMissing(Pawn pawn, BodyPartRecord partBPR)
		{
			if (partBPR is not null && !pawn.health.hediffSet.PartIsMissing(partBPR))
			{
				pawn.health.AddHediff(HediffDefOf.MissingBodyPart, partBPR);
			}
		}
	}
}
