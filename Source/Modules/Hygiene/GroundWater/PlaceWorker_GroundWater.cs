using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene.GroundWater
{
	/// <summary>
	/// 水井只能建在地下水脉上（移植 DBH 的 <c>PlaceWorker_WaterGrid</c>）。
	/// 太空/海冰这类整张图都没有地下水的图不拦，交给低温采水节点或地表水。
	/// </summary>
	public class PlaceWorker_GroundWater : PlaceWorker
	{
		protected virtual bool Deep => false;

		public override AcceptanceReport AllowsPlacing(
			BuildableDef def,
			IntVec3 loc,
			Rot4 rot,
			Map map,
			Thing thingToIgnore = null,
			Thing thing = null)
		{
			if (!HygieneSettings.Current.groundWaterRequired)
			{
				return true;
			}
			MapComponent_GroundWater groundWater = map?.GetComponent<MapComponent_GroundWater>();
			if (groundWater == null || !groundWater.HasAnyWater(Deep))
			{
				return true;
			}
			if (groundWater.ValueAt(loc, Deep) > 0.5f)
			{
				return true;
			}
			return "CK.Hygiene.WellNeedsWater".Translate();
		}

		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, UnityEngine.Color ghostCol, Thing thing = null)
		{
			Find.CurrentMap?.GetComponent<MapComponent_GroundWater>()?.MarkForDraw(Deep);
		}
	}

	/// <summary>深水井：只看深层水脉，范围更大。</summary>
	public class PlaceWorker_DeepGroundWater : PlaceWorker_GroundWater
	{
		protected override bool Deep => true;
	}
}
