using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>电弧弹：命中时生成青色电弧冲击效果。</summary>
	public class Projectile_CKArcBolt : Bullet
	{
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			Map map = Map;
			IntVec3 position = Position;
			base.Impact(hitThing, blockedByShield);

			if (map == null || !position.IsValid || !position.InBounds(map))
			{
				return;
			}
			ThingDef mote = DefDatabase<ThingDef>.GetNamedSilentFail("CK_Mote_ArcImpact");
			if (mote != null)
			{
				MoteMaker.MakeStaticMote(position, map, mote, 1.36f);
			}
		}
	}
}
