using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using rjw.Modules.Shared.Logs;

namespace rjw.Modules.Attraction
{
	/// <summary>
	/// 如何将从曲线查得的值应用到系数上。
	/// </summary>
	public enum FactorOperation {
		/// <summary>
		/// 将该值加到系数上。
		/// </summary>
		Add,
		/// <summary>
		/// 将该值与系数相乘。
		/// </summary>
		Multiply,
		/// <summary>
		/// <para>将该值加到系数上，但该值是由当前系数
		/// 参与计算得出的。</para>
		/// <para>`GetOperand` 函数提供当前系数，
		/// 因此你可以执行类似 `factor += factor * scalar` 的运算。
		/// 使用此运算可以告知这一点，从而让该运算
		/// 以安全的顺序应用。</para>
		/// </summary>
		AddByFactor
	}

	/// <summary>
	/// <para>用于标记标准偏好应用器的特性。这些
	/// 偏好应自动应用到所有请求。</para>
	/// <para>该方法必须是静态的，且签名需匹配
	/// <see cref="AttractionUtility.ApplyPreference" /> 委托。它应检查
	/// 请求以判断该偏好是否应生效，若是则
	/// 将自身添加到请求中。</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class StandardPreferenceAttribute : Attribute { }

	/// <summary>
	/// 先按优先级降序、再按键排序，这样游戏各次运行之间的
	/// 求值顺序就是确定的。
	/// </summary>
	public class PrefComparer : IComparer<AttractionPreference>
	{
		public int Compare(AttractionPreference a, AttractionPreference b)
		{
			if (a.Priority.CompareTo(b.Priority) is var prio)
				if (prio is not 0)
					return -prio;
			return string.CompareOrdinal(a.Key, b.Key);
		}
	}

	/// <summary>
	/// <para>小人偏好的基类。</para>
	/// <para>实现者编写子类时，应使其便于针对怪癖的实现
	/// 随意调整。</para>
	/// </summary>
	public abstract class AttractionPreference
	{
		/// <summary>
		/// 全局比较器实例。
		/// </summary>
		public static readonly PrefComparer Comparer = new();

		/// <summary>
		/// 用于移除 "P_" 和 "R_" 等标准偏好前缀的正则表达式。
		/// </summary>
		private static readonly Regex PrefixRemover = new(@"^[APSR]_");

		private static readonly Color ColorWhite = GenColor.FromHex("FFFFFF");
		private static readonly Color ColorMuted = GenColor.FromHex("808080");
		private static readonly Color ColorImproved = GenColor.FromHex("66CC00");
		private static readonly Color ColorDegraded = GenColor.FromHex("FF3300");

		/// <summary>
		/// 该偏好的日志记录器实例。
		/// </summary>
		protected ILog Log;

		/// <summary>
		/// <para>该偏好适用的吸引力模式。</para>
		/// <para>仅当请求了此模式时，该偏好才会被应用。</para>
		/// </summary>
		public AttractionMode Mode { get; private set; }

		/// <summary>
		/// <para>用于从请求中查找该偏好的字符串。</para>
		/// <para>怪癖可以用该键取得某个众所周知的偏好，并调整它
		/// 以体现该怪癖的细微差别。</para>
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// 使用该值对系数施加何种运算。
		/// </summary>
		public FactorOperation Operation { get; private set; }

		/// <summary>
		/// <para>运行优先级。</para>
		/// <para>主要对很可能把系数设为零的乘法偏好有用。
		/// 给它们更高的优先级，可以通过提前终止
		/// 求值来优化性能。</para>
		/// </summary>
		public int Priority { get; protected set; } = 0;

		/// <summary>
		/// 初始化该偏好。子类必须调用。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="operation">如何使用该值改变系数。</param>
		protected AttractionPreference(
			AttractionMode mode,
			string key,
			FactorOperation operation)
		{
			Log = LogManager.GetLogger($"PawnPreference::{GetType().Name}");
			Mode = mode;
			Key = key;
			Operation = operation;
		}

