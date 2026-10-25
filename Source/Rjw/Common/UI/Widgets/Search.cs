using System;
using RimWorld;
using Verse;

namespace rjw.RJWUI.RJWidgets;

/// <summary>
/// 用于快速搜索的缓存 AND 词元匹配器。
/// </summary>
public static class Search
{
	private static LoadedLanguage? cachedLang;
	private static readonly System.Collections.Generic.Dictionary<string, string> translationCache = new();
	private static readonly System.Collections.Generic.Dictionary<string, string> corpusCache = new();
	private static string cachedFilterText = "";
	private static string[] cachedTokens = Array.Empty<string>();

	/// <summary>
	/// 检查某一行是否匹配当前筛选文本。
	/// </summary>
	/// <param name="filter">快速筛选器。为 null 或未启用时返回 true。</param>
	/// <param name="labelKey">标签标识符，匹配前会先翻译。</param>
	/// <param name="tooltipKey">提示标识符，匹配前会先翻译。为 null 则跳过此项。</param>
	/// <param name="extraKeysOrLiterals">更多标识符或字面文本，匹配前会先翻译。</param>
	/// <returns>每个词元都能在该行文本中找到时为 true；筛选器未启用或为空时也为 true。</returns>
	/// <remarks>
	/// <para>词元按空白拆分，匹配时忽略大小写。翻译结果按语言缓存。</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// if (Search.Matches(filter, "LabelKey", "TooltipKey")) { DrawRow(); }
	/// </code>
	/// </example>
	/// <seealso cref="MatchesTranslated"/>
	public static bool Matches(QuickSearchFilter? filter, string labelKey, string? tooltipKey = null, params string[]? extraKeysOrLiterals)
	{
		if (filter is not { Active: true })
			return true;
		EnsureCachesValid();
		string[] tokens = GetTokens(filter);
		if (tokens.Length == 0)
			return true;
		string corpus = GetCorpus(labelKey, tooltipKey, extraKeysOrLiterals);
		for (int i = 0; i < tokens.Length; i++)
		{
			if (corpus.IndexOf(tokens[i], StringComparison.InvariantCultureIgnoreCase) == -1)
				return false;
		}
		return true;
	}

	/// <summary>
	/// 用已翻译的行文本检查当前筛选文本。
	/// </summary>
	/// <param name="filter">快速筛选器。为 null 或未启用时返回 true。</param>
	/// <param name="textA">要搜索的第一段文本。</param>
	/// <param name="textB">要搜索的第二段文本。为 null 时只搜索 <paramref name="textA"/>。</param>
	/// <returns>每个词元都能找到时为 true；筛选器未启用或为空时也为 true。</returns>
	/// <remarks>
	/// <para>每个词元都必须出现在 <paramref name="textA"/> 或 <paramref name="textB"/> 中。跨越两段文本的词元不算匹配。</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// if (Search.MatchesTranslated(filter, label, tooltip)) { DrawRow(); }
	/// </code>
	/// </example>
	/// <seealso cref="Matches"/>
	public static bool MatchesTranslated(QuickSearchFilter? filter, string? textA, string? textB = null)
	{
		if (filter is not { Active: true })
			return true;
		EnsureCachesValid();
		string[] tokens = GetTokens(filter);
		if (tokens.Length == 0)
			return true;
		if (textA == null)
			textA = "";
		if (textB == null)
		{
			for (int i = 0; i < tokens.Length; i++)
			{
				if (textA.IndexOf(tokens[i], StringComparison.InvariantCultureIgnoreCase) == -1)
					return false;
			}
			return true;
		}
		// 每个词元必须匹配 A 或 B。忽略跨越 A/B 的词元。
		for (int i = 0; i < tokens.Length; i++)
		{
			string token = tokens[i];
			if (textA.IndexOf(token, StringComparison.InvariantCultureIgnoreCase) == -1
				&& textB.IndexOf(token, StringComparison.InvariantCultureIgnoreCase) == -1)
				return false;
		}
		return true;
	}

	private static void EnsureCachesValid()
	{
		if (cachedLang != LanguageDatabase.activeLanguage)
		{
			translationCache.Clear();
			corpusCache.Clear();
			cachedLang = LanguageDatabase.activeLanguage;
		}
	}

	private static string[] GetTokens(QuickSearchFilter filter)
	{
		string text = filter.Text ?? "";
		if (text != cachedFilterText)
		{
			cachedFilterText = text;
			cachedTokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		}
		return cachedTokens;
	}

	private static string TranslateCached(string? key)
	{
		if (key is null || key.Length == 0)
			return "";
		if (translationCache.TryGetValue(key, out string? cached))
			return cached;
		// 与 UI 相同的文本：译文、回退值或键。
		string resolved = key.Translate();
		translationCache[key] = resolved;
		return resolved;
	}

	private static string GetCorpus(string labelKey, string? tooltipKey, string[]? extraKeysOrLiterals)
	{
		// 以 NUL 连接的键。
		string cacheKey;
		if (tooltipKey == null && (extraKeysOrLiterals == null || extraKeysOrLiterals.Length == 0))
		{
			cacheKey = labelKey ?? "";
		}
		else
		{
			int extraCount = extraKeysOrLiterals?.Length ?? 0;
			string[] parts = new string[2 + extraCount];
			parts[0] = labelKey ?? "";
			parts[1] = tooltipKey ?? "";
			for (int i = 0; i < extraCount; i++)
				parts[i + 2] = extraKeysOrLiterals![i] ?? "";
			cacheKey = string.Join("\0", parts);
		}
		if (corpusCache.TryGetValue(cacheKey, out string? corpus))
			return corpus;
		corpus = BuildCorpus(labelKey, tooltipKey, extraKeysOrLiterals);
		corpusCache[cacheKey] = corpus;
		return corpus;
	}

	private static string BuildCorpus(string? labelKey, string? tooltipKey, string[]? extraKeysOrLiterals)
	{
		System.Text.StringBuilder sb2 = new(TranslateCached(labelKey));
		string tip = TranslateCached(tooltipKey);
		if (tip.Length > 0)
		{
			sb2.Append('\n');
			sb2.Append(tip);
		}
		if (extraKeysOrLiterals != null)
		{
			for (int i = 0; i < extraKeysOrLiterals.Length; i++)
			{
				string extra = extraKeysOrLiterals[i];
				if (string.IsNullOrEmpty(extra))
					continue;
				sb2.Append('\n');
				sb2.Append(TranslateCached(extra));
			}
		}
		return sb2.ToString();
	}
}
