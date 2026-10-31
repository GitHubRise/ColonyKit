using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_Fridge : CompProperties
	{
		public CompProperties_Fridge()
		{
			compClass = typeof(CompFridge);
		}
	}

	/// <summary>
	/// 自带制冷的冰箱：接电并打开开关时，放在冰箱格上的食物停止腐烂
	/// （<see cref="Patch_CompRottable_Fridge"/> 会跳过 CompRottable 的腐烂 tick）。
	/// </summary>
	public class CompFridge : ThingComp
	{
		private CompPowerTrader power;
		private CompFlickable flicker;

		public bool IsRefrigerated
		{
			get
			{
				if (power == null || !power.PowerOn)
				{
					return false;
				}
				return flicker == null || flicker.SwitchIsOn;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			power = parent.GetComp<CompPowerTrader>();
			flicker = parent.GetComp<CompFlickable>();
		}

		public override string CompInspectStringExtra()
		{
			if (power == null)
			{
				return null;
			}
			if (!power.PowerOn)
			{
				return "CK.Hygiene.Fridge.NoPower".Translate();
			}
			if (flicker != null && !flicker.SwitchIsOn)
			{
				return "CK.Hygiene.Fridge.Off".Translate();
			}
			return "CK.Hygiene.Fridge.On".Translate();
		}
	}
}
