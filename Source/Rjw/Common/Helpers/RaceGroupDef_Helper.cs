#nullable disable

﻿using rjw.Modules.Shared.Extensions;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rjw
{
	class RaceGroupDef_Helper
	{
		/// <summary>
		/// TryGetRaceGroupDef 的缓存。
		/// </summary>
		static readonly IDictionary<PawnKindDef, RaceGroupDef> RaceGroupByPawnKind = new Dictionary<PawnKindDef, RaceGroupDef>();

		public static bool TryGetRaceGroupDef(PawnKindDef pawnKindDef, out RaceGroupDef raceGroupDef)
		{
			if (pawnKindDef == null)
			{
				ModLog.Error("Called TryGetRaceGroupDef with pawnKindDef = null");
				raceGroupDef = null;
				return false;
			}

			if (RaceGroupByPawnKind.TryGetValue(pawnKindDef, out raceGroupDef))
				{
					return raceGroupDef != null;
				}
				else
				{
					raceGroupDef = GetRaceGroupDefInternal(pawnKindDef);
					RaceGroupByPawnKind.Add(pawnKindDef, raceGroupDef);
					return raceGroupDef != null;
				}
		}
		public static bool TryGetRaceGroupDef(Pawn pawn, out RaceGroupDef raceGroupDef)
		{
			if (pawn == null)
			{
				ModLog.Error("Called TryGetRaceGroupDef with pawn = null");
				raceGroupDef = null;
				return false;
			}

			if (pawn.kindDef == null)
			{
				ModLog.Error($"{pawn} has kindDef = null");
				raceGroupDef = null;
				return false;
			}

			return TryGetRaceGroupDef(pawn.kindDef, out raceGroupDef);
		}

		/// <summary>
		/// 返回该小人最匹配的 RaceGroupDef，若找不到则返回 null。
		/// </summary>
		static RaceGroupDef GetRaceGroupDefInternal(Pawn pawn)
		{
			return GetRaceGroupDefInternal(pawn.kindDef);
		}
		static RaceGroupDef GetRaceGroupDefInternal(PawnKindDef kindDef)
		{
			var raceName = kindDef.race.defName;
			var pawnKindName = kindDef.defName;
			var groups = DefDatabase<RaceGroupDef>.AllDefs;

			var kindMatches = groups.Where(group => group.pawnKindNames?.Contains(pawnKindName) ?? false).ToList();
			var raceMatches = groups.Where(group => group.raceNames?.Contains(raceName) ?? false).ToList();
			var count = kindMatches.Count() + raceMatches.Count();
			if (count == 0)
			{
				//ModLog.Message($"Pawn named '{pawn.Name}' matched no RaceGroupDef. If you want to create a matching RaceGroupDef you can use the raceName '{raceName}' or the pawnKindName '{pawnKindName}'.");
				return null;
			}
			else if (count == 1)
			{
				// ModLog.Message($"Pawn named '{pawn.Name}' matched 1 RaceGroupDef.");
				return kindMatches.Concat(raceMatches).Single();
			}
			else
			{
				// ModLog.Message($"Pawn named '{pawn.Name}' matched {count} RaceGroupDefs.");

				// 如果匹配到多个 RaceGroupDef，则从中选一个。
				// 首先优先选择不在 rjw 中定义的定义。
				// 然后优先选择按 kind 匹配而非按种族匹配。
				return kindMatches.FirstOrDefault(match => !IsThisMod(match))
					?? raceMatches.FirstOrDefault(match => !IsThisMod(match))
					?? kindMatches.FirstOrDefault()
					?? raceMatches.FirstOrDefault();
			}
		}

		static bool IsThisMod(Def def)
		{
			var rjwContent = LoadedModManager.RunningMods.Single(pack => pack.Name == "RimJobWorld");
			return rjwContent.AllDefs.Contains(def);
		}

		/// <summary>
		/// 如果选定了种族部件（即使该部件是“无部件”）则返回 true。
		/// </summary>
		public static bool TryAddRacePart(Pawn pawn, BodyPartRecord partBPR, Gender gender = Gender.None)
		{
			if (!TryGetRaceGroupDef(pawn, out var raceGroupDef))
			{
				// 没有种族，因此没有选出任何东西。
				return false;
			}

			if (!RacePartDef_Helper.TryChooseRacePartDef(pawn, raceGroupDef, partBPR, out var racePartDef, gender))
			{
				// 找不到部件，因此没有选出任何东西。
				return false;
			}

			if (racePartDef.IsNone)
			{
				// “无部件”被明确选中。
				return true;
			}

			if (!racePartDef.TryGetHediffDef(out var hediffDef))
			{
				// 找不到 hediffDef。
				return false;
			}

			if (partBPR.IsMissingForPawn(pawn))
			{
				// 身体部位已毁坏。
				return false;
			}

			var hediff = RacePartDef_Helper.MakePart(hediffDef, pawn, partBPR, racePartDef);
			pawn.health.AddHediff(hediff, partBPR);
			// 已选定并添加了一个部件。
			return true;
		}

		/// <summary>
		/// <para>检查两个小人是否为相容的配偶。</para>
		/// <para>这会检查它们在各自种族组中是否有相似类型的生殖器；
		/// 只要有任何重叠，就视为相容。
		/// 如果一方缺少种族组，则回退到比较它们的皮革。</para>
		/// <para>例如：狗和座狼都有犬科解剖结构，因此相容。</para>
		/// <para>对类人小人始终返回 true，因为有趣！</para>
		/// </summary>
		/// <param name="pawn">第一个小人。</param>
		/// <param name="other">第二个小人。</param>
		/// <returns>两个小人是否相容。</returns>
		public static bool IsCompatibleForMating(Pawn pawn, Pawn other)
		{
			// 类人小人始终兼容。
			if (pawn.IsHumanLike() || other.IsHumanLike()) return true;
			// 同种族始终兼容。
			if (pawn.kindDef.race == other.kindDef.race) return true;
			// 史莱姆可以变形出任何生殖器。
			if (xxx.is_slime(pawn) || xxx.is_slime(other)) return true;

			// 若任一方缺少种族组，则回退到其他属性。
			if (!TryGetRaceGroupDef(pawn.kindDef, out var pGroup))
				return MatingCompatibilityFallback(pawn, other);
			if (!TryGetRaceGroupDef(other.kindDef, out var oGroup))
				return MatingCompatibilityFallback(pawn, other);

			// 如果它们属于同一组，就无需做任何特殊处理。
			if (pGroup == oGroup) return true;

			// 从这里开始，我们只是检查两组在生殖器方面是否有重叠。
			foreach (var pPart in pGroup.maleGenitals)
				foreach (var oPart in oGroup.maleGenitals)
					if (pPart == oPart) return true;
			foreach (var pPart in pGroup.femaleGenitals)
				foreach (var oPart in oGroup.femaleGenitals)
					if (pPart == oPart) return true;

			return false;
		}

		private static bool MatingCompatibilityFallback(Pawn pawn, Pawn other)
		{
			var pProps = pawn.RaceProps;
			var oProps = other.RaceProps;
			// 它们的掠食行为是否不同？
			if (pProps.predator != oProps.predator) return false;
			// 它们吃的东西是否不同？
			if (pProps.foodType != oProps.foodType) return false;
			// 它们的皮革类型是否不同？
			if (pProps.leatherDef != oProps.leatherDef) return false;
			// 它们的肉类型是否不同？
			if (pProps.meatDef != oProps.meatDef) return false;
			return true;
		}
	}
}
