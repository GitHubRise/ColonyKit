using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH Building_basin：接水网的洗手池。</summary>
	public class Building_Basin : Building_AssignableFixture
	{
		public float waterUsedPerTick = 0.04f;
		public float HeatUsedPerTick = 0.00025f;

		public override float waterUsed => 4f * HygieneSettings.Current.waterUseMultiplier;

		public override FixtureType fixture => FixtureType.Basin;

		public override FixtureQuality Quality => FixtureQuality.normal;

		public virtual bool TryUseWater(out ContaminationLevel contam)
		{
			contam = ContaminationLevel.Treated;
			float amount = waterUsedPerTick * HygieneSettings.Current.waterUseMultiplier;
			if (!HasWaterSupply || !Working(amount).Accepted)
			{
				return false;
			}
			if (!pipe.pipeNet.PullWater(amount, out contam))
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
				&& pawn.CanReach(this, InteractionPathMode, Danger.Deadly)
				&& HasWaterSupply
				&& Working().Accepted;
		}

		protected override void Tick()
		{
			base.Tick();
			// 有水的洗手池每 120 tick 在交互格滴一下水花；限频，别每 tick 生成。
			if (!HasWaterSupply || !this.IsHashIntervalTick(120))
			{
				return;
			}
			IntVec3 cell = InteractionCell;
			if (!cell.IsValid || !cell.InBounds(Map))
			{
				return;
			}
			FleckDef splashes = DefDatabase<FleckDef>.GetNamedSilentFail("CK_MoteWaterSplashes");
			if (splashes == null)
			{
				return;
			}
			Vector3 loc = cell.ToVector3Shifted();
			loc.y += 0.1f;
			if (!loc.ShouldSpawnMotesAt(Map))
			{
				return;
			}
			FleckMaker.Static(loc, Map, splashes, Rand.Range(0.5f, 0.8f));
		}
	}
}
