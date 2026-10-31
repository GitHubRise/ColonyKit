using System;
using ColonyKit.Modules.Robotics;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	internal static class HygieneUtility
	{
		/// <summary>普通喝水增加 2 点（0.02）饱腹感；凉白开增加 5 点（0.05）。</summary>
		public const float SatietyPerDrink = 0.02f;

		public const float SatietyPerBoiledDrink = 0.05f;

		public static void AddDrinkSatiety(Pawn pawn)
		{
			AddDrinkSatiety(pawn, SatietyPerDrink);
		}

		public static void AddDrinkSatiety(Pawn pawn, float points)
		{
			if (pawn?.needs?.food == null)
			{
				return;
			}
			pawn.needs.food.CurLevel = Mathf.Min(1f, pawn.needs.food.CurLevel + points);
		}

		public static Need_Hygiene EnsureNeed(Pawn pawn)
		{
			return Ensure<Need_Hygiene>(pawn, "CK_Hygiene");
		}

		public static Need_Bladder EnsureBladder(Pawn pawn)
		{
			return Ensure<Need_Bladder>(pawn, "CK_Bladder");
		}

		public static Need_Thirst EnsureThirst(Pawn pawn)
		{
			return Ensure<Need_Thirst>(pawn, "CK_Thirst");
		}

		/// <summary>机械保姆的补需求入口：读档/出厂时直接把口渴和膀胱补上。</summary>
		public static void EnsureRobotNeeds(Pawn pawn)
		{
			if (!NannyBotUtility.IsNannyBot(pawn) || pawn.needs == null)
			{
				return;
			}
			EnsureThirst(pawn);
			EnsureBladder(pawn);
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
			if (def == null || def.needClass == null || !ShouldInject(pawn, def))
			{
				return null;
			}
			Need need = (Need)Activator.CreateInstance(def.needClass, pawn);
			need.def = def;
			pawn.needs.AllNeeds.Add(need);
			need.SetInitialLevel();
			return need as T;
		}

		/// <summary>
		/// 手动补需求时也必须走和 ShouldHaveNeed 补丁一致的判断，
		/// 否则读档时会把 DBH 已软停用的需求重新加回去。
		/// </summary>
		internal static bool ShouldInject(Pawn pawn, NeedDef def)
		{
			return HygieneSettings.Current.ShouldInject(pawn, def);
		}

		/// <summary>设置里的策略/豁免变化后，让所有小人的需求列表立即重新评估。</summary>
		public static void RefreshAllPawns()
		{
			if (Current.Game == null)
			{
				return;
			}
			foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive)
			{
				if (pawn?.needs == null
					|| ((!pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)
						&& !NannyBotUtility.IsNannyBot(pawn)))
				{
					continue;
				}
				pawn.needs.AddOrRemoveNeedsAsAppropriate();
				EnsureRobotNeeds(pawn);
			}
		}
	}
}
