using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 自定义 Def：XML 里的根元素必须是**类型全名**（<c>ModTemplate.DemoConfigDef</c>），
	/// 因为游戏用 <c>GenTypes.GetTypeInAnyAssembly</c> 按名字解析节点。
	/// </summary>
	/// <remarks>
	/// 选它而不是 ThingDef / HediffDef，是为了让模板零美术资源也能跑：
	/// 纯数据 Def 不需要贴图、不参与渲染，只承载配置。
	/// 要加真实内容时，从这里复制一个类，再把字段换成业务需要的即可。
	/// </remarks>
	public class DemoConfigDef : Def
	{
		/// <summary>启动自检时打印的问候语，用来确认 XML → 强类型对象的链路通了。</summary>
		public string greeting = "Hello from ModTemplate";

		/// <summary>
		/// 可选备注，由 <c>Patches/OptionalCompatibility.xml</c> 在特定 Mod 激活时追加。
		/// 默认空串，所以没装对应 Mod 时设置页不会显示这一行。
		/// </summary>
		public string note = "";

		/// <summary>演示用数值字段；实际项目里换成自己的配置项。</summary>
		public int logIntervalTicks = 60000;
	}
}
