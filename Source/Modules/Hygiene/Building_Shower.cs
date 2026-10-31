using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH Building_shower 的淋浴常量和取水逻辑移植。</summary>
	public class Building_Shower : Building_AssignableFixture
	{
		public virtual float CleanedPerTick => 0.002f;

		public virtual float WaterUsedPerTick => 0.13f;

		public override float waterUsed => 50f * HygieneSettings.Current.waterUseMultiplier;

		public virtual float HeatUsedPerTick => 0.00025f;

		public override FixtureType fixture => FixtureType.Bath;

		/// <summary>Simple showers may run cold without power; the power shower overrides this.</summary>
		public override bool PowerRequiredToUse => false;

		public bool IsHot =>
			(powerComp != null && powerComp.PowerOn)
			|| (pipe?.pipeNet?.HasHotWater == true);

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
			float amount = WaterUsedPerTick * HygieneSettings.Current.waterUseMultiplier;
			if (!HasWaterSupply || !Working(amount).Accepted)
			{
				return false;
			}
			if (!pipe.pipeNet.PullWater(amount, out contam))
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
			ThrowWashSteam();
			return true;
		}

		/// <summary>
		/// 洗澡时每 30 tick 在花洒位置冒一团蒸汽（DBH Mote_WashSteam）。
		/// 淋浴器的 tickerType 是 Rare，没有每 tick 的 Tick()，所以挂在
		/// JobDriver_TakeShower 每 tick 调用的 TryUseWater 上，和 DBH 一样；
		/// occupiedSinceTick 由 Job 的 init/finishAction 维护。
		/// </summary>
		private void ThrowWashSteam()
		{
			if (!Spawned || occupiedSinceTick < 0 || !this.IsHashIntervalTick(30))
			{
				return;
			}
			FleckDef steam = DefDatabase<FleckDef>.GetNamedSilentFail("CK_MoteWashSteam");
			if (steam == null)
			{
				return;
			}
			Vector3 loc = Position.ToVector3Shifted();
			loc += new Vector3(Rand.Range(-0.2f, 0.2f), 0f, Rand.Range(-0.2f, 0.2f));
			if (!loc.ShouldSpawnMotesAt(Map))
			{
				return;
			}
			FleckCreationData data = FleckMaker.GetDataStatic(loc, Map, steam, 1.2f);
			data.velocityAngle = 0f;
			data.velocitySpeed = Rand.Range(0.1f, 0.15f);
			Map.flecks.CreateFleck(data);
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
