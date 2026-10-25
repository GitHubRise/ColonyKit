using Verse;

namespace rjw.Modules.Attraction
{
	/// <summary>
	/// 吸引力调试界面用到的文本。
	/// </summary>
	/// <remarks>
	/// 这里只在缺少翻译时回退到英文原名，避免调试输出里出现未翻译的键名。
	/// </remarks>
	internal static class AttractionText
	{
		/// <summary>
		/// 取翻译；没有对应条目时返回 <paramref name="fallback"/>。
		/// </summary>
		public static string TranslateOr(string key, string fallback)
		{
			string text = key.Translate();
			return string.IsNullOrEmpty(text) || text == key ? fallback : text;
		}

		/// <summary>吸引力目的的名称。</summary>
		public static string PurposeName(AttractionPurpose purpose) =>
			TranslateOr("AttractionPurpose_" + purpose, GenText.SplitCamelCase(purpose.ToString()));

		/// <summary>吸引力模式的名称（年龄/外貌/情感/社交）。</summary>
		public static string ModeName(AttractionMode mode) =>
			TranslateOr("AttractionMode_" + mode, GenText.SplitCamelCase(mode.ToString()));

		/// <summary>单个偏好项的名称，<paramref name="strippedKey"/> 已去掉 A_/P_/R_/S_ 前缀。</summary>
		public static string PreferenceName(string strippedKey) =>
			TranslateOr("AttractionPref_" + strippedKey, GenText.SplitCamelCase(strippedKey).Replace("_", ", "));

		/// <summary>性别的名称。</summary>
		public static string SexName(GenderHelper.Sex sex) =>
			TranslateOr("SexLabel_" + sex, sex.ToString());
	}
}
