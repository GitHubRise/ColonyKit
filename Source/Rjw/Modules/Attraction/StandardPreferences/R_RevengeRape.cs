#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理强暴者报复某人的欲望。</para>
	/// <para>“为了这个殖民地的民众，我发誓…… 复仇！”</para>
	/// </summary>
	public sealed class R_RevengeRape : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 必须是用于强暴。
			if (!request.Purpose.IsForRape()) return;
			// 不适用于动物之间…… 暂时还不。
			if (request.Category.InvolvesAnimals()) return;

			request.SetPreference(new R_RevengeRape());
		}

		private R_RevengeRape() : base(
			AttractionMode.Romantic,
			nameof(R_RevengeRape),
			FactorOperation.Add,
			AttractionUtility.CanHaveRevengeRape,
			whenTrue: FixedValue(0.2f))
		{ }
	}
}
