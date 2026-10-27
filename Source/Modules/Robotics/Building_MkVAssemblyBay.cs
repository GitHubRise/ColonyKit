using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 组装舱：只在通电组装时发光，空闲时保持熄灭。</summary>
	public class Building_MkVAssemblyBay : Building, IThingGlower
	{
		public bool ShouldBeLitNow()
		{
			CompMkVAssemblyBay comp = GetComp<CompMkVAssemblyBay>();
			return comp != null && comp.IsWorking && comp.IsPowered;
		}
	}
}
