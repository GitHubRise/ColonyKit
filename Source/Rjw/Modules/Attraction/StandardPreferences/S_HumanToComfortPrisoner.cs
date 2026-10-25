#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 人类小人对泄欲囚犯时施加加成。
	/// </summary>
	public sealed class S_HumanToComfortPrisoner : FlatAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅适用于性行为。
			if (request.Purpose.IsNotForSex()) return;
			// 不能涉及动物。
			if (request.Category.InvolvesAnimals()) return;
			// 且目标必须被设为泄欲对象。
			if (!request.Target.IsDesignatedComfort()) return;

			request.SetPreference(new S_HumanToComfortPrisoner());
		}

		private S_HumanToComfortPrisoner() : base(
			AttractionMode.Social,
			nameof(S_HumanToComfortPrisoner),
			FactorOperation.Add,
			0.2f)
		{ }
	}
}
