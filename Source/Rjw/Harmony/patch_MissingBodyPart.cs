#nullable disable

﻿using Verse;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace rjw
{
	[HarmonyPatch(typeof(Hediff_MissingPart), "PostAdd")]
	public class Hediff_MissingBodyPart_RemoveParts
	{
		[HarmonyPostfix]
		static void PostAdd(Hediff_MissingPart __instance)
		{
			// 获取该状态所附着的身体部位
			var bodyPart = __instance.Part;

			if (bodyPart == null) 
				return;
			if (bodyPart.def == null) 
				return;

			// 只处理 RJW 身体部位
			if (bodyPart.def == xxx.anusDef || bodyPart.def == xxx.mechanusDef || bodyPart.def == xxx.genitalsDef || bodyPart.def == xxx.mechgenitalsDef || bodyPart.def == xxx.breastsDef || bodyPart.def == xxx.mechbreastsDef)
			{
				// 如果该身体部位有效且带有状态
				if ( __instance.pawn.health.hediffSet.hediffs.Any(h => h.Part == bodyPart && h.def != __instance.def))
				{
					// 获取该部位上的所有状态，排除当前这个
					List<Hediff> hediffsToRemove = __instance.pawn.health.hediffSet.hediffs.Where(h => h.Part == bodyPart && h.def != __instance.def).ToList();

					// 移除每个状态
					foreach (Hediff hediff in hediffsToRemove)
					{
						__instance.pawn.health.RemoveHediff(hediff);
					}
				}
			}
		}
	}
}
