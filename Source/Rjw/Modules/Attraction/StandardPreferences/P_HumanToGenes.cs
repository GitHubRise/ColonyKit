#nullable disable

using System.Linq;
using Verse;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理目标的基因。需要启用生物技术。
	/// </summary>
	public sealed class P_HumanToGenes : AttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 需要生物技术。
			if (!ModsConfig.BiotechActive) return;
			// 需要观察者拥有基因。
			if (request.Pawn.genes is null) return;
			// 不适用于动物目标。
			if (request.Category.AnimalTarget()) return;

			request.SetPreference(new P_HumanToGenes());
		}

		private P_HumanToGenes() : base(
			AttractionMode.Physical,
			nameof(P_HumanToGenes),
			FactorOperation.Multiply)
		{ }

		protected override float GetOperand(ref AttractionRequest request, float factor)
		{
			var pawn = request.Pawn;
			var target = request.Target;
			var operand = 1f;

			if (!ModsConfig.BiotechActive) return operand;
			if (target.genes is not { } genes) return operand;

			// 拥有某些基因会让你偏好同样拥有这些基因的人。
			// `SecondaryRomanceChanceFactor` 使心地善良的小人免于
			// 因缺少基因而受罚。模组也可能把这些改成加成，
			// 因此我们会在可能时采用这些值。
			var onlyBoosts = xxx.is_kind(pawn) || request.HasLoveRelationWithTarget;

			foreach (var gene in genes.GenesListForReading.Where(gene => gene.Active))
				if (gene.def.missingGeneRomanceChanceFactor != 1f)
					if (!onlyBoosts || gene.def.missingGeneRomanceChanceFactor > 1f)
						if (pawn.genes?.HasActiveGene(gene.def) is not true)
							operand *= gene.def.missingGeneRomanceChanceFactor;

			return operand;
		}
	}
}
