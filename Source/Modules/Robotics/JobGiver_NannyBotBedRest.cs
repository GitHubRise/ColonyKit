using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人受伤到一半以下、又没有人正在修时，去最近的空医疗床上休养。
	/// 找不到床就什么都不做（照常干活 / Mk-V 野外自修复），不会出现「站在床边没活干」的状态。
	/// </summary>
	public class JobGiver_NannyBotBedRest : ThinkNode_JobGiver
	{
		/// <summary>耐久低于上限的这个比例才会主动去躺床；再高就照常干活。</summary>
		public const float AutoRestDurabilityFraction = 0.5f;

		/// <summary>排在充电（9）和救援/灭火（8/7）之后、普通家务（默认 6）之前。</summary>
		private const float RestPriority = 6.5f;

		public override float GetPriority(Pawn pawn)
		{
			if (!ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return 0f;
			}
			RoboticsSettings settings = NannyBotUtility.Settings;
			if (!settings.robotBedRest || settings.robotBedRestRate <= 0f)
			{
				return 0f;
			}
			if (!NannyBotUtility.IsNannyBot(pawn) || !NannyBotUtility.CanWork(pawn))
			{
				return 0f;
			}
			if (NannyBotUtility.IsFormingCaravan(pawn))
			{
				return 0f;
			}
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp == null || comp.Durability > comp.MaxDurability * AutoRestDurabilityFraction)
			{
				return 0f;
			}
			return RestPriority;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (GetPriority(pawn) <= 0f)
			{
				return null;
			}
			Building_Bed bed = NannyBotUtility.FindBedForRest(pawn);
			if (bed == null)
			{
				// 没有空医疗床：直接返回，让思考树继续试下一个 JobGiver（家务 / 野外自修复）。
				return null;
			}
			return JobMaker.MakeJob(NannyBotDefOf.CK_RobotRestInBed, bed);
		}
	}
}
