#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 动物观察被指定为配种目标的个体时施加加成。
	/// </summary>
	public sealed class S_AnimalToBreedee : FlatAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅适用于性行为。
			if (request.Purpose.IsNotForSex()) return;
			// 观察者必须是已驯服的动物。
			if (!request.Pawn.IsTameAnimal()) return;
			// 且目标必须被设为配种目标。
			if (!request.Target.IsDesignatedBreeding()) return;

			request.SetPreference(new S_AnimalToBreedee());
		}

		private S_AnimalToBreedee() : base(
			AttractionMode.Social,
			nameof(S_AnimalToBreedee),
			FactorOperation.Add,
			0.2f)
		{ }
	}
}
