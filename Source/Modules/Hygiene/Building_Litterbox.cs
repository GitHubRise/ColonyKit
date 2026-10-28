using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 猫砂盆（DBH Building_Litterbox 的移植）：室内小动物用的便盆，不需要接水管/排污，
	/// 永远「可用」；动物会走到这里解决膀胱需求。
	/// </summary>
	public class Building_Litterbox : Building_AssignableFixture
	{
		public override FixtureType fixture => FixtureType.Litter;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		public override AcceptanceReport Working(float waterUsed = 0f)
		{
			return true;
		}
	}
}
