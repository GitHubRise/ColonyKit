#nullable disable

using System.Diagnostics;
using System.Collections.Generic;
using Verse;

namespace rjw.Modules.Benchmark
{
#if BENCHMARK
	/// <summary>
	/// <para>BENCHMARKING ENABLED!</para>
	/// <para>A disposable ref-struct that represents a running benchmark.</para>
	/// <para>You must enable the `BENCHMARK` compiler constant to enable this.</para>
	/// <example>
	/// <para>To benchmark a method, the simplest way is to place a `using` expression
	/// at the start of the method.  The unused variable is required by this form of
	/// `using`.</para>
	/// <code>
	/// // Pull in the benchy type at the top of the C# file.
	/// using Benchy = rjw.Modules.Benchmark.Benchy;
	/// 
	/// void MethodToTest()
	/// {
	///   using var _benchy = Benchy.Watch(nameof(MethodToTest));
	///   DoNormalMethodThings();
	/// }
	/// </code>
	/// <para>When the method returns, the ref-struct is disposed, which records the
	/// run time of the function.</para>
	/// <para>A `using` block also works, if you prefer that.  Then you don't even need
	/// to define a variable, if that bugs you.</para>
	/// </example>
	/// </summary>
	public ref struct Benchy
	{
		private string methodName;
		private int warmupCalls;
		private readonly Stopwatch stopwatch;

		private Benchy(string methodName, int warmupCalls)
		{
			this.methodName = methodName;
			this.warmupCalls = warmupCalls;

			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		public void Dispose()
		{
			stopwatch.Stop();
			BenchmarkCore.AddResult(methodName, warmupCalls, stopwatch.ElapsedTicks);
		}

		/// <summary>
		/// <para>BENCHMARKING ENABLED!</para>
		/// <para>Begins timing a method from this point until disposed.</para>
		/// <para>If your method is called rarely, set `warmupCalls` lower, as needed.</para>
		/// </summary>
		/// <param name="methodName">The name of the method.</param>
		/// <param name="warmupCalls">How many calls to skip before taking samples.</param>
		/// <returns>A benchy instance, representing a benchmark context.</returns>
		public static Benchy Watch(string methodName, int warmupCalls = 10) =>
			new(methodName, warmupCalls);
	}

	public struct BenchyRecord
	{
		/// <summary>
		/// The name of the method benchmarked.
		/// </summary>
		public string methodName;
		/// <summary>
		/// <para>The total number of calls seen thus far.</para>
		/// <para>Note: the number of samples may not match this value.</para>
		/// </summary>
		public long totalCalls;
		/// <summary>
		/// The recorded sample times, in integer nanoseconds.
		/// </summary>
		public long[] samples;

		public BenchyRecord(string methodName, long totalCalls, long[] samples)
		{
			this.methodName = methodName;
			this.totalCalls = totalCalls;
			this.samples = samples;
		}
	}

	public static class BenchmarkCore
	{
		const int MAX_SAMPLES = 1000;
		static readonly long nanoSecsPerTick = 1000L * 1000L * 1000L / Stopwatch.Frequency;

		/// <summary>
		/// <para>Stores the samples of the benchmarked methods.</para>
		/// <para>The results are stored in integer nanoseconds.</para>
		/// </summary>
		static readonly Dictionary<string, Queue<long>> samples = new();

		/// <summary>
		/// <para>Stores the number of times each method was called.</para>
		/// </summary>
		static readonly Dictionary<string, long> callCounts = new();

		public static void AddResult(string methodName, int warmupCalls, long ticks)
		{
			var totalCalls = callCounts.GetWithFallback(methodName, 0L) + 1L;
			callCounts.SetOrAdd(methodName, totalCalls);

			// Let a few calls go without logging.  This warms up the jitter.
			if (totalCalls <= warmupCalls) return;

			// The call was too fast to really get a good read.  Use a minimum.
			ticks = ticks == 0L ? 1L : ticks;

			var resultStore = samples.TryGetValue(methodName, out var q) ? q : new();

			// Drop samples if we have too many.
			while (resultStore.Count >= MAX_SAMPLES)
				resultStore.Dequeue();

			resultStore.Enqueue(ticks * nanoSecsPerTick);

			samples.SetOrAdd(methodName, resultStore);
		}

		/// <summary>
		/// <para>Yields the current results of benchmarks.</para>
		/// <para>The `samples` are copied arrays, so are safe to manipulate.</para>
		/// </summary>
		/// <returns>An enumerable of benchmark records.</returns>
		public static IEnumerable<BenchyRecord> GetSamples()
		{
			foreach (var kvp in samples)
			{
				// Only bother if we have at least one sample.
				if (kvp.Value.Count == 0) continue;
				var totalCalls = callCounts.GetWithFallback(kvp.Key, 0L);
				yield return new BenchyRecord(kvp.Key, totalCalls, kvp.Value.ToArray());
			}
		}

	}
#else
	/// <summary>
	/// <para>性能基准测试已禁用！</para>
	/// <para>禁用基准测试时使用的占位 ref 结构体。</para>
	/// <para>你必须启用 `BENCHMARK` 编译器常量才能启用它。</para>
	/// <para>这样可以在代码中保留 `Benchy.Watch()` 而对性能
	/// 影响极小。</para>
	/// <para>但当你不再打算做基准测试时，应当将它们移除。</para>
	/// </summary>
	public ref struct Benchy
	{
		private Benchy(string methodName) { }
		public void Dispose() { }
		/// <summary>
		/// <para>性能基准测试已禁用！</para>
		/// <para>未定义 `BENCHMARK` 编译器常量时，它没有任何用处。</para>
		/// </summary>
		/// <param name="methodName">方法名称。</param>
		/// <param name="warmupCalls">采集样本前要跳过的调用次数。</param>
		/// <returns>一个不做任何事的新占位实例。</returns>
		public static Benchy Watch(string methodName, int warmupCalls = 10) =>
			new(methodName);
	}
#endif
}
