#nullable disable

namespace rjw.Modules.Attraction
{
	/// <summary>
	/// 吸引力请求的目的是什么。
	/// </summary>
	public enum AttractionPurpose
	{
		/// <summary>
		/// 大体上衡量一个小人对另一个小人的兴趣程度。
		/// </summary>
		General,
		/// <summary>
		/// 衡量一个小人有多想和另一个小人做爱。
		/// </summary>
		ForFucking,
		/// <summary>
		/// 衡量一个小人有多想强暴另一个小人。
		/// </summary>
		ForRape
	}

	public static class AttractionPurposeExtensions
	{
		/// <summary>
		/// 用于检查吸引力目的是否与性无关的便捷扩展方法。
		/// </summary>
		/// <param name="purpose">要检查的目的。</param>
		/// <returns>是否与性无关。</returns>
		public static bool IsNotForSex(this AttractionPurpose purpose) =>
			purpose is AttractionPurpose.General;

		/// <summary>
		/// 用于检查吸引力目的是否为任何形式的性行为（包括强暴）的
		/// 便捷扩展方法。
		/// </summary>
		/// <param name="purpose">要检查的目的。</param>
		/// <returns>是否为任何形式的性行为。</returns>
		public static bool IsForSex(this AttractionPurpose purpose) =>
			purpose is not AttractionPurpose.General;

		/// <summary>
		/// 用于检查吸引力目的是否为强暴的便捷扩展方法。
		/// </summary>
		/// <param name="purpose">要检查的目的。</param>
		/// <returns>是否为强暴。</returns>
		public static bool IsForRape(this AttractionPurpose purpose) =>
			purpose is AttractionPurpose.ForRape;
	}
}
