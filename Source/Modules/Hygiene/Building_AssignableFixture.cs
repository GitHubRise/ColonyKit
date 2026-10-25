using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public enum FixtureType
	{
		Undefined,
		Basin,
		Bath,
		Toilet,
		Litter
	}

	public enum FixtureQuality
	{
		shabby,
		normal,
		fine,
		luxury
	}

	/// <summary>DBH Building_AssignableFixture 的淋浴子集：水/电/可用性检查。</summary>
	public class Building_AssignableFixture : Building
	{
		public CompPipe pipe;
		public CompPowerTrader powerComp;

		public virtual float waterUsed => 0f;

		public virtual FixtureType fixture => FixtureType.Undefined;

		public virtual FixtureQuality Quality => FixtureQuality.normal;

		public virtual bool NeedsSewage => false;

		public virtual bool ForPrisoners => Position.IsInPrisonCell(Map);

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			pipe = GetComp<CompPipe>();
			powerComp = GetComp<CompPowerTrader>();
		}

		public virtual AcceptanceReport Working(float WaterUsed = 0f)
		{
			if (WaterUsed == 0f)
			{
				WaterUsed = waterUsed;
			}
			if (powerComp != null && !powerComp.PowerOn)
			{
				return "NoPower".Translate();
			}
			if (pipe == null)
			{
				return true;
			}
			if (pipe.pipeNet == null)
			{
				return "Nowater".Translate();
			}
			if (WaterUsed != 0f && pipe.pipeNet.WaterStorage <= WaterUsed)
			{
				return "Nowater".Translate();
			}
			return true;
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if (!(Working().Accepted))
			{
				text += "\n" + Working().Reason;
			}
			return text;
		}
	}
}
