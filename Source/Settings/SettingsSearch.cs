using System;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 设置页检索状态：控制器画搜索框，<see cref="SettingsWidgets"/> 按查询过滤控件。
	/// </summary>
	/// <remarks>
	/// 检索范围是「当前页」：页签标题不参与过滤，切页后查询保留。
	/// 匹配不区分大小写，空查询视为未启用。
	/// </remarks>
	public static class SettingsSearch
	{
		public static string Query = "";

		public static bool Active => !Query.NullOrEmpty() && Query.Trim().Length > 0;

		/// <summary>当前页命中的控件/文本数量，由 <see cref="SettingsWidgets"/> 递增。</summary>
		public static int PageMatches { get; internal set; }

		public static void BeginPage()
		{
			PageMatches = 0;
		}

		public static bool Matches(string text)
		{
			if (!Active)
			{
				return true;
			}
			if (text.NullOrEmpty())
			{
				return false;
			}

			return text.IndexOf(Query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
