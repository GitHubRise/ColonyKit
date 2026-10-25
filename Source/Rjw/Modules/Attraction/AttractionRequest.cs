using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using rjw.Modules.Shared.Logs;
using static RimWorld.LovePartnerRelationUtility;

namespace rjw.Modules.Attraction
{
	public ref struct AttractionRequest
	{
		/// <summary>
		/// 用于自定义某个请求的吸引力偏好的委托。
		/// </summary>
		/// <param name="request">要修改的请求。</param>
		public delegate void ModifyPreferences(ref AttractionRequest request);

		/// <summary>
		/// 进行观察的小人。
		/// </summary>
		public readonly Pawn Pawn;

		/// <summary>
		/// 被观察的小人。
		/// </summary>
		public readonly Pawn Target;

		/// <summary>
		/// <para>吸引力将如何被使用。</para>
		/// <para>系数会依此值而以不同方式计算。</para>
		/// </summary>
		public readonly AttractionPurpose Purpose;

		/// <summary>
		/// 这两个小人如何分类，即动物还是人类。
		/// </summary>
		public readonly AttractionCategory Category;

		/// <summary>
		/// <para>表示这两者之间是否存在任何形式的恋爱关系。</para>
		/// <para>尽管恋兽者会将该纽带视为恋爱关系，但这里只计入
		/// 人类的标准关系。</para>
		/// </summary>
		public bool HasLoveRelationWithTarget =>
			hasLoveRelation ??= Relations.Any(IsLovePartnerRelation);
		private bool? hasLoveRelation = null;

		/// <summary>
		/// <para>该小人与其目标之间关系的缓存列表。</para>
		/// <para>请不要修改此列表。</para>
		/// </summary>
		public List<PawnRelationDef> Relations =>
			relations ??= Pawn.GetRelations(Target).ToList();
		private List<PawnRelationDef>? relations = null;

		/// <summary>
		/// 此实例是否正在阻止操纵偏好的尝试。
		/// </summary>
		public bool IsLocked => IsLocked;
		private bool isLocked = false;

		/// <summary>
		/// 表示该小人的关系相对于目标所处的状态。
		/// </summary>
		public AttractionStatus Status =>
			status ??= AttractionUtility.GetStatus(Pawn, Target);
		private AttractionStatus? status = null;

		/// <summary>
		/// <para>用于忽略出血预检的选项。</para>
		/// <para>仅在目的为性时才被考虑。</para>
		/// </summary>
		public bool ignoreBleeding = false;

		/// <summary>
		/// 设为 false 时跳过对系数做最终处理的选项。
		/// </summary>
		public bool finalize = true;

		/// <summary>
		/// 可选的修饰器，设置后可在怪癖之前自定义偏好。
		/// </summary>
		public ModifyPreferences? preQuirkModifier = null;

		/// <summary>
		/// 可选的修饰器，设置后可在怪癖之后自定义偏好。
		/// </summary>
		[Obsolete]
		public ModifyPreferences? postQuirkModifier = null;

		/// <summary>
		/// 该偏好的日志记录器实例。
		/// </summary>
		private readonly ILog Log;

		/// <summary>
		/// 小人偏好的主存储。
		/// </summary>
		private readonly Dictionary<string, AttractionPreference> Preferences =
			new(AttractionUtility.StandardApplicatorsCount);

		/// <summary>
		/// 使用给定目的创建一个新请求。
		/// </summary>
		/// <param name="purpose">要使用的目的。</param>
		/// <param name="pawn">进行观察的小人。</param>
		/// <param name="target">被观察的小人。</param>
		public AttractionRequest(AttractionPurpose purpose, Pawn pawn, Pawn target)
		{
			Log = LogManager.GetLogger("AttractionRequest");
			Purpose = purpose;
			Pawn = pawn;
			Target = target;
			Category = AttractionUtility.GetCategory(pawn, target);
		}

		/// <summary>
		/// 创建一个通用请求。
		/// </summary>
		/// <param name="pawn">进行观察的小人。</param>
		/// <param name="target">被观察的小人。</param>
		public AttractionRequest(Pawn pawn, Pawn target)
		: this(AttractionPurpose.General, pawn, target) { }

		public bool HasPreference(string key) =>
			Preferences.ContainsKey(key);

		public AttractionPreference? GetPreference(string key)
		{
			if (isLocked)
			{
				Log.Error($"Tried to get preference with key `{key}` while locked.");
				return null;
			}
			return Preferences.TryGetValue(key, out var pref) ? pref : null;
		}

		public T? GetPreference<T>(string key) where T : AttractionPreference =>
			GetPreference(key) as T;

		public void SetPreference(AttractionPreference pref)
		{
			// 锁定期间唯一允许的操作是添加新偏好。
			if (isLocked && HasPreference(pref.Key))
			{
				Log.Error($"Tried to replace preference with key `{pref.Key}` while locked.");
				return;
			}
			Preferences[pref.Key] = pref;
		}

		public void RemovePreference(string key)
		{
			if (isLocked)
			{
				Log.Error($"Tried to remove preference with key `{key}` while locked.");
				return;
			}
			Preferences.Remove(key);
		}

		/// <summary>
		/// 获取求值器实例。请在设置好请求后，
		/// 用 `using` 块调用它。
		/// </summary>
		/// <returns>求值器实例。</returns>
		public PreferenceEvaluator GetEvaluator() => new(Preferences.Values);

		public override string ToString() =>
			$"{xxx.get_pawnname(Pawn)} -> {xxx.get_pawnname(Target)}";
	}

