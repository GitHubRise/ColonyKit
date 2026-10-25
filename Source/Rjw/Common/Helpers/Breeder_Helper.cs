#nullable disable

//#define TESTMODE // Uncomment to enable logging.

using Multiplayer.API;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;
using Verse.AI;
using static rjw.CasualSex_Helper;

namespace rjw
{
	/// <summary>
	/// 与动物性交的辅助类
	/// </summary>
	public class BreederHelper
	{
		public const int max_animals_at_once = 1; // 别忘了目的是繁衍，而不是鸡奸

		const float MinimumAttraction = 0.1f;

		[Conditional("TESTMODE")]
		private static void DebugText(string msg)
		{
			ModLog.Message(msg);
		}

		public static bool IsBreedableFor(Pawn breeder, Pawn breedee)
		{
			// 基本资格检查。
			if (breedee == breeder) return false;
			if (breedee.Map != breeder.Map) return false;
			if (breedee.Suspended) return false;
			if (breedee.IsForbidden(breeder)) return false;
			if (breedee.HostileTo(breeder)) return false;
			if (!xxx.is_not_dying(breedee)) return false;
			if (!xxx.can_get_raped(breedee)) return false;
			if (!Pather_Utility.cells_to_target_rape(breeder, breedee)) return false;
			if (!breeder.CanReserve(breedee, max_animals_at_once)) return false;

			// 确保配对符合用户配置。
			if (!xxx.can_do_animalsex(breeder, breedee)) return false;

			return true;
		}

		public static IEnumerable<Pawn> DesignatedBreedeesFor(Pawn animal)
		{
			DebugText($" Count of designated breeders: {DesignatorsData.rjwBreeding.Count}");

			if (!animal.IsAnimal())
			{
				DebugText($"BreederHelper::find_designated_breeder( {xxx.get_pawnname(animal)} ) was not an animal");
				yield break;
			}

			foreach (var breedee in DesignatorsData.rjwBreeding)
			{
				// 基本资格检查。
				if (!IsBreedableFor(animal, breedee)) continue;

				// 已经怀孕的动物会被跳过。
				// 不过，怀孕的人类仍会继续被配种，因为有趣。
				if (breedee.IsPregnant() && breedee.IsAnimal()) continue;

				yield return breedee;
			}
		}

		/// <summary>
		/// 为动物寻找合适的、被指定用于配种的小人。
		/// </summary>
		/// <param name="pawn">执行配种的小人。</param>
		/// <returns>选中的配种对象，若未找到则为 null。</returns>
		public static Pawn FindDesignatedBreeder(Pawn pawn)
		{
			DebugText($"BreederHelper::FindDesignatedBreeder( {xxx.get_pawnname(pawn)} ) called");
			DebugText($" Pawn faction: {pawn.Faction}");
			
			var targets = DesignatedBreedeesFor(pawn);

			DebugText($" Initial target count: {targets.Count()}");

			// 供考虑的 TODO ——
			// 目前，指定配种总是被视为强暴。感觉
			// 这里应该处理得更细致。更好的做法可能是：
			// - 动物对动物始终是自愿的。
			// - 当人类对动物有吸引力时，动物对人是自愿的
			//   （吸引力大于 1）。这主要只适用于
			//   兽交癖者，但基本上会交给吸引力系统来
			//   做出判断。
			//   - 这里的一个交互点可能是：你可以直接让被指定的
			//     配种对象醉到愿意当配种者，直到兽交癖生效。
			// - 否则就按强暴处理。
			//
			// 实际的工作也需要遵守这些规则，并确保
			// 兽交癖者在真正乐在其中时不会遭受“崩坏”。

			var ignoreOrientation = Genital_Helper.has_male_bits(pawn) || xxx.is_insect(pawn);
			foreach (var result in SexAppraiser.FindVictims(pawn, targets, true, ignoreOrientation))
			{
				var target = result.Target;
				DebugText($" Checking target: {xxx.get_pawnname(target)}");

				if (result.ObserverAttraction < MinimumAttraction) continue;
				if (!Pather_Utility.can_path_to_target(pawn, target)) continue;

				DebugText($" Using target: {xxx.get_pawnname(target)}");
				DebugText($" Race of target: {target.kindDef.race.defName.ToLower()}");
				return target;
			}

			DebugText(" Found no available targets");
			return null;
		}

		public static IEnumerable<Pawn> AnimalBreedeesFor(Pawn pawn)
		{
			DebugText($" Count of map pawns: {pawn.Map.mapPawns.AllPawnsSpawned.Count}");

			foreach (var breedee in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				// 基本资格检查。
				if (!breedee.IsAnimal()) continue;
				if (!IsBreedableFor(pawn, breedee)) continue;

				yield return breedee;
			}
		}

		/// <summary>
		/// <para>为动物或人类寻找合适的动物进行配种。</para>
		/// <para>目标动物可能被指定用于配种，也可能没有。</para>
		/// </summary>
		/// <param name="pawn">执行配种的小人。</param>
		/// <returns>选中的配种对象，若未找到则为 null。</returns>
		[SyncMethod]
		public static Pawn FindAnimalToBreed(Pawn pawn)
		{
			DebugText("BreederHelper::FindAnimalToBreed( " + xxx.get_pawnname(pawn) + " ) called");

			var maxDanger = Danger.None;

			// 我们将裁剪这份初始的小人列表。
			var targets = AnimalBreedeesFor(pawn);

			DebugText($" Initial target count: {targets.Count()}");

			// 如果已经没有任何候选，直接跳到失败分支。
			if (!targets.Any()) goto notFound;

			var parts = pawn.GetGenitalsList();
			if (!Genital_Helper.has_male_bits(pawn, parts) && (Genital_Helper.has_vagina(pawn, parts) || Genital_Helper.has_anus(pawn)))
			{
				// 该小人只能被进入，因此找一个能做到的对象。
				targets = targets.Where(xxx.can_fuck);
			}

			if (xxx.need_some_sex(pawn) >= 3.0f)
			{
				// 如果性奋，允许选择可能处于安全温度之外的目标。
				maxDanger = Danger.Some;
			}

			if (pawn.IsAnimal())
			{
				// 动物只会选择它们能看到的跨物种目标。
				// 同物种目标仍可在远距离互相找到，
				// 有助于数量稀少的动物彼此相遇。
				targets = targets.Where(x => pawn.IsSameSpecies(x) || pawn.CanSee(x));

				// 动物总是忽略温度危险。
				maxDanger = Danger.Some;
			}

			DebugText($" Filtered target count: {targets.Count()}");

			foreach (var result in SexAppraiser.FindPartners(pawn, targets))
			{
				var target = result.Target;
				DebugText($" Checking target: {xxx.get_pawnname(target)}");

				if (result.ObserverAttraction < MinimumAttraction) continue;
				if (!Pather_Utility.can_path_to_target(pawn, target, maxDanger)) continue;
				if (GetCloseness(pawn, target) is QuickieCloseness.OutOfRange) continue;

				DebugText($" Using target: {xxx.get_pawnname(target)}");
				DebugText($" Race of target: {target.kindDef.race.defName.ToLower()}");
				return target;
			}

		notFound:
			DebugText(" Found no available targets");
			return null;
		}
	}
}
