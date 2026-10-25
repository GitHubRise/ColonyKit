#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>当动物观察自己的物种时施加加成。</para>
	/// <para>这基本相当于 `P_HumanToBodyType`，只不过
	/// 是针对动物的。</para>
	/// </summary>
	public sealed class P_AnimalToSameSpecies : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 请求必须发生在动物之间。
			if (request.Category is not AttractionCategory.BetweenAnimals) return;

			request.SetPreference(new P_AnimalToSameSpecies());
		}

		private P_AnimalToSameSpecies() : base(
			AttractionMode.Physical,
			nameof(P_AnimalToSameSpecies),
			FactorOperation.Add,
			IsSameSpecies,
			whenTrue: FixedValue(0.25f))
		{ }

		private static bool IsSameSpecies(ref AttractionRequest request) =>
			request.Target.IsSameSpecies(request.Pawn);
	}
}
