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

		/// <summary>Simple showers may run cold without power; the power shower overrides this.</summary>
		public override bool PowerRequiredToUse => false;

		public bool IsHot =>
			(powerComp != null && powerComp.PowerOn)
			|| (pipe?.pipeNet?.HasHotWater == true);

		/// <summary>淋浴器必须接在同网有水的水管上；没有水网时不能使用。</summary>
		public bool HasWaterSupply => pipe?.pipeNet != null;

		public bool CanUse(Pawn pawn)
		{
			return Spawned
				&& PawnAllowed(pawn)
				&& !Occupied(pawn)
				&& pawn.CanReserve(this)
				&& pawn.CanReach(this, PathEndMode.Touch, Danger.Deadly)
				&& HasWaterSupply
				// Working() 同时检查堵塞、供电要求和水箱存量；否则断电/
				// 堵塞/电热水淋浴断电也会被选为目标，走到一半才失败。
				&& Working().Accepted;
		}

		public bool TryUseWater(out bool cold, out ContaminationLevel contam)
		{
			cold = true;
			contam = ContaminationLevel.Treated;
			if (!HasWaterSupply || !Working(WaterUsedPerTick).Accepted)
			{
				return false;
			}
			if (!pipe.pipeNet.PullWater(WaterUsedPerTick, out contam))
			{
				return false;
			}
			if (powerComp != null && powerComp.PowerOn)
			{
				cold = false;
			}
			else if (pipe.pipeNet.PullHotWater(HeatUsedPerTick))
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
