using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 雨水桶（DBH Building_Butt 的移植）：不接管网，下雨自动接水（容量 100），
	/// 只要有 1 L 水就能当洗手池用。
	/// </summary>
	public class Building_Butt : Building_AssignableFixture
	{
		public override bool CanSetGender => false;

		public override FixtureType fixture => FixtureType.Basin;

		public override FixtureQuality Quality => FixtureQuality.normal;

		public override AcceptanceReport Working(float waterUsed = 0f)
		{
			CompWaterStorage storage = GetComp<CompWaterStorage>();
			return storage != null && storage.WaterStorage > 1f;
		}
	}
}
