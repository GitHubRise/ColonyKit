using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>2×2 淋浴器；有电为热水澡，断电为冷水澡。首版不接水网。</summary>
	public class Building_Shower : Building
	{
		private CompPowerTrader power;

		public bool IsHot => power != null && power.PowerOn;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			power = GetComp<CompPowerTrader>();
		}

		public bool CanUse(Pawn pawn)
		{
			return Spawned
				&& pawn.CanReserve(this)
				&& pawn.CanReach(this, PathEndMode.Touch, Danger.Deadly);
		}

		public bool TryUseWater()
		{
			// 水网暂时是可选增强：有水就消耗，没有水也不会阻止洗澡。
			if (WaterNetwork.HasInfrastructure(this))
			{
				WaterNetwork.TryConsume(this, 0.05f);
			}
			return true;
		}

		public float WaterAvailable => WaterNetwork.TotalWater(this);

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
