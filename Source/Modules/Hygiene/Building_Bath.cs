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

		public override void ExposeData()
		{
			base.ExposeData();
			// 之前没有存水/水温，泡澡途中读档会变成空缸。
			Scribe_Values.Look(ref WaterInTub, "WaterInTub", 0f);
			Scribe_Values.Look(ref contamination, "contamination", ContaminationLevel.Treated);
			Scribe_Values.Look(ref cold, "cold", true);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			text += "CK.Hygiene.BathWater".Translate(WaterInTub.ToString("F0"), waterUsed.ToString("F0"));
			text += "\n" + (cold ? "CK.Hygiene.BathCold".Translate() : "CK.Hygiene.BathHot".Translate());
			return text;
		}

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
			cold = !pipe.pipeNet.PullHotWater(heatUsedPerTick);
			return true;
		}

		public bool TryPullPlug()
		{
			cold = true;
			WaterInTub = 0f;
			contamination = ContaminationLevel.Treated;
			return true;
		}

		public virtual bool CanUse(Pawn pawn)
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
