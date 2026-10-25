using System;
using RimWorld;
using Verse;
using static rjw.Genital_Helper;
using static rjw.GenderHelper;

namespace rjw
{
	/// <summary>
	/// <para>根据小人是否只能被属性请求中的另一方
	/// 插入来影响属性。</para>
	/// <para>这需要用 `pawn.GetStatValueForPawn` 获取属性，
	/// 以便提供另一方小人。</para>
	/// <para>虽然有一个说明部分，但我认为在正常情况下它
	/// 永远无法显示在信息面板中，
	/// 因为此功能要生效就需要另一方小人，而
	/// 信息面板没有另一方小人可提供。</para>
	/// </summary>
	public sealed class StatPart_WhenOnlyPenetrable : StatPart
	{
		public enum ValueMode { Factor, Offset }

		/// <summary>
		/// 这些值是以系数还是偏移量的形式应用。
		/// </summary>
		public ValueMode mode = ValueMode.Factor;

		/// <summary>
		/// 仅在可被插入时使用的数值。
		/// </summary>
		public float value = 0f;

		public override void TransformValue(StatRequest req, ref float val)
		{
			if (!req.HasThing) return;
			if (req.Thing is not Pawn pawn) return;
			if (req.Pawn is not Pawn otherPawn) return;
			if (!IsOnlyPenetrable(pawn, otherPawn)) return;

			val = mode switch
			{
				ValueMode.Factor => val * value,
				ValueMode.Offset => val + value,
				_ => throw new InvalidOperationException("bad value for `mode`")
			};
		}

		public override string? ExplanationPart(StatRequest req)
		{
			if (!req.HasThing) return null;
			if (req.Thing is not Pawn pawn) return null;
			if (req.Pawn is not Pawn otherPawn) return null;
			if (!IsOnlyPenetrable(pawn, otherPawn)) return null;

			// 这些不影响数值，因此无需说明。
			if (mode == ValueMode.Factor && value == 1f) return null;
			if (mode == ValueMode.Offset && value == 0f) return null;

			var numSense = mode switch
			{
				ValueMode.Factor => ToStringNumberSense.Factor,
				ValueMode.Offset => ToStringNumberSense.Offset,
				_ => throw new InvalidOperationException("bad value for `mode`")
			};

			var label = "StatPart_WhenOnlyPenetrable".Translate(otherPawn.LabelIndefinite());
			var text = value.ToStringByStyle(ToStringStyle.FloatMaxThree, numSense);
			return $"{label}: {text}";
		}

		private static bool IsOnlyPenetrable(Pawn pawn, Pawn otherPawn) =>
			(GetSex(otherPawn), GetSex(pawn)) switch
			{
				(Sex.Male or Sex.Trap or Sex.Futa, Sex.Female) => true,
				(Sex.Male or Sex.Trap or Sex.Futa, Sex.None) when has_anus(pawn) => true,
				_ => false
			};
	}
}