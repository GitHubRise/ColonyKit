using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 智能马桶（DBH Building_AdvToilet 的移植）：用水减半（7 L/次）、品质 fine、不会被堵。
	/// 「不会堵」在 CK 里靠 def 不挂 CompBlockage 实现。
	/// </summary>
	public class Building_AdvToilet : Building_Toilet
	{
		public override float waterUsed => 7f * HygieneSettings.Current.waterUseMultiplier;

		public override FixtureQuality Quality => FixtureQuality.fine;

		protected override string SeatUpTexPath => "Things/Building/Hygiene/CK_SmartToiletUp";
	}
}
