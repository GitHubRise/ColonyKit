using System.Collections.Generic;
using System.Linq;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 选中殖民者后右键洁具/水井时的指令菜单：
	/// 马桶→使用，洗手池→洗手，淋浴器→淋浴，浴缸/双人浴缸→泡澡，水井→打水；
	/// 另有清理堵塞（CompBlockage）和清洁附近污渍。
	/// </summary>
	public class FloatMenuOptionProvider_HygieneFixtures : FloatMenuOptionProvider
	{
		protected override bool Drafted => false;

		protected override bool Undrafted => true;

		protected override bool Multiselect => false;

		protected override bool RequiresManipulation => true;

		public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
		{
			Pawn pawn = context.FirstSelectedPawn;
			if (pawn == null
				|| !pawn.IsColonistPlayerControlled
				|| !(clickedThing is Building building)
				|| building.Map != pawn.Map
				|| building.Faction != Faction.OfPlayer)
			{
				yield break;
			}

			// 使用类：各设备自己的 CanUse 已经包含供水、堵塞、占用、可达与预约检查。
			if (clickedThing is Building_Toilet toilet)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.UseToilet", pawn, toilet, "CK_UseToilet",
					toilet.CanUse(pawn), RejectReason(toilet));
			}
			else if (clickedThing is Building_Basin basin)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.WashHands", pawn, basin, "CK_WashHands",
					basin.CanUse(pawn), RejectReason(basin));
			}
			else if (clickedThing is Building_Shower shower)
			{
				yield return UseOption("CK.Hygiene.FloatMenu.Shower", pawn, shower, "CK_TakeShower",
					shower.CanUse(pawn), RejectReason(shower));
			}
			else if (clickedThing is Building_Bath bath)
			{
				// 双人浴缸也用同一个泡澡 Job；想邀请别人由 JobGiver_HaveWash 自动掷骰。
				yield return UseOption("CK.Hygiene.FloatMenu.Bath", pawn, bath, "CK_TakeBath",
					bath.CanUse(pawn), RejectReason(bath));
			}

			// 清理堵塞：复用机器人「疏通洁具」Job（JobDriver_NannyUnblock 只依赖 CompBlockage）。
			CompBlockage blockage = building.TryGetComp<CompBlockage>();
			if (blockage != null && blockage.blocked)
			{
				bool canUnblock = pawn.CanReserveAndReach(building, PathEndMode.Touch, pawn.NormalMaxDanger());
				yield return UseOption("CK.Hygiene.FloatMenu.Unblock", pawn, building, "CK_NannyUnblock",
					canUnblock, canUnblock ? null : "CannotReach".Translate().ToString());
			}

			// 清洁：洁具周围一格内最近的污渍，走原版 Clean Job。
			Filth filth = FindFilth(building);
			if (filth != null && pawn.CanReserveAndReach(filth, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				yield return new FloatMenuOption(
					"CK.Hygiene.FloatMenu.Clean".Translate(),
					() =>
					{
						Job job = JobMaker.MakeJob(JobDefOf.Clean);
						job.AddQueuedTarget(TargetIndex.A, filth);
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					},
					MenuOptionPriority.Default,
					null,
					filth);
			}

			// 打水：往同一张水网的水箱里手动加水（没有水箱/水箱全满时不给）。
			if (building.TryGetComp<CompWaterInlet>() != null)
			{
				CompPipe pipe = building.GetComp<CompPipe>();
				PlumbingNet net = pipe?.pipeNet;
				bool canDraw = net != null && net.WaterTowers.Any(t => t != null && t.WorkingNow && t.space > 1f);
				yield return UseOption("CK.Hygiene.FloatMenu.DrawWater", pawn, building, "CK_DrawWater",
					canDraw,
					canDraw ? null : "CK.Hygiene.FloatMenu.NoWaterTank".Translate().ToString());
			}
		}

		private static FloatMenuOption UseOption(
			string labelKey, Pawn pawn, Thing target, string jobDefName, bool canDo, string reason)
		{
			string label = labelKey.Translate();
			if (!canDo)
			{
				return new FloatMenuOption(
					label + " " + "CK.Hygiene.FloatMenu.Disabled".Translate(reason ?? labelKey.Translate()),
					null);
			}
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail(jobDefName);
			if (def == null)
			{
				return new FloatMenuOption(label + " " + "CK.Hygiene.FloatMenu.Disabled".Translate(jobDefName), null);
			}
			return new FloatMenuOption(
				label,
				() =>
				{
					Job job = JobMaker.MakeJob(def, target);
					pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				},
				MenuOptionPriority.Default,
				null,
				target);
		}

		private static string RejectReason(Building_AssignableFixture fixture)
		{
			if (fixture.blockageComp != null && fixture.blockageComp.blocked)
			{
				return "CK.Hygiene.Blocked".Translate().ToString();
			}
			if (!fixture.HasWaterSupply)
			{
				return "CK.Hygiene.NoWaterSupply".Translate().ToString();
			}
			AcceptanceReport report = fixture.Working();
			if (!report.Accepted)
			{
				return report.Reason;
			}
			if (!fixture.Spawned)
			{
				return "CK.Hygiene.FloatMenu.CannotUse".Translate().ToString();
			}
			return "CK.Hygiene.FloatMenu.CannotUse".Translate().ToString();
		}

		private static Filth FindFilth(Building building)
		{
			Map map = building.Map;
			if (map == null)
			{
				return null;
			}
			foreach (IntVec3 cell in building.OccupiedRect().ExpandedBy(1))
			{
				if (!cell.InBounds(map))
				{
					continue;
				}
				foreach (Thing thing in cell.GetThingList(map))
				{
					if (thing is Filth filth && !filth.Destroyed && filth.Map == map)
					{
						return filth;
					}
				}
			}
			return null;
		}
	}
}
