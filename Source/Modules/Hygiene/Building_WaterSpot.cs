using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 免费取水点（蓝色地贴，类似屠宰点）：无需材料、无需研究，放下去就能直接喝水/洗手/打水。
	/// 不接水网、不存水，视为一处无限的地表水源；水桶可以直接在这里打水。
	/// </summary>
	public class Building_WaterSpot : Building_Basin
	{
		public override bool CanSetGender => false;

		public override bool ShowFixtureGizmos => false;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		public override float waterUsed => 2f * HygieneSettings.Current.waterUseMultiplier;

		public override PathEndMode InteractionPathMode => PathEndMode.Touch;

		/// <summary>免费取水点不需要水网：永远可用。</summary>
		public override bool HasWaterSupply => true;

		public override AcceptanceReport Working(float waterUsed = 0f)
		{
			return true;
		}

		/// <summary>无限地表水：喝/洗手不消耗，水桶打水由 CompWaterInlet 按井水处理。</summary>
		public override bool TryUseWater(out ContaminationLevel contam)
		{
			contam = ContaminationLevel.Untreated;
			return true;
		}
	}
}
