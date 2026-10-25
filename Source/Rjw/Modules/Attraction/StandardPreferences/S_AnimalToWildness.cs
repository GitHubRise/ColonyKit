#nullable disable

using UnityEngine;
using Verse;
using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理动物（或野人）观察另一者并偏好与自身
	/// 相近野性的情况。</para>
	/// <para>与 `S_HumanToWildness` 类似，目标的训练会降低
	/// 观察动物所感知到的野性，但没有人类那样
	/// 明显。</para>
	/// </summary>
	public sealed class S_AnimalToWildness : CurveAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 观察者必须是动物或野人。
			if (!request.Pawn.AnimalOrWildMan()) return;
			// 目标必须是动物或野人。
			if (!request.Target.AnimalOrWildMan()) return;
			// 观察者必须未被驯服。
			if (request.Pawn.IsTameAnimal()) return;
			// 目标必须未死亡。
			if (request.Target.Dead) return;

			request.SetPreference(new S_AnimalToWildness());
		}

		/// <summary>
		/// 曲线的点，将野性之差映射为要施加的系数。
		/// 这里使用绝对值差。
		/// </summary>
		private static readonly CurvePoint[] wildnessPoints = new CurvePoint[]
		{
			new(0f, 1f),
			new(0.1f, 1f),
			new(0.2f, 0.9f),
			new(0.4f, 0.7f)
		};

		private S_AnimalToWildness() : base(
			AttractionMode.Social,
			nameof(S_AnimalToWildness),
			FactorOperation.Multiply,
			GetWildnessDifference,
			wildnessPoints)
		{ }

		private static float GetWildnessDifference(ref AttractionRequest request) =>
			Mathf.Abs(GetWildness(request.Pawn) - GetWildness(request.Target));

		private static float GetWildness(Pawn target)
		{
			// 野人具有固定的野性。
			if (target.IsWildMan()) return AttractionUtility.WildManWildness;

			var baseWildness = target.GetStatValue(StatDefOf.Wildness);
			if (baseWildness == 0f) return 0f;

			// 每个可训练项都会降低野性。
			var trainingModifier = 0.1f * TrainingCount(target);
			return Mathf.Clamp01(baseWildness - trainingModifier);
		}

		private static int TrainingCount(Pawn target)
		{
			if (target.training is not { } training) return 0;
			if (!target.IsTameAnimal()) return 0;

			// 已驯服的动物至少拥有“驯服度”这一可训练项，
			// 因此至少能得到 1。
			var count = 0;
			foreach (var def in DefDatabase<TrainableDef>.AllDefsListForReading)
				if (training.HasLearned(def))
					count += 1;
			return count;
		}
	}
}
