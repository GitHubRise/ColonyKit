#nullable disable

using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理动物与其天敌之间的关系。</para>
	/// <para>猎物会避免与天敌交配，但捕食者乐于
	/// 操潜在的猎物。另外，如果两个捕食者互
	/// 为对方的猎物，则两者抵消。</para>
	/// </summary>
	public sealed class P_AnimalToPredator : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 请求必须发生在动物之间。
			if (request.Category is not AttractionCategory.BetweenAnimals) return;
			// 目标必须是捕食者。
			if (!request.Target.RaceProps.predator) return;
			// 目标不能是同一物种。
			if (request.Target.IsSameSpecies(request.Pawn)) return;
			// 两个动物都已驯服时忽略；它们应该已习惯
			// 彼此相邻生活，并能识别其他已驯服的动物。
			if (request.Pawn.IsTameAnimal() && request.Target.IsTameAnimal()) return;

			request.SetPreference(new P_AnimalToPredator());
		}

		private P_AnimalToPredator() : base(
			AttractionMode.Physical,
			nameof(P_AnimalToPredator),
			FactorOperation.Multiply,
			IsPotentialPrey,
			whenTrue: FixedValue(0.4f))
		{ }

		private static bool IsPotentialPrey(ref AttractionRequest request)
		{
			var pawn = request.Pawn;
			var target = request.Target;
			var pPred = pawn.RaceProps.predator && FoodUtility.IsAcceptablePreyFor(pawn, target);
			var tPred = target.RaceProps.predator && FoodUtility.IsAcceptablePreyFor(target, pawn);
			return pPred != tPred && tPred;
		}
	}
}
