using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>围观洗衣机（DBH JobDriver_WatchWashingMachine 的移植）：断电就结束。</summary>
	public class JobDriver_WatchWashingMachine : JobDriver_WatchBuilding
	{
		protected override void WatchTickAction(int delta)
		{
			Building building = TargetA.Thing as Building;
			CompPowerTrader power = building?.TryGetComp<CompPowerTrader>();
			if (power != null && !power.PowerOn)
			{
				EndJobWith(JobCondition.Incompletable);
				return;
			}
			base.WatchTickAction(delta);
		}
	}
}
