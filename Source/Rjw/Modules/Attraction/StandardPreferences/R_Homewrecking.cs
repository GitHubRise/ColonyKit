#nullable disable

using UnityEngine;
using Verse;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理目标的恋爱关系，阻止侵犯已确立伴侣的
	/// 性互动。
	/// </summary>
	public sealed class R_Homewrecking : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 被某些设置禁用。
			if (RJWSettings.WildMode || RJWSettings.HippieMode) return;
			// 仅对随意性行为有效。
			if (request.Purpose is not AttractionPurpose.ForFucking) return;
			// 目前不适用于动物或类动物的人。
			if (request.Pawn.AnimalOrWildMan()) return;
			if (request.Target.AnimalOrWildMan()) return;
			// 精神变态不在意恋爱关系的细微之处。
			if (xxx.is_psychopath(request.Pawn)) return;

			request.SetPreference(new R_Homewrecking());
		}

		private R_Homewrecking() : base(
			AttractionMode.Romantic,
			nameof(R_Homewrecking),
			FactorOperation.Multiply,
			WouldBeHomewrecking,
			whenTrue: WhenHomewrecking,
			whenFalse: FixedValue(1f))
		{ }

		private static bool WouldBeHomewrecking(ref AttractionRequest request) =>
			!request.HasLoveRelationWithTarget && xxx.HasNonPolyPartner(request.Target);

		private static float WhenHomewrecking(ref AttractionRequest request, float factor)
		{
			var pawn = request.Pawn;
			var target = request.Target;

			// 花痴有特殊规则。
			if (RJWHookupSettings.NymphosCanHomewreck)
				if (xxx.is_nympho(pawn) && xxx.is_frustrated(pawn))
					return 1f;
			if (RJWHookupSettings.NymphosCanHomewreckReverse)
				if (xxx.is_nympho(target) && xxx.is_frustrated(target))
					return 1f;

			// 如果两个小人都喝醉了，我们采用醉得最轻者的严重度。
			if (pawn.HasBeerGoggles(out var pSev) && target.HasBeerGoggles(out var tSev))
				return Mathf.Min(pSev, tSev);

			// 否则，他们是社会中受人尊敬的成员，会就此退出。
			return 0f;
		}
	}
}
