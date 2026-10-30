using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>免费取水点只能放在河流、浅水等水面地形上；陆地不放行。</summary>
	public class PlaceWorker_WaterSpot : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(
			BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			TerrainDef terrain = loc.GetTerrain(map);
			if (terrain == null || !terrain.IsWater)
			{
				return "CK.Hygiene.WaterSpot.NeedWater".Translate();
			}
			return true;
		}
	}
}
