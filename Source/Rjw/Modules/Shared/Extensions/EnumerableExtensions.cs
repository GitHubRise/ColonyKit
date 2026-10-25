using System;
using System.Collections.Generic;
using System.Linq;

namespace rjw.Modules.Shared.Extensions
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// <para>一个可能适用于某个 `T` 类型值的函数。</para>
		/// <para>只有当它返回 `true` 时，`output` 才被认为有意义；
		/// 否则使用该值属于未定义行为。</para>
		/// </summary>
		/// <typeparam name="T">输入类型。</typeparam>
		/// <typeparam name="U">输出类型。</typeparam>
		/// <param name="val">输入值。</param>
		/// <param name="output">用于存储输出值的变量。</param>
		/// <returns>该函数是否适用于 `val`。</returns>
		public delegate bool PartialFunction<T, U>(T val, out U output);

		/// <summary>
		/// 使用 `keySelector` 函数筛选出不同的值。
		/// </summary>
		/// <typeparam name="TSource">值的类型。</typeparam>
		/// <typeparam name="TKey">键的类型。</typeparam>
		/// <param name="source">源可枚举对象。</param>
		/// <param name="keySelector">键选择器。</param>
		/// <returns>筛选后的可枚举对象。</returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector)
		{
			HashSet<TKey> seenKeys = new();
			foreach (TSource element in source)
				if (seenKeys.Add(keySelector(element)))
					yield return element;
		}

		/// <summary>
		/// <para>LINQ 的 `First` 方法，但采用 try 模式。</para>
		/// <para>若序列为空，`result` 将是 `T` 的默认值。</para>
		/// </summary>
		/// <typeparam name="T">任意类型。</typeparam>
		/// <param name="source">源可枚举对象。</param>
		/// <param name="result">用于存储结果的变量。</param>
		/// <returns>是否找到了元素。</returns>
		public static bool TryFirst<T>(this IEnumerable<T> source, out T result)
		{
			foreach (var item in source)
			{
				result = item;
				return true;
			}

			// 空的可枚举对象，因此使用默认值。
			result = default!;
			return false;
		}

		/// <summary>
		/// <para>以元组形式获取可枚举对象中的项及其索引。</para>
		/// <para>这适用于任何种类的可枚举对象（即使不可索引），
		/// 索引表示其产出的顺序。</para>
		/// </summary>
		/// <typeparam name="T">任意类型。</typeparam>
		/// <param name="self">该可枚举对象。</param>
		/// <returns>包含项与索引的元组可枚举对象。</returns>
		public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self) =>
			self.Select((item, index) => (item, index));

		/// <summary>
		/// <para>使用偏函数的筛选与映射组合函数。</para>
		/// </summary>
		/// <typeparam name="T">输入类型，来自该可枚举对象。</typeparam>
		/// <typeparam name="U">输出类型。</typeparam>
		/// <param name="self">此可枚举对象实例。</param>
		/// <param name="fn">要应用的偏函数。</param>
		/// <returns>函数适用处的新可枚举对象。</returns>
		public static IEnumerable<U> Collect<T, U>(this IEnumerable<T> self, PartialFunction<T, U> fn) {
			foreach (var val in self)
				if (fn(val, out var output))
					yield return output;
		}

		/// <summary>
		/// <para>在此可枚举对象中搜索第一个可应用于给定偏函数的值，
		/// 若存在则给出变换后的值。</para>
		/// </summary>
		/// <typeparam name="T">输入类型，来自该可枚举对象。</typeparam>
		/// <typeparam name="U">输出类型。</typeparam>
		/// <param name="self">此可枚举对象实例。</param>
		/// <param name="fn">要应用的偏函数。</param>
		/// <param name="result">用于放置结果的变量。</param>
		/// <returns>是否找到适用的值以及结果是否有意义。</returns>
		public static bool TryCollectFirst<T, U>(this IEnumerable<T> self, PartialFunction<T, U> fn, out U result) {
			foreach (var val in self)
				if (fn(val, out result))
					return true;

			result = default!;
			return false;
		}
	}
}
