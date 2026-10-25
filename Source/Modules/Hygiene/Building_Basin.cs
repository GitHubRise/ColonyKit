using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH Building_basin：接水网的洗手池。</summary>
	public class Building_Basin : Building_AssignableFixture
	{
		public float waterUsedPerTick = 0.04f;
		public float HeatUsedPerTick = 0.00025f;

		public override float waterUsed => 1f;

		public override FixtureType fixture => FixtureType.Basin;

		public override FixtureQuality Quality => FixtureQuality.normal;

		public bool TryUseWater(out ContaminationLevel contam)
		{
			contam = ContaminationLevel.Treated;
			if (pipe?.pipeNet == null)
			{
				return true;
			}
			if (!pipe.pipeNet.PullWater(waterUsedPerTick, out contam))
			{
				return false;
			}
			pipe.pipeNet.PullHotWater(HeatUsedPerTick);
			return true;
		}

		public bool CanUse(Pawn pawn)
		{
			return Spawned
				&& PawnAllowed(pawn)
				&& !Occupied(pawn)
				&& pawn.CanReserve(this)
				&& pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly);
		}
	}
}
