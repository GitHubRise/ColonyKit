using ColonyKit.Modules.Hygiene.Dbh;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_FreeWaterInlet : CompProperties_WaterInlet
	{
		public CompProperties_FreeWaterInlet()
		{
			compClass = typeof(CompFreeWaterInlet);
		}
	}

	/// <summary>免费取水点的进水口：只作为「可打水」的标记，不显示「地下水能力」。</summary>
	public class CompFreeWaterInlet : CompWaterInlet
	{
		public override string CompInspectStringExtra()
		{
			return null;
		}
	}

	public class CompProperties_SurfaceWaterInlet : CompProperties_WaterInlet
	{
		public CompProperties_SurfaceWaterInlet()
		{
			compClass = typeof(CompSurfaceWaterInlet);
		}
	}

	/// <summary>
	/// 水龙头的水上进水口：只有建在水面（TerrainDef.IsWater）时才给同网提供
	/// <see cref="CompWaterInlet.GroundWaterCapacity"/>，建在陆地上为 0，
	/// 不会影响原有水井/水泵逻辑。这样水龙头放在河里，同网水泵就能把地表水
	/// 通过水管抽到远处的水箱。
	/// </summary>
	public class CompSurfaceWaterInlet : CompWaterInlet
	{
		public bool OnWater =>
			parent.Spawned
			&& parent.Map != null
			&& parent.Position.GetTerrain(parent.Map).IsWater;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			UpdateCapacity();
		}

		public override void CompTick()
		{
			base.CompTick();
			if (parent.IsHashIntervalTick(250))
			{
				UpdateCapacity();
			}
		}

		private void UpdateCapacity()
		{
			GroundWaterCapacity = OnWater ? Props.Capacity : 0f;
		}

		public override string CompInspectStringExtra()
		{
			// 陆地上的水龙头不显示“地下水 0”，避免误解。
			return GroundWaterCapacity > 0.0001f ? base.CompInspectStringExtra() : null;
		}
	}
}
