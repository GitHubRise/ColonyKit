using System;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	internal static class HygieneUtility
	{
		public static Need_Hygiene EnsureNeed(Pawn pawn)
		{
			return Ensure<Need_Hygiene>(pawn, "CK_Hygiene");
		}

		public static Need_Bladder EnsureBladder(Pawn pawn)
		{
			return Ensure<Need_Bladder>(pawn, "CK_Bladder");
		}

		private static T Ensure<T>(Pawn pawn, string defName) where T : Need
		{
			if (pawn?.needs == null)
			{
				return null;
			}
			T existing = pawn.needs.TryGetNeed<T>();
			if (existing != null)
			{
				return existing;
			}
			NeedDef def = DefDatabase<NeedDef>.GetNamedSilentFail(defName);
			if (def == null || def.needClass == null)
			{
				return null;
			}
			Need need = (Need)Activator.CreateInstance(def.needClass, pawn);
			need.def = def;
			pawn.needs.AllNeeds.Add(need);
			need.SetInitialLevel();
			return need as T;
		}
	}
}
