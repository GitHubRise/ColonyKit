using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using static rjw.GenderHelper;
using static rjw.RJWPreferenceSettings;

using MP = Multiplayer.API.MP;
using Rjw_sexuality = rjw.RJWPreferenceSettings.Rjw_sexuality;
using Seeded = rjw.Modules.Rand.Seeded;

namespace rjw
{
	// 在 `DO_LOGGING` 为 `false` 时禁用不可达代码警告。
	#pragma warning disable CS0162

	public static class OrientationUtility
	{
		/// <summary>
		/// <para>是否记录关于性向计算的信息。</para>
		/// <para>当此项为 `false` 时，JIT 编译器应优化掉所有检查此项的分支，
		/// 以及该检查带来的性能开销。</para>
		/// </summary>
		const bool DoLogging = false;
/*
		/// <summary>
		/// <para>Determines the orientation for a pawn in a deterministic way.</para>
		/// <para>This should never return `Orientation.None`.</para>
		/// </summary>
		/// <param name="pawn">The pawn to inspect.</param>
		/// <returns>The calculated orientation.</returns>
		public static Orientation DetermineOrientation(Pawn? pawn)
		{
			if (pawn is null) return Orientation.None;

			var orientation = sexuality_distribution switch
			{
				// Drones and simple droids are hard-coded as asexual until genitals are
				// strapped to them. At that point, they should roll a new orientation.
				_ when IsSexlessDrone(pawn) => Orientation.Asexual,
				// Try to get the orientation from the configured orientation source.
				Rjw_sexuality.Vanilla when FromVanillaTraits(pawn, out var vanillaOri) => vanillaOri,
				Rjw_sexuality.Psychology when FromPsychologySexuality(pawn, out var psyOri) => psyOri,
				Rjw_sexuality.SYRIndividuality when FromIndividualitySexuality(pawn, out var indOri) => indOri,
				// The fail safe in case any of these fail or RJW's random orientation
				// source is selected.
				_ => RollOrientation(pawn)
			};

			// If a nympho ends up asexual, force them to be pansexual instead.
			// if (xxx.is_nympho(pawn) && orientation == Orientation.Asexual)
			//   orientation = Orientation.Pansexual;

			// Handle how a pawn's gender dysphoria affects their orientation.
			orientation = HandleDysphoria(pawn, orientation);

			return orientation;
		}
*/
		public static float PreferenceFactor(Pawn pawn, Pawn partner)
		{
			//if (xxx.is_mechanoid(pawn))
			//	return 0f;

			if (pawn.GetCompRJW() is not { } rjwComp)
			{
				//ModLog.Message($"Error, pawn: {xxx.get_pawnname(pawn)} doesn't have CompRJW, modded race?");
				return 0f;
			}

			// 处于心灵恋爱中的小人总是彼此倾心。
			if (pawn.HasPsychicLoveWith(partner)) return 1f;

			var ori = rjwComp.orientation;

			// 可以直接跳过性别判断的简单情形。
			switch (ori) {
				case Orientation.None:
				case Orientation.Asexual:
					return 0f;
				case Orientation.Pansexual:
					return 1f;
				default:
					break;
			}

			var pawnSex = GetSex(pawn);
			var partnerSex = GetSex(partner);

			var isHetero = CanBeHetero(pawnSex, partnerSex);
			var isHomo = CanBeHomo(pawnSex, partnerSex);

			// 哦，你们这些疯狂的扶她，一发情就逮谁上谁！
			if (isHetero && isHomo) return 1f;

			return ori switch
			{
				// 只想要单一方向的性取向。
				Orientation.Heterosexual when isHetero && !isHomo => 1f,
				Orientation.Homosexual when isHomo && !isHetero => 1f,
				// 纯双性恋必须要有阴茎或阴道。
				// 毕竟他们不是泛性恋。
				Orientation.Bisexual when isHetero || isHomo => 1f,
				// 偏好较强时各种双性恋取向的层次。
				Orientation.MostlyHeterosexual when isHetero => 1f,
				Orientation.LeaningHeterosexual when isHetero => 1f,
				Orientation.LeaningHomosexual when isHomo => 1f,
				Orientation.MostlyHomosexual when isHomo => 1f,
				// 偏好较弱时各种双性恋取向的层次。
				Orientation.MostlyHeterosexual when isHomo => 0.2f,
				Orientation.LeaningHeterosexual when isHomo => 0.6f,
				Orientation.LeaningHomosexual when isHetero => 0.6f,
				Orientation.MostlyHomosexual when isHetero => 0.2f,
				_ => 0f
			};
		}

