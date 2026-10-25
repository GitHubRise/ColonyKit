#nullable disable

using UnityEngine;
using Verse;
using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理人类观察小人身上野性的情况。</para>
	/// <para>这是根据小人的 `SexWildnessAversion` 属性施加的
	/// 惩罚，该属性越接近零，惩罚就越线性地
	/// 降低。</para>
	/// <para>训练动物也有助于降低此惩罚。</para>
	/// </summary>
	public sealed class S_HumanToWildness : AttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 观察者不能是动物或野人。
			if (request.Pawn.AnimalOrWildMan()) return;
			// 目标必须是动物或野人。
			if (!request.Target.AnimalOrWildMan()) return;

			request.SetPreference(new S_HumanToWildness());
		}

		private S_HumanToWildness() : base(
			AttractionMode.Social,
			nameof(S_HumanToWildness),
			FactorOperation.Multiply)
		{ }

		protected override float GetOperand(ref AttractionRequest request, float factor)
		{
			var target = request.Target;
			var wildness = GetWildness(target);
			if (wildness == 0f) return 1f;
			// 按其厌恶度的倒数降低野性。
			var reduction = 1f - request.Pawn.GetStatValueForPawn(RJWStatDefOf.SexWildnessAversion, target);
			// 剩余的野性会被反转，作为最终操作数。
			return 1f - Mathf.Clamp01(wildness - reduction);
		}

		private static float GetWildness(Pawn target)
		{
			// 死亡的目标显然无法表现得野性。
			if (target.Dead) return 0f;
			// 野人具有固定的野性。
			if (target.IsWildMan()) return AttractionUtility.WildManWildness;

			var baseWildness = target.GetStatValue(StatDefOf.Wildness);
			if (baseWildness == 0f) return 0f;

			// 每个可训练项都会降低野性。
			var trainingBonus = 0.2f * TrainingCount(target);
			return Mathf.Clamp01(baseWildness - trainingBonus);
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
