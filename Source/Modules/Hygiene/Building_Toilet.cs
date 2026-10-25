using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class Building_Toilet : Building_AssignableFixture
	{
		public override float waterUsed => 14f;

		public override FixtureType fixture => FixtureType.Toilet;

		public override FixtureQuality Quality => FixtureQuality.normal;

		public bool TryUseFlush()
		{
			if (pipe?.pipeNet == null)
			{
				return true;
			}
			pipe.pipeNet.PushSewage(14f);
			return pipe.pipeNet.PullWater(waterUsed, out _);
		}

		public bool CanUse(Pawn pawn)
		{
			return Spawned
				&& PawnAllowed(pawn)
				&& !Occupied(pawn)
				&& pawn.CanReserve(this)
				&& pawn.CanReach(this, PathEndMode.OnCell, Danger.Deadly);
		}
	}
}
