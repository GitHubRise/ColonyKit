using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH Building_bath：1×2 浴缸，先注水再泡澡。</summary>
	public class Building_Bath : Building_AssignableFixture
	{
		public float cleanedPerTick = 0.001f;
		public float waterUsedPerTick = 1f;
		public float heatUsedPerTick = 0.00025f;
		public ContaminationLevel contamination = ContaminationLevel.Treated;
		public float WaterInTub;
		private bool cold = true;

		public override float waterUsed => 190f * HygieneSettings.Current.waterUseMultiplier;

		public override FixtureType fixture => FixtureType.Bath;

		public override FixtureQuality Quality => FixtureQuality.luxury;

		public bool IsCold => cold;

		public bool IsFull => WaterInTub >= waterUsed;

		public bool HasWaterSupply => pipe != null && !pipe.closed && pipe.pipeNet != null;

		public bool TryFillBath()
		{
			float amount = waterUsedPerTick * HygieneSettings.Current.waterUseMultiplier;
			if (!HasWaterSupply || !Working(amount).Accepted)
			{
				return false;
			}
			if (!pipe.pipeNet.PullWater(amount, out ContaminationLevel contam))
			{
				return false;
			}
			if (contam > contamination)
			{
				contamination = contam;
			}
			WaterInTub += amount;
			if (pipe.pipeNet.PullHotWater(heatUsedPerTick))
			{
				cold = false;
			}
			return true;
		}

		public bool TryPullPlug()
		{
			cold = true;
			WaterInTub = 0f;
			contamination = ContaminationLevel.Treated;
			return true;
		}

		public bool CanUse(Pawn pawn)
		{
			return Spawned
				&& PawnAllowed(pawn)
				&& !Occupied(pawn)
				&& pawn.CanReserve(this)
				&& pawn.CanReach(this, PathEndMode.Touch, Danger.Deadly)
				&& HasWaterSupply
				// 一缸需要 waterUsed（190 L）；不够就别开始放水。
				&& Working().Accepted;
		}
	}
}
