#nullable disable

using Seeded = rjw.Modules.Rand.Seeded;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 啤酒眼镜能让你看到截然不同的视角……
	/// </summary>
	public sealed class R_BeerGoggles : FlatAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅用于性行为。
			if (request.Purpose.IsNotForSex()) return;
			// 必须处于醉酒状态。
			if (!request.Pawn.HasBeerGoggles(out var severity)) return;

			using (Seeded.ForHour(request.Pawn, request.Target))
				request.SetPreference(new R_BeerGoggles(Verse.Rand.Range(-1f, 1f) * severity));
		}

		private R_BeerGoggles(float operand) : base(
			AttractionMode.Romantic,
			nameof(R_BeerGoggles),
			FactorOperation.Add,
			operand)
		{ }
	}
}
