#nullable disable

namespace rjw.Modules.Attraction
{
	/// <summary>
	/// 主要的吸引力因素。
	/// </summary>
	public enum AttractionMode
	{
		/// <summary>
		/// 这考虑了一个小人如何看待另一个小人的年龄。
		/// </summary>
		Age,
		/// <summary>
		/// 这考虑了一个小人如何看待另一个小人的身体。  它还可能考虑
		/// 影响小人的因素，如药物、性欲或生育力。
		/// </summary>
		Physical,
		/// <summary>
		/// 这考虑了性向、恋人等事物，以及其他
		/// 影响爱意的因素。
		/// </summary>
		Romantic,
		/// <summary>
		/// 这考虑了看法、社会角色以及 RJW 的标记等事物。
		/// </summary>
		Social
	}
}
