#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理动物目标的年龄吸引力。
	/// </summary>
	public sealed class A_AgeForAnimal : AttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 目标必须是动物。
			if (!request.Category.AnimalTarget()) return;

			request.SetPreference(new A_AgeForAnimal());
		}

		private A_AgeForAnimal() : base(
			AttractionMode.Age,
			nameof(A_AgeForAnimal),
			FactorOperation.Multiply)
		{ }

		protected override float GetOperand(ref AttractionRequest request, float factor)
		{
			var target = request.Target;
			if (target.ageTracker.CurLifeStage.reproductive) return 1f;
			if (target.ageTracker.AgeBiologicalYears > 1) return 1f;
			// 非常年幼的动物；根据预期寿命做一些假设。
			return target.RaceProps.lifeExpectancy > 8f ? 0.5f : 1f;
		}
	}
}