		/// <summary>
		/// <para>检查给定伴侣对给定小人是否有吸引力。</para>
		/// <para>这会将其性器官与 `pawn` 的性向进行比对。</para>
		/// <para>警告：这在单人模式下是非确定性的。</para>
		/// </summary>
		/// <param name="pawn">正在考虑伴侣的小人。</param>
		/// <param name="partner">被考虑的小人。</param>
		/// <returns>该伴侣是否满足其偏好。</returns>
		public static bool CheckPreference(Pawn pawn, Pawn partner)
		{
			CompRJW.UpdateOrientation(pawn);
			CompRJW.UpdateOrientation(partner);

			var factor = PreferenceFactor(pawn, partner);

			// 如果是这几种之一，就可以跳过随机数。
			if (factor <= 0f) return false;
			if (factor >= 1f) return true;

			// 在多人游戏中，我们避免使用随机数，以减少 `[SyncMethod]` 造成的卡顿，
			// 做法是假定只要他们对这个小人有那么一点兴趣，他们就会
			// 直接接受。
			if (MP.IsInMultiplayer) return true;

			// 在单人游戏中，我们会把这个系数当作概率使用。
			return Rand.Chance(factor);
		}

		/// <summary>
		/// 获取二元性向的反向性向。
		/// </summary>
		/// <param name="ori">性向。</param>
		/// <returns>该性向的反向性向，或该性向本身。</returns>
		public static Orientation GetReverse(Orientation ori) => ori switch
		{
			Orientation.Heterosexual => Orientation.Homosexual,
			Orientation.MostlyHeterosexual => Orientation.MostlyHomosexual,
			Orientation.LeaningHeterosexual => Orientation.LeaningHomosexual,
			Orientation.Homosexual => Orientation.Heterosexual,
			Orientation.MostlyHomosexual => Orientation.MostlyHeterosexual,
			Orientation.LeaningHomosexual => Orientation.LeaningHeterosexual,
			_ => ori
		};
/*
		/// <summary>
		/// <para>Yields all orientations between the given two.</para>
		/// <para>Only works for the binary orientations.</para>
		/// </summary>
		/// <param name="from">The starting point.</param>
		/// <param name="to">The ending point.</param>
		/// <returns>An enumerable of orientations.</returns>
		public static IEnumerable<Orientation> OrientationGradient(Orientation from, Orientation to)
		{
			// Check to make sure we even can scale.
			if (from == to) yield break;
			if (!CanScale(from)) yield break;
			if (!CanScale(to)) yield break;

			// Fortunately, the orientations enum has them in order, so we can
			// convert them to an integer and just run between them.
			var start = (int)from;
			var end = (int)to;
			if (end > start)
				for (var i = start; i <= end; i += 1)
					yield return (Orientation)i;
			else
				for (var i = start; i >= end; i -= 1)
					yield return (Orientation)i;

			static bool CanScale(Orientation ori) => ori switch
			{
				Orientation.None => false,
				Orientation.Asexual => false,
				Orientation.Pansexual => false,
				_ => true
			};
		}
*/
		/// <summary>
		/// <para>获取小人的性向。</para>
		/// <para>如果该小人为 `null` 或缺少 `CompRJW`，则返回 "None"。</para>
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>性向。</returns>
		public static Orientation GetOrientation(this Pawn? pawn) =>
			pawn?.GetCompRJW()?.orientation ?? Orientation.None;

