#nullable disable

namespace rjw.Modules.Attraction
{
	/// <summary>
	/// 标准身体类型的枚举。模组可以将自己的自定义身体
	/// 映射到这些值，以便 RJW 以一致的方式处理它们。
	/// </summary>
	public enum StandardizedBody
	{
		/// <summary>
		/// <para>未知身体。</para>
		/// <para>涵盖两种情况：没有 `story` 的非动物，或者其
		/// `story.bodyType` 无法匹配到常见原型的小人。</para>
		/// </summary>
		Unknown,
		/// <summary>
		/// <para>机械族的通用身体。</para>
		/// <para>也用作兜底，以应对机械族竟然拥有背景故事、
		/// 但身体类型未知的罕见情况。</para>
		/// </summary>
		Mechanoid,
		/// <summary>
		/// <para>虫族的通用身体。</para>
		/// <para>也用作兜底，以应对虫族竟然拥有背景故事、
		/// 但身体类型未知的罕见情况。</para>
		/// </summary>
		Insectoid,
		/// <summary>
		/// <para>给予其他任何动物的身体类型。</para>
		/// <para>也用作兜底，以应对动物竟然拥有背景故事、
		/// 但身体类型未知的罕见情况。</para>
		/// </summary>
		Animal,
		/// <summary>
		/// 类人婴儿身体。
		/// </summary>
		Baby,
		/// <summary>
		/// 类人儿童身体。
		/// </summary>
		Child,
		/// <summary>
		/// 类人男性身体。
		/// </summary>
		Male,
		/// <summary>
		/// 类人女性身体。
		/// </summary>
		Female,
		/// <summary>
		/// 类人瘦削身体。
		/// </summary>
		Thin,
		/// <summary>
		/// 类人壮硕身体。
		/// </summary>
		Hulk,
		/// <summary>
		/// 类人肥胖身体。
		/// </summary>
		Fat
	}
}