		/// <summary>
		/// 求值该偏好，按需调整 `factor`。
		/// </summary>
		/// <param name="request">吸引力请求。</param>
		/// <param name="factor">当前系数。</param>
		public void Evaluate(ref AttractionRequest request, ref float factor)
		{
			// 确保子类拿到的系数不小于 0。
			var safeFactor = Mathf.Max(0f, factor);

			var operand = GetOperand(ref request, safeFactor);
			factor = Operation switch
			{
				FactorOperation.Add or FactorOperation.AddByFactor =>
					factor + operand,
				FactorOperation.Multiply when operand < 0f =>
					LogProblem(factor, "Multiplying by a negative number is not allowed."),
				FactorOperation.Multiply =>
					factor * operand,
				var unknown =>
					LogProblem(factor, $"Unknown FactorOperation: {(int)unknown}")
			};
		}

		/// <summary>
		/// 生成一个字符串，描述该偏好如何改变系数。
		/// 它应像正常求值那样更新系数。
		/// </summary>
		/// <param name="request">吸引力请求。</param>
		/// <param name="factor">当前系数。</param>
		/// <returns>说明字符串。</returns>
		public virtual TaggedString Explain(ref AttractionRequest request, ref float factor)
		{
			var noopValue = NoopValue(Operation);
			var prevFactor = factor;
			var safeFactor = Mathf.Max(0f, prevFactor);
			var operand = GetOperand(ref request, safeFactor);
			var operandText = Operation switch
			{
				FactorOperation.Add or FactorOperation.AddByFactor =>
					operand.ToStringByStyle(ToStringStyle.FloatMaxThree, ToStringNumberSense.Offset),
				_ =>
					$"{operand.ToStringByStyle(ToStringStyle.FloatMaxThree)}x"
			};
			Evaluate(ref request, ref factor);

			// 若系数未改变，则把颜色调暗。
			var labelColor = factor == prevFactor ? ColorMuted : ColorWhite;
			// 我们会根据操作数试图改变系数的方式为其着色。
			var operandColor
				= operand == noopValue ? labelColor
				: operand < noopValue ? ColorDegraded * labelColor
				: ColorImproved * labelColor;

			var label = PrefixRemover.Replace(Key, "");
			return new StringBuilder()
				.Append(AttractionText.PreferenceName(label))
				.Append(": ").AppendTagged(operandText.Colorize(operandColor))
				.Append(" ⇒ ").Append(factor.ToStringByStyle(ToStringStyle.FloatMaxThree))
				.ToString()
				.Colorize(labelColor);
		}

		private float LogProblem(float factor, string msg)
		{
			Log.Warning(msg);
			return factor;
		}

		/// <summary>
		/// 获取用于调整系数的操作数。
		/// </summary>
		/// <param name="request">吸引力请求。</param>
		/// <param name="factor">当前系数。</param>
		/// <returns>操作数。</returns>
		protected abstract float GetOperand(ref AttractionRequest request, float factor);

		/// <summary>
		/// 获取一个不会改变系数的值。
		/// </summary>
		/// <returns>该值。</returns>
		protected static float NoopValue(FactorOperation op) =>
			op == FactorOperation.Multiply ? 1f : 0f;
	}

	/// <summary>
	/// 使用固定值调整系数的小人偏好。
	/// </summary>
	public class FlatAttractionPreference : AttractionPreference
	{
		/// <summary>
		/// 该固定值。
		/// </summary>
		public float Value { get; set; }

		/// <summary>
		/// 实例化一个新的固定值偏好。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="operation">如何使用该值改变系数。</param>
		/// <param name="value">要应用的值。</param>
		public FlatAttractionPreference(
			AttractionMode mode,
			string key,
			FactorOperation operation,
			float value) : base(mode, key, operation)
		{
			Value = value;
		}

		protected override float GetOperand(ref AttractionRequest request, float factor) =>
			Value;
	}

	/// <summary>
	/// 根据谓词的求值结果改变系数的小人偏好。
	/// </summary>
	public class PredicatedAttractionPreference : AttractionPreference
	{
		public delegate bool PredicateFn(ref AttractionRequest request);

		public delegate float WhenFn(ref AttractionRequest request, float factor);

		/// <summary>
		/// 执行测试以判断该小人对目标持正面还是
		/// 负面反应的函数。
		/// </summary>
		public PredicateFn Predicate { get; set; }