		/// <summary>
		/// <para>将 "mostly" 或 "leaning" 性向简化为其最接近的明确
		/// 性向。</para>
		/// <para>例如 `MostlyHeterosexual` 变为 `Heterosexual`，`LeaningHeterosexual`
		/// 变为 `Bisexual`。</para>
		/// </summary>
		/// <param name="ori">要简化的性向。</param>
		/// <returns>简化后的性向。</returns>
		public static Orientation Simplify(this Orientation ori) => ori switch
		{
			Orientation.MostlyHeterosexual => Orientation.Heterosexual,
			Orientation.MostlyHomosexual => Orientation.Homosexual,
			Orientation.LeaningHeterosexual => Orientation.Bisexual,
			Orientation.LeaningHomosexual => Orientation.Bisexual,
			_ => ori
		};
/*
		private static bool IsMindlessDrone(Pawn pawn)
		{
			if (xxx.is_drone(pawn)) return true;
			if (pawn.IsUnsexyRobot()) return true;
			return false;
		}

		private static bool IsSexlessDrone(Pawn pawn)
		{
			// Someone strapped some junk to this thing, so it needs an orientation.
			if (Genital_Helper.has_genitals(pawn)) return false;
			return IsMindlessDrone(pawn);
		}

		private static Orientation HandleDysphoria(Pawn pawn, Orientation orientation)
		{
			switch (OrientationTransitionMode)
			{
				case TransitionMode.GraduallyReversed:
				return DoGraduallyReversed(pawn, orientation);
				case TransitionMode.SamePreference:
					return DoSamePreference(pawn, orientation);
				default:
					return orientation;
			}
		}

		/// <summary>
		/// <para>Handles the pawn's orientation such that their preferred sex
		/// remains the same even when they have flipped sexes.</para>
		/// <para>That is, if a gay female transitions, they will become a
		/// hetero male, still liking girls.  The only difference is they have
		/// a dick to fuck the girls they like with now.</para>
		/// </summary>
		/// <param name="pawn">The pawn to inspect.</param>
		/// <param name="orientation">Their natural orientation.</param>
		/// <returns>An adjusted orientation.</returns>
		private static Orientation DoSamePreference(Pawn pawn, Orientation orientation)
		{
			// Only consider cases where the pawn's sex has reversed from their original.
			return (GetOriginalSex(pawn), GetSex(pawn)) switch {
				// Had penis, now have vagina.
				(Sex.Male or Sex.Trap, Sex.Female) => GetReverse(orientation),
				// Had vagina, now have penis.
				(Sex.Female, Sex.Male or Sex.Trap) => GetReverse(orientation),
				// For any other combinations, just use the current orientation.
				_ => orientation
			};
		}

		/// <summary>
		/// <para>Handles the pawn's orientation such that they start off preferring
		/// the sex they did before a transition, but gradually begin to prefer the
		/// reverse sex as they accept the transition, going through a period
		/// of bisexuality.</para>
		/// <para>This is the one to use for fetishy stuff like emasculation torture.</para>
		/// </summary>
		/// <param name="pawn">The pawn to inspect.</param>
		/// <param name="orientation">Their natural orientation.</param>
		/// <returns>An adjusted orientation.</returns>
		private static Orientation DoGraduallyReversed(Pawn pawn, Orientation orientation)
		{
			if (!GetSexChangeComp(pawn, out var comp)) return orientation;

			// We only need to bother with this if they're not fully transitioned.
			if (comp.IsHappy) return orientation;

			// Only consider transitions between the traditional binaries.
			var shouldReverse = (comp.FromSex, comp.ToSex) switch {
				// Had penis, now have vagina.
				(Sex.Male or Sex.Trap, Sex.Female) => true,
				// Had vagina, now have penis.
				(Sex.Female, Sex.Male or Sex.Trap) => true,
				// For any other combinations, just use the current orientation.
				_ => false
			};

			if (!shouldReverse) return orientation;

			// We must actually have a reversible orientation.
			var reversed = GetReverse(orientation);
			if (reversed == orientation) return orientation;

			// Build the gradient from reversed to natural.  Just checking the
			// length to be thorough.
			var gradient = OrientationGradient(reversed, orientation).ToArray();
			if (gradient.Length == 0) return orientation;

			// Convert the transition progress into an index of the gradient.
			var pos = GenMath.LerpDouble(0f, 1f, 0, gradient.Length - 1, comp.Progress);
			var index = Math.Min(gradient.Length - 1, Math.Max(0, Mathf.FloorToInt(pos)));
			return gradient[index];
		}

		/// <summary>
		/// <para>Obtains an orientation from the Psychology mod.</para>
		/// <para>This may fail if the pawn has no `CompPsychology`.</para>
		/// <para>This is used for `Rjw_sexuality.Psychology`.</para>
		/// </summary>
		/// <param name="pawn">The pawn.</param>
		/// <param name="orientation">The pawn's new orientation.</param>
		/// <returns>Whether an orientation could be found.</returns>
		private static bool FromPsychologySexuality(Pawn pawn, out Orientation orientation)
		{
			orientation = Orientation.None;

			// Soft dependency: only succeeds when Psychology is loaded and granted the pawn a rating.
			if (ExternalSexuality.TryGetKinseyRating(pawn, out int kinsey))
			{
				orientation = ExternalSexuality.MapKinsey(kinsey);

				if (DoLogging)
					ModLog.Message($"RJW + Psychology: Inherited pawn ({xxx.get_pawnname(pawn)}) sexuality from Psychology - {orientation}");
			}
			else if (!pawn.IsAnimal() && !IsMindlessDrone(pawn))
				ModLog.Warning($"FromPsychologySexuality {xxx.get_pawnname(pawn)}, def: {pawn.def?.defName}, kindDef: {pawn.kindDef?.race.defName}");

			return orientation != Orientation.None;
		}

		/// <summary>
		/// <para>Obtains an orientation from the Individuality mod.</para>
		/// <para>This may fail if the pawn has no `CompIndividuality`.</para>
		/// <para>This is used for `Rjw_sexuality.SYRIndividuality`.</para>
		/// </summary>
		/// <param name="pawn">The pawn.</param>
		/// <param name="orientation">The pawn's new orientation.</param>
		/// <returns>Whether an orientation could be found.</returns>
		private static bool FromIndividualitySexuality(Pawn pawn, out Orientation orientation)
		{
			orientation = Orientation.None;

			// Soft dependency: only succeeds when [SYR] Individuality is loaded and set a sexuality.
			if (ExternalSexuality.TryGetIndividualitySexuality(pawn, out string individualitySexuality))
			{
				orientation = ExternalSexuality.MapIndividuality(individualitySexuality);

				if (DoLogging)
					ModLog.Message($"RJW + [SYR]Individuality: Inherited pawn ({xxx.get_pawnname(pawn)}) sexuality from Individuality - {orientation}");
			}
			else if (!pawn.IsAnimal() && !IsMindlessDrone(pawn))
				ModLog.Warning($"FromIndividualitySexuality {xxx.get_pawnname(pawn)}, def: {pawn.def?.defName}, kindDef: {pawn.kindDef?.race.defName}");

			return orientation != Orientation.None;
		}

		/// <summary>
		/// <para>Obtains an orientation from vanilla RimWorld traits.</para>
		/// <para>This may fail if the pawn has no traits tracker.</para>
		/// <para>This is used for `Rjw_sexuality.Vanilla`.</para>
		/// </summary>
		/// <param name="pawn">The pawn.</param>
		/// <param name="orientation">The pawn's new orientation.</param>
		/// <returns>Whether an orientation could be found.</returns>
		private static bool FromVanillaTraits(Pawn pawn, out Orientation orientation)
		{
			orientation = Orientation.None;

			if (!xxx.has_traits(pawn)) return false;

			if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
				orientation = Orientation.Asexual;
			else if (pawn.story.traits.HasTrait(TraitDefOf.Gay))
				orientation = Orientation.Homosexual;
			else if (pawn.story.traits.HasTrait(TraitDefOf.Bisexual))
				orientation = Orientation.Bisexual;
			else
				orientation = Orientation.Heterosexual;

			if (DoLogging)
				ModLog.Message($"RJW: Inherited pawn ({xxx.get_pawnname(pawn)}) sexuality from vanilla traits - {orientation}");

			return orientation != Orientation.None;
		}

		/// <summary>
		/// <para>Does a weighted-random roll for a pawn's orientation, based on the
		/// user-configured weights (incorrectly referred to as "ratios").</para>
		/// <para>This is seeded to the pawn, so repeated calls will return the
		/// same value.</para>
		/// <para>This is used for `Rjw_sexuality.RimJobWorld` or as a fallback.</para>
		/// </summary>
		/// <param name="pawn">The pawn to roll for.</param>
		/// <returns>The selected orientation.</returns>
		private static Orientation RollOrientation(Pawn pawn)
		{
			// Seed this roll, making it fully deterministic for multiplayer.
			using (Seeded.With(pawn))
				return RNG.RollOrientation(pawn);
		}

		/// <summary>
		/// These methods employ RNG.  You should be using a deterministic seed
		/// (using <see cref="Seeded" />) or using the Multiplayer API's
		/// `SyncMethod` attribute for methods responding to a player action.
		/// </summary>
		public static class RNG
		{
			private static readonly Orientation[] RollableOrientations = new[] {
				Orientation.Asexual,
				Orientation.Pansexual,
				Orientation.Heterosexual,
				Orientation.MostlyHeterosexual,
				Orientation.LeaningHeterosexual,
				Orientation.Bisexual,
				Orientation.LeaningHomosexual,
				Orientation.MostlyHomosexual,
				Orientation.Homosexual
			};

			/// <summary>
			/// <para>Determines the orientation weights for animals.</para>
			/// <para>A simpler system for animals, with most of them being heterosexual.
			/// Don't want to disturb player breeding projects by adding too many gay animals.</para>
			/// </summary>
			/// <param name="ori">The orientation to get the weight for.</param>
			/// <returns>The weight.</returns>
			private static float AnimalWeights(Orientation ori) => ori switch
			{
				Orientation.Asexual => 0.03f,
				Orientation.Heterosexual => 0.82f,
				Orientation.Bisexual => 0.11f,
				Orientation.Homosexual => 0.04f,
				_ => 0f
			};

			/// <summary>
			/// Creates a function that determines the orientation weights for non-animals.
			/// </summary>
			/// <returns>A weight function.</returns>
			private static Func<Orientation, float> HumanWeights()
			{
				// If bisexuality is set to 0, we'll just use a simple weighting function
				// that leaves bisexuality out of it.
				if (bisexual_ratio <= 0f)
				{
					return (Orientation ori) => ori switch
					{
						Orientation.Asexual => asexual_ratio,
						Orientation.Pansexual => pansexual_ratio,
						Orientation.Heterosexual => heterosexual_ratio,
						Orientation.Homosexual => homosexual_ratio,
						_ => 0f
					};
				}

				// At one point, RJW had only simple bisexuality, but compatibility with
				// mods adding shades of bisexuality was added and now we have weights
				// for hetero, homo, and bisexual that we need to split 7 ways.

				// I'm just going to throw them into a curve and sample at various points
				// to create a linear distribution between them.
				var curve = new SimpleCurve()
				{
					new(0f, heterosexual_ratio),
					new(0.5f, bisexual_ratio),
					new(1f, homosexual_ratio)
				};

				const float CurveOffset = 1f / 6f;

				return (Orientation ori) => ori switch
				{
					Orientation.Asexual => asexual_ratio,
					Orientation.Pansexual => pansexual_ratio,
					Orientation.Heterosexual => curve.Evaluate(0 * CurveOffset),
					Orientation.MostlyHeterosexual => curve.Evaluate(1 * CurveOffset),
					Orientation.LeaningHeterosexual => curve.Evaluate(2 * CurveOffset),
					Orientation.Bisexual => curve.Evaluate(3 * CurveOffset),
					Orientation.LeaningHomosexual => curve.Evaluate(4 * CurveOffset),
					Orientation.MostlyHomosexual => curve.Evaluate(5 * CurveOffset),
					Orientation.Homosexual => curve.Evaluate(6 * CurveOffset),
					_ => 0f
				};
			}

			/// <summary>
			/// <para>Does a weighted-random roll for a pawn's orientation, based on the
			/// user-configured weights (incorrectly referred to as "ratios").</para>
			/// </summary>
			/// <param name="pawn">The pawn to roll for.</param>
			/// <returns>The selected orientation.</returns>
			public static Orientation RollOrientation(Pawn pawn)
			{
				if (!Genital_Helper.has_genitals(pawn)) return Orientation.Asexual;

				Func<Orientation, float> weightFn = xxx.is_animal(pawn) ? AnimalWeights : HumanWeights();
				if (RollableOrientations.TryRandomElementByWeight(weightFn, out var orientation))
					return orientation;

				return Orientation.Asexual;
			}
		}*/
	}
}