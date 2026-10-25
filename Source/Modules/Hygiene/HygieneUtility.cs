using System;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	internal static class HygieneUtility
	{
		public static Need_Hygiene EnsureNeed(Pawn pawn)
		{
			if (pawn?.needs == null)
			{
				return null;
			}

			Need_Hygiene existing = pawn.needs.TryGetNeed<Need_Hygiene>();
			if (existing != null)
			{
				return existing;
			}

			NeedDef def = DefDatabase<NeedDef>.GetNamedSilentFail("CK_Hygiene");
			if (def == null || def.needClass == null)
			{
				return null;
			}

			Need need = (Need)Activator.CreateInstance(def.needClass, pawn);
			need.def = def;
			pawn.needs.AllNeeds.Add(need);
			need.SetInitialLevel();
			return need as Need_Hygiene;
		}
	}
}
