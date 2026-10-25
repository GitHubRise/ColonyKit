#nullable disable

using Verse;

namespace rjw.Modules.Rand
{
	/// <summary>
	/// <para>与 `using` 语句一起使用时，创建一个带种子的 RNG 上下文。</para>
	/// <para>退出代码块后，将恢复原有的 RNG 状态。</para>
	/// <example>
	/// <para>如何创建带种子的上下文：</para>
	/// <code>
	/// // 在 C# 文件顶部引入 `Seeded` 类型。
	/// using Seeded = rjw.Modules.Rand.Seeded;
	/// 
	/// string RandomYepNah(Pawn pawn)
	/// {
	///   // 把它放进一个 `using` 中，配以新种子或可靠的种子来源（比如
	///   // 这里的这个小人），然后在里面做你的随机操作。无需把
	///   // 它绑定到变量。
	///   using (Seeded.With(pawn))
	///   {
	///     var someChance = Verse.Rand.Chance(0.5f);
	///     return someChance ? "Yep!" : "Nah...";
	///   }
	/// }
	/// </code>
	/// <para>退出代码块时该 ref struct 会被释放，从而为你弹出
	/// RNG 状态。即使 using 块中抛出了异常也会如此，
	/// 所以 RNG 状态不会被搞乱。</para>
	/// <para>嵌套多个 RNG 上下文应当是安全的。它们会按
	/// 正确的顺序弹出。</para>
	/// </example>
	/// </summary>
	public readonly ref struct Seeded
	{
		public readonly int seed;

		private Seeded(int replacementSeed)
		{
			seed = replacementSeed;
			Verse.Rand.PushState(replacementSeed);
		}

		public void Dispose()
		{
			Verse.Rand.PopState();
		}

		/// <summary>
		/// <para>与 `using` 语句一起使用时，创建一个隔离的 RNG 上下文。</para>
		/// <para>退出代码块后，将恢复原有的 RNG 状态。</para>
		/// <para>此版本将使用给定的种子。</para>
		/// </summary>
		/// <param name="replacementSeed">要使用的种子。</param>
		/// <returns>一个 RNG 上下文。</returns>
		public static Seeded With(int replacementSeed) =>
			new(replacementSeed);
		
		/// <summary>
		/// <para>与 `using` 语句一起使用时，创建一个隔离的 RNG 上下文。</para>
		/// <para>退出代码块后，将恢复原有的 RNG 状态。</para>
		/// <para>此版本将使用基于物体 ID 的种子，因此
		/// 每次进入代码块时，该物体的随机结果
		/// 都可复现。只要你始终执行相同的操作，它
		/// 就总会产生相同的 RNG。</para>
		/// </summary>
		/// <param name="thing">用作种子来源的物体。</param>
		/// <returns>一个 RNG 上下文。</returns>
		public static Seeded With(Thing thing) =>
			new(thing.HashOffset());
		
		/// <summary>
		/// <para>与 `using` 语句一起使用时，创建一个隔离的 RNG 上下文。</para>
		/// <para>退出代码块后，将恢复原有的 RNG 状态。</para>
		/// <para>这与 <see cref="With(Thing)"/> 类似，但会使用一个与给定物体
		/// 关联的特殊种子，该种子在游戏内的当前小时里保持
		/// 稳定。</para>
		/// <para>如有需要，可以从该上下文的 struct 中获取种子。只需
		/// 把上下文赋给一个变量即可。</para>
		/// </summary>
		/// <param name="thing">用作种子来源的物体。</param>
		/// <param name="salt">可加入以进一步搅乱种子的盐值。</param>
		/// <returns>一个 RNG 上下文。</returns>
		public static Seeded ForHour(Thing thing, int salt = 42) =>
			new(Verse.Rand.RandSeedForHour(thing, salt));

		/// <summary>
		/// <para>与 `using` 语句一起使用时，创建一个隔离的 RNG 上下文。</para>
		/// <para>退出代码块后，将恢复原有的 RNG 状态。</para>
		/// <para>这与 <see cref="With(Thing)"/> 类似，但会使用一个与给定物体
		/// 关联的特殊种子，该种子在游戏内的当前小时里保持
		/// 稳定。</para>
		/// <para>如有需要，可以从该上下文的 struct 中获取种子。只需
		/// 把上下文赋给一个变量即可。</para>
		/// </summary>
		/// <param name="thing">用作种子来源的物体。</param>
		/// <param name="saltSource">用于进一步搅乱种子的物体。</param>
		/// <returns>一个 RNG 上下文。</returns>
		public static Seeded ForHour(Thing thing, Thing saltSource) =>
			new(Verse.Rand.RandSeedForHour(thing, saltSource.HashOffset()));

		/// <summary>
		/// <para>与 `using` 语句一起使用时，创建一个隔离的 RNG 上下文。</para>
		/// <para>退出代码块后，将恢复原有的 RNG 状态。</para>
		/// <para>这与 <see cref="With(Thing)"/> 类似，但会使用一个随机种子，
		/// 该种子基于上一个 RNG 状态。这应当在多人游戏中保持稳定。</para>
		/// <para>如有需要，可以从该上下文的 struct 中获取种子。只需
		/// 把上下文赋给一个变量即可。</para>
		/// </summary>
		/// <returns>一个 RNG 上下文。</returns>
		public static Seeded Randomly() =>
			new(Verse.Rand.Int);
	}
}