		/// <summary>
		/// <para>`Test` 返回 true 时求值的函数。</para>
		/// <para>为 null 时，此情况不做任何处理。</para>
		/// </summary>
		public WhenFn? WhenTrue { get; set; }

		/// <summary>
		/// <para>`Test` 返回 false 时求值的函数。</para>
		/// <para>为 null 时，此情况不做任何处理。</para>
		/// </summary>
		public WhenFn? WhenFalse { get; set; }

		/// <summary>
		/// <para>实例化一个新的谓词偏好。</para>
		/// <para>提示：你可以对 `whenTrue` 和 `whenFalse` 使用命名参数，
		/// 只设置其中一个，被省略的那个视为空操作。</para>
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="operation">如何使用该值改变系数。</param>
		/// <param name="predicateFn">检查请求的函数。</param>
		/// <param name="whenTrue">谓词为 true 时对系数做什么。</param>
		/// <param name="whenFalse">谓词为 false 时对系数做什么。</param>
		public PredicatedAttractionPreference(
			AttractionMode mode,
			string key,
			FactorOperation operation,
			PredicateFn predicateFn,
			WhenFn? whenTrue = null,
			WhenFn? whenFalse = null) : base(mode, key, operation)
		{
			Predicate = predicateFn;
			WhenTrue = whenTrue;
			WhenFalse = whenFalse;
		}

		protected override float GetOperand(ref AttractionRequest request, float factor)
		{
			var whenFn = Predicate(ref request) ? WhenTrue : WhenFalse;
			if (whenFn is null) return NoopValue(Operation);
			return whenFn.Invoke(ref request, factor);
		}

		/// <summary>
		/// 使用固定值创建 `WhenFn` 委托。
		/// </summary>
		/// <param name="value">另一个操作数。</param>
		/// <returns>一个委托。</returns>
		public static WhenFn? FixedValue(float value) =>
			(ref AttractionRequest request, float factor) => value;
	}

	/// <summary>
	/// <para>检查某个特定值、再根据该值
	/// 改变系数的小人偏好。</para>
	/// <para>这基本上就是 LINQ 的 `Select` 运算符，只是用于偏好。</para>
	/// </summary>
	/// <typeparam name="T">要映射的类型。</typeparam>
	public class SelectAttractionPreference<T> : AttractionPreference
	{
		public delegate T FetchFn(ref AttractionRequest request);

		public delegate float SelectFn(T value, ref AttractionRequest request);

		/// <summary>
		/// 获取供选择使用的值的函数。
		/// </summary>
		public FetchFn Fetch { get; set; }

		/// <summary>
		/// 将该值与其选项比对并相应更新系数的
		/// 函数。
		/// </summary>
		public SelectFn Select { get; set; }

		/// <summary>
		/// 实例化一个新的选择偏好。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="operation">如何使用该值改变系数。</param>
		/// <param name="fetchFn">用于获取待选择值的函数。</param>
		/// <param name="selectFn">接收该值并更新系数的函数。</param>
		public SelectAttractionPreference(
			AttractionMode mode,
			string key,
			FactorOperation operation,
			FetchFn fetchFn,
			SelectFn selectFn) : base(mode, key, operation)
		{
			Fetch = fetchFn;
			Select = selectFn;
		}

		protected override float GetOperand(ref AttractionRequest request, float factor) =>
			Select(Fetch(ref request), ref request);
	}

	/// <summary>
	/// 小人偏好：从由枚举值表示的有限可能性中 
	/// 挑选一个值来改变系数。
	/// </summary>
	/// <typeparam name="T">枚举的类型。</typeparam>
	public class OptionAttractionPreference<T> : AttractionPreference where T : struct, Enum
	{
		public delegate T FetchFn(ref AttractionRequest request);

		/// <summary>
		/// 获取待查找值的函数。
		/// </summary>
		public FetchFn Fetch { get; set; }

		/// <summary>
		/// <para>选项，即从值到 float 的映射。</para>
		/// <para>若未找到该值，则改用默认值。</para>
		/// </summary>
		public Dictionary<T, float> Options { get; set; }

		/// <summary>
		/// 未找到合适选项时使用的默认值。
		/// </summary>
		public float Default { get; set; }

