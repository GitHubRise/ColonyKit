#nullable disable

namespace rjw.Modules.Attraction
{
	/// <summary>
	/// 观察者如何看待自己相对于目标的关系。
	/// </summary>
	public enum AttractionStatus
	{
		/// <summary>
		/// <para>该小人没有配偶。</para>
		/// <para>不过，可能还有其他意识形态因素会让该小人
		/// 想要避免约炮。</para>
		/// </summary>
		Open,
		/// <summary>
		/// 请求的目标是该小人的配偶。
		/// </summary>
		Marital,
		/// <summary>
		/// 该小人至少有一名配偶，而目标不是其中之一。
		/// </summary>
		Extramarital
	}
}
