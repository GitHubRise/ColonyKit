using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace rjw
{
	/// <summary>
	/// 根据小人的体型影响属性。
	/// </summary>
	public class StatPart_BodyType : StatPart
	{
		public enum ValueMode { Factor, Offset }

		/// <summary>
		/// 这些值是以系数还是偏移量的形式应用。
		/// </summary>
		public ValueMode mode = ValueMode.Factor;

		/// <summary>
		/// 体型到数值的字典。
		/// </summary>
		public Dictionary<BodyTypeDef, float> bodyToValue = new();

		public override void TransformValue(StatRequest req, ref float val)
		{
			if (!req.HasThing) return;
			if (req.Thing is not Pawn pawn) return;
			if (pawn.story?.bodyType is not { } bodyType) return;
			if (!bodyToValue.TryGetValue(bodyType, out var value)) return;

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
			if (pawn.story?.bodyType is not { } bodyType) return null;
			if (!bodyToValue.TryGetValue(bodyType, out var value)) return null;

			// 这些不影响数值，因此无需说明。
			if (mode == ValueMode.Factor && value == 1f) return null;
			if (mode == ValueMode.Offset && value == 0f) return null;

			var numSense = mode switch
			{
				ValueMode.Factor => ToStringNumberSense.Factor,
				ValueMode.Offset => ToStringNumberSense.Offset,
				_ => throw new InvalidOperationException("bad value for `mode`")
			};

			var label = "StatPart_BodyType".Translate();
			var text = value.ToStringByStyle(ToStringStyle.FloatMaxThree, numSense);
			return $"{label}: {text}";
		}
	}
}