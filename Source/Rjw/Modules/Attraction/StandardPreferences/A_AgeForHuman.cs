#nullable disable

using System.Collections.Generic;
using Verse;
using rjw.Modules.Shared.Helpers;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理非动物目标的年龄吸引力。
	/// </summary>
	public sealed class A_AgeForHuman : CurveAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 动物观察者没有可与人类比较的年龄。
			if (request.Category is not AttractionCategory.BetweenHumans) return;
			// 机器人也没有。
			if (request.Pawn.IsUnsexyRobot()) return;

			// 这些曲线针对的是目标的性别。
			var sourceCurve = request.Target.gender switch
			{
				Gender.Male => AgeConfigDef.Instance.attractivenessByAgeMale,
				// 也可能是 `Gender.None`，但无所谓。
				_ => AgeConfigDef.Instance.attractivenessByAgeFemale
			};

			request.SetPreference(new A_AgeForHuman(sourceCurve));
		}

		private A_AgeForHuman(IEnumerable<CurvePoint> points) : base(
			AttractionMode.Age,
			nameof(A_AgeForHuman),
			GetScaledAge,
			points)
		{ }

		private static float GetScaledAge(ref AttractionRequest request) =>
			AgeHelper.ScaleToHumanAge(request.Target);
	}
}