	/// <summary>
	/// 这控制对共享资源的访问，该资源用于减少求值请求时产生的
	/// GC 开销。
	/// </summary>
	public readonly ref struct PreferenceEvaluator
	{
		/// <summary>
		/// 内部偏好存储。
		/// </summary>
		private static readonly List<AttractionPreference> SortedStorage = [];

		public PreferenceEvaluator(IEnumerable<AttractionPreference> prefs)
		{
			SortedStorage.AddRange(prefs);
			SortedStorage.Sort(AttractionPreference.Comparer);
		}

		public void Dispose() =>
			SortedStorage.Clear();

		/// <summary>
		/// <para>为给定模式执行一次求值。</para>
		/// <para>遗憾的是目前还没有 ref 字段，因此你只能在此处
		/// 传入请求，而不能在构造函数中传入。</para>
		/// </summary>
		/// <param name="mode">要求值的模式。</param>
		/// <param name="request">吸引力请求。</param>
		/// <returns>该模式的系数。</returns>
		public float Evaluate(AttractionMode mode, ref AttractionRequest request)
		{
			// 由于加法和乘法都可交换，只要
			// 我们分别进行，就应该总能得到相同的值。
			// 可以保证 `PrefCache` 对每种可能的
			// 组合都有条目，因此不需要 `TryGetValue`。

			var factor = 1f;

			// 我们先做加法部分。
			foreach (var pref in SortedStorage)
				if (pref.Mode == mode && pref.Operation == FactorOperation.Add)
					pref.Evaluate(ref request, ref factor);

			// 若是这种情况，就没必要做乘法部分了。
			if (factor <= 0f) return 0f;

			// 接下来做乘法部分。
			foreach (var pref in SortedStorage)
			{
				if (pref.Mode == mode && pref.Operation == FactorOperation.Multiply)
				{
					pref.Evaluate(ref request, ref factor);
					// 若遇到 0 就提前中止。
					if (factor <= 0f) return 0f;
				}
			}

			// 最后是按系数相加的伪乘法部分。
			foreach (var pref in SortedStorage)
				if (pref.Mode == mode && pref.Operation == FactorOperation.AddByFactor)
					pref.Evaluate(ref request, ref factor);

			return factor;
		}

		/// <summary>
		/// <para>为给定模式执行一次求值，但会生成详细解释该过程的
		/// 字符串列表。</para>
		/// </summary>
		/// <param name="mode">要求值的模式。</param>
		/// <param name="request">吸引力请求。</param>
		/// <param name="factor">用于存放系数的变量。</param>
		/// <returns>解释列表。</returns>
		public List<TaggedString> Explain(
			AttractionMode mode,
			ref AttractionRequest request,
			out float factor)
		{
			factor = 1f;
			var list = new List<TaggedString>();

			foreach (var pref in SortedStorage)
				if (pref.Mode == mode && pref.Operation == FactorOperation.Add)
					list.Add(pref.Explain(ref request, ref factor));

			factor = Mathf.Max(0f, factor);

			foreach (var pref in SortedStorage)
			{
				if (pref.Mode == mode && pref.Operation == FactorOperation.Multiply)
				{
					list.Add(pref.Explain(ref request, ref factor));
					factor = Mathf.Max(0f, factor);
				}
			}

			foreach (var pref in SortedStorage)
				if (pref.Mode == mode && pref.Operation == FactorOperation.AddByFactor)
					list.Add(pref.Explain(ref request, ref factor));

			factor = Mathf.Max(0f, factor);
			return list;
		}
	}
}