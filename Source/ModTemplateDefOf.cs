using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// DefOf：把 XML 里的 defName 绑定成静态字段，代码里就不用写字符串。
	/// 字段名必须和 defName 完全一致（<c>Default</c> ↔ <c>&lt;defName&gt;Default&lt;/defName&gt;</c>）。
	/// </summary>
	/// <remarks>
	/// 绑定发生在 XML 解析之后、<c>[StaticConstructorOnStartup]</c> 之前，
	/// 所以 Bootstrap 里可以安全使用；在 Mod 构造函数里用则一定拿到 null。
	///
	/// 静态构造函数里的 <c>EnsureInitializedInCtor</c> 只是加一道保险：
	/// 真在过早的时机访问时，它会给出「DefOf 尚未初始化」的明确报错，
	/// 而不是让 null 流到业务代码里变成更难查的空引用。
	/// </remarks>
	[DefOf]
	public static class ModTemplateDefOf
	{
		/// <summary>演示配置，对应 Defs/DemoConfig.xml 里的 defName=Default。</summary>
		public static DemoConfigDef Default;

		static ModTemplateDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(ModTemplateDefOf));
		}
	}
}
