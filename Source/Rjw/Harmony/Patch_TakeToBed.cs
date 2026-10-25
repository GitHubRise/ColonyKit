#nullable disable

﻿using RimWorld;
using Verse.AI;
using HarmonyLib;
using Verse;

namespace rjw
{
	//阻止狱卒打断囚犯之间的性行为
	[HarmonyPatch(typeof(WorkGiver_Warden_TakeToBed), "TakeDownedToBedJob")]
	public static class TakeDownedToBedJob_Prefix
	{
		[HarmonyPrefix]
		private static bool Prefix(ref Pawn prisoner, ref Pawn warden, ref Job __result)
		{
			if (prisoner.jobs.curDriver is JobDriver_SexBaseReciever  && !prisoner.health.HasHediffsNeedingTend())
			{
				__result = null;
				return false;
			}

			return true;
		}
	}
}
