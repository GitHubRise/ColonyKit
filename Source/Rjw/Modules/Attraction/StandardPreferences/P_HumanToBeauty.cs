#nullable disable

using Verse;
using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理目标的美貌属性。
	/// </summary>
	public sealed class P_HumanToBeauty : CurveAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 不适用于动物观察者。
			if (request.Category.AnimalObserver()) return;

			request.SetPreference(new P_HumanToBeauty());
		}

		/// <summary>
		/// 曲线的点，将小人的美貌属性映射为要施加的系数。
		/// </summary>
		private static readonly CurvePoint[] beautyPoints = new CurvePoint[]
		{
			new(-2f, 0.5f),
			new(-1f, 0.8f),
			new(0f, 1f),
			new(1f, 1.1f),
			new(2f, 1.25f)
		};

		private P_HumanToBeauty() : base(
			AttractionMode.Physical,
			nameof(P_HumanToBeauty),
			GetBeautyStat,
			beautyPoints)
		{ }

		private static float GetBeautyStat(ref AttractionRequest request) =>
			request.Target.GetStatValue(StatDefOf.PawnBeauty);
	}
}
