using ColonyKit.Modules.Hygiene.Dbh;
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
			CompPipe waterPipe = pipe;
			CompPipe sewagePipe = null;
			foreach (CompPipe candidate in GetComps<CompPipe>())
			{
				if (candidate.mode == PipeType.Sewage)
				{
					sewagePipe = candidate;
					break;
				}
			}
			if (sewagePipe?.pipeNet != null)
			{
				sewagePipe.pipeNet.PushSewage(14f);
			}
			return waterPipe?.pipeNet == null
				|| waterPipe.pipeNet.PullWater(waterUsed, out _);
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
