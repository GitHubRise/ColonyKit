#nullable disable

using System.Collections.Generic;
using Verse;
using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理体型偏好。这由一对
	/// 属性控制，它们会被其他各种系统修改。</para>
	/// <para>查看 XML 数据以了解哪些因素会影响它们。</para>
	/// </summary>
	public sealed class P_BodySize : CurveAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 对具有特殊强暴机制的小人忽略。我们姑且认为
			// 它们的解剖结构允许它们承受任意体型的小人。
			if (xxx.is_mechanoid(request.Pawn)) return;
			if (xxx.is_insect(request.Pawn)) return;

			var points = GetPoints(request.Pawn, request.Target);
			request.SetPreference(new P_BodySize(points));
		}

		private P_BodySize(IEnumerable<CurvePoint> points) : base(
			AttractionMode.Physical,
			nameof(P_BodySize),
			GetRelativeBodySize,
			points)
		{ }

		private static IEnumerable<CurvePoint> GetPoints(Pawn pawn, Pawn target)
		{
			var min = pawn.GetStatValueForPawn(RJWStatDefOf.SexMinSizePreference, target);
			var max = pawn.GetStatValueForPawn(RJWStatDefOf.SexMaxSizePreference, target);

			yield return new(min * 0.5f, 0.4f);
			yield return new(min, 1f);
			yield return new(max, 1f);
			yield return new(max * 2f, 0.4f);
		}

		private static float GetRelativeBodySize(ref AttractionRequest request)
		{
			var pSize = request.Pawn.BodySize;
			var tSize = request.Target.BodySize;

			// 体重必须合理。
			if (pSize <= 0f || tSize <= 0f) return 1f;

			// 低于 1.0 的值表示目标比观察者更轻。
			return tSize / pSize;
		}
	}
}
