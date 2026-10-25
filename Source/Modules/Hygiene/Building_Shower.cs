using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH Building_shower 的淋浴常量和取水逻辑移植。</summary>
	public class Building_Shower : Building_AssignableFixture
	{
		public virtual float CleanedPerTick => 0.002f;

		public virtual float WaterUsedPerTick => 0.13f;

		public override float waterUsed => 50f;

		public virtual float HeatUsedPerTick => 0.00025f;

		public override FixtureType fixture => FixtureType.Bath;

		public bool IsHot => powerComp != null && powerComp.PowerOn;

		public bool CanUse(Pawn pawn)
		{
			return Spawned
				&& PawnAllowed(pawn)
				&& !Occupied(pawn)
				&& pawn.CanReserve(this)
				&& pawn.CanReach(this, PathEndMode.Touch, Danger.Deadly);
		}

		public bool TryUseWater(out bool cold, out ContaminationLevel contam)
		{
			cold = true;
			contam = ContaminationLevel.Treated;
			if (pipe?.pipeNet != null && !pipe.pipeNet.PullWater(WaterUsedPerTick, out contam))
			{
				return false;
			}
			if (powerComp != null && powerComp.PowerOn)
			{
				cold = false;
			}
			else if (pipe?.pipeNet != null && pipe.pipeNet.PullHotWater(HeatUsedPerTick))
			{
				cold = false;
			}
			if (pipe == null)
			{
				cold = false;
			}
			return true;
		}

		public bool TryUseWater()
		{
			return TryUseWater(out _, out _);
		}

		public float WaterAvailable => pipe?.pipeNet?.WaterStorage ?? 0f;

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			text += IsHot
				? "CK.Hygiene.HotWater".Translate()
				: "CK.Hygiene.ColdWater".Translate();
			text += "\n" + "CK.Hygiene.ShowerWater".Translate(WaterAvailable.ToString("F0"));
			return text;
		}
	}
}
