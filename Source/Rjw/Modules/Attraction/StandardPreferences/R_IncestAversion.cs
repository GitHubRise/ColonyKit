#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理观察者的非恋爱关系，用于抑制
	/// 乱伦遭遇。
	/// </summary>
	public sealed class R_IncestAversion : AttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request) =>
			request.SetPreference(new R_IncestAversion());

		private R_IncestAversion() : base(
			AttractionMode.Romantic,
			nameof(R_IncestAversion),
			FactorOperation.Multiply)
		{ }

		protected override float GetOperand(ref AttractionRequest request, float factor)
		{
			var operand = 1f;

			// `romanceChanceFactor` 专门用于压制乱伦。
			// `RelationsUtility.Incestuous` 方法会检查 `romanceChanceFactor != 1f`，
			// 所以即使是加成，原版 RimWorld 也会算作乱伦，因此我
			// 就不费心去检查是否为加成值了。这里也包含动物，
			// 因为 RJW 加入了动物怀孕，因而也有家庭关系。
			foreach (var relDef in request.Relations)
				operand *= relDef.romanceChanceFactor;
			return operand;
		}
	}
}
