using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 所有可排优先级的家务 JobGiver 的共同父类。
	/// 优先级数字 1–4（1 最高），0 为关闭；设置页总开关也会把有效优先级压成 0。
	/// </summary>
	public abstract class JobGiver_NannyBotChore : ThinkNode_JobGiver
	{
		protected abstract RobotChore Chore { get; }

		public override float GetPriority(Pawn pawn)
		{
			if (!ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return 0f;
			}
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp == null || comp.paused || comp.repairing)
			{
				return 0f;
			}
			int priority = comp.GetEffectivePriority(Chore);
			return priority <= 0 ? 0f : 5f - priority;
		}

		protected bool HasChorePriority(Pawn pawn)
		{
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			return comp != null && comp.GetEffectivePriority(Chore) > 0;
		}

		protected bool CanDoChore(Pawn pawn)
		{
			return HasChorePriority(pawn) && NannyBotUtility.CanWork(pawn);
		}
	}
}