		/// <summary>
		/// 实例化一个新的选项偏好。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="operation">如何使用该值改变系数。</param>
		/// <param name="fetchFn">用于获取待挑选值的函数。</param>
		/// <param name="options">用于组装字典的选项。</param>
		/// <param name="defaultVal">查找失败时使用的值。</param>
		public OptionAttractionPreference(
			AttractionMode mode,
			string key,
			FactorOperation operation,
			FetchFn fetchFn,
			IEnumerable<KeyValuePair<T, float>> options,
			float defaultVal) : base(mode, key, operation)
		{
			Fetch = fetchFn;
			Options = options.ToDictionary((v) => v.Key, (v) => v.Value);
			Default = defaultVal;
		}

		/// <summary>
		/// 使用给定运算和不会改变系数的默认值
		/// 实例化一个新的选项偏好。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="operation">如何使用该值改变系数。</param>
		/// <param name="fetchFn">用于获取待挑选值的函数。</param>
		/// <param name="options">用于组装字典的选项。</param>
		public OptionAttractionPreference(
			AttractionMode mode,
			string key,
			FactorOperation operation,
			FetchFn fetchFn,
			IEnumerable<KeyValuePair<T, float>> options)
		: this(mode, key, operation, fetchFn, options, NoopValue(operation)) { }

		/// <summary>
		/// 使用乘法运算和不会改变系数的默认值
		/// 实例化一个新的选项偏好。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="fetchFn">用于获取待挑选值的函数。</param>
		/// <param name="options">用于组装字典的选项。</param>
		public OptionAttractionPreference(
			AttractionMode mode,
			string key,
			FetchFn fetchFn,
			IEnumerable<KeyValuePair<T, float>> options)
		: this(mode, key, FactorOperation.Multiply, fetchFn, options) { }

		protected override float GetOperand(ref AttractionRequest request, float factor) =>
			Options.TryGetValue(Fetch(ref request), out var result) ? result : Default;
	}

	public class CurveAttractionPreference : AttractionPreference
	{
		public delegate float FetchFn(ref AttractionRequest request);

		/// <summary>
		/// 获取用于对曲线求值的 `x` 值。
		/// </summary>
		public FetchFn Fetch { get; set; }

		/// <summary>
		/// 用于查找要应用到系数的值的曲线。
		/// </summary>
		public SimpleCurve Curve { get; private set; }

		/// <summary>
		/// 实例化一个新的曲线偏好。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="operation">如何使用该值改变系数。</param>
		/// <param name="fetchFn">获取用于对曲线求值的值的函数。</param>
		/// <param name="points">构成曲线的点。</param>
		public CurveAttractionPreference(
			AttractionMode mode,
			string key,
			FactorOperation operation,
			FetchFn fetchFn,
			IEnumerable<CurvePoint> points) : base(mode, key, operation)
		{
			Fetch = fetchFn;
			Curve = new(points);
		}

		/// <summary>
		/// 使用默认乘法运算实例化一个新的曲线偏好。
		/// </summary>
		/// <param name="mode">该偏好适用的吸引力模式。</param>
		/// <param name="key">该偏好的唯一查找键。</param>
		/// <param name="fetchFn">获取用于对曲线求值的值的函数。</param>
		/// <param name="points">构成曲线的点。</param>
		public CurveAttractionPreference(
			AttractionMode mode,
			string key,
			FetchFn fetchFn,
			IEnumerable<CurvePoint> points) : base(mode, key, FactorOperation.Multiply)
		{
			Fetch = fetchFn;
			Curve = new(points);
		}

		/// <summary>
		/// <para>用给定的点替换曲线中的所有点。</para>
		/// <para>如果你把另一个 `SimpleCurve` 当作来源，尤其是
		/// `static readonly` 的曲线，这样可以确保你原来的曲线不会被
		/// 其他怪癖之类的东西意外修改。</para>
		/// </summary>
		/// <param name="points">要设置的点。</param>
		public void ReplaceCurve(IEnumerable<CurvePoint> points)
		{
			if (!points.Any())
			{
				Log.Error("An attempt was made to replace the curve with an empty one.");
				return;
			}

			Curve.Points.Clear();
			foreach (var point in points) Curve.Add(point);
		}

		protected override float GetOperand(ref AttractionRequest request, float factor) =>
			Curve.Evaluate(Fetch(ref request));
	}
}