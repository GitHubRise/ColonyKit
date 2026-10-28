using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class WorkGiver_EmptySepticTank : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_SepticTank"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t?.TryGetComp<CompSewageHandler>() is CompSewageHandler comp))
			{
				return false;
			}
			// 带 DBH 排水设置的池子（污水处理池/可处理化粪池）按自己的阈值判断：
			// 非强制时要 AllowDrain 且超过 PcntToEmpty 才清（DBH WorkGiver_emptySepticTank 的行为）。
			if (!forced && comp is CompSepticTank septic && !septic.EmptyNow)
			{
				return false;
			}
			// 原来只要超过 1 L 就开工：每冲一次水都会派一趟活，化粪池的缓冲容量
			// 永远用不上，Blocked/溢出也永远见不到。非强制时等到 80% 容量再抽；
			// 强制时留 10 L 下限，避免点一下只抽掉几升。
			float threshold = forced ? 10f : Mathf.Max(1f, comp.Props.capacity * 0.8f);
			if (comp.sewageBuffer < threshold)
			{
				return false;
			}
			// 和 DBH 一样补上可达性、燃烧与拆除检查：原来的判断只有
			// CanReserve + IsForbidden，小人会被派去够不着的池子，然后反复失败。
			if (t.IsForbidden(pawn)
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			return !t.IsBurning();
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(
				DefDatabase<JobDef>.GetNamedSilentFail("CK_EmptySepticTank"), t);
		}
	}
}
