#nullable disable

using System.Linq;
using RimWorld;
using Verse;

namespace rjw
{
	/// <summary>
	/// <para>将计算出的数值以其倒数形式呈现的属性工作者。</para>
	/// <para>也就是说，若计算值为 `2`，则会输出 `1/2`。</para>
	/// <para>最适合用于最小值为 `1` 的属性。</para>
	/// <para>不支持负值。若数值最终
	/// 为零或更小，则会用该属性的最大值作为其倒数。</para>
	/// </summary>
	public class StatWorker_Inverse : StatWorker
	{
		public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
		{
			base.FinalizeValue(req, ref val, applyPostProcess);
			val = DoInvert(val);
		}

		public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
		{
			// 真希望他们直接用生成器 yield 出这些部位……
			var baseText = base.GetExplanationFinalizePart(req, numberSense, finalVal);
			var oldLines = baseText.Split('\n');
			
			// 有点绕：`finalVal` 已经是取过倒数的值，所以我们必须把它还原
			// 才能得到取倒数之前的值。
			var preInvertText = stat.ValueToString(DoInvert(finalVal), stat.toStringNumberSense, true);
			var postInvertText = stat.ValueToString(finalVal, stat.toStringNumberSense, true);
			var label = "StatWorker_Invert".Translate(preInvertText);

			var newLines = oldLines.Take(-1)
				.Append($"{label}: {postInvertText}")
				.Concat(oldLines.TakeLast(1));
			return string.Join("\n", newLines);
		}

		private float DoInvert(float val)
		{
			if (val >= stat.maxValue) return 0f;
			if (val <= 0f) return stat.maxValue;
			return 1f / val;
		}
	}
}
