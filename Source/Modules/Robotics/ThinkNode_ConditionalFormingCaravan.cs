using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人一旦被加进正在组建的远行队，商队职责就是最高优先任务：这里把原版
	/// <c>LordDuty</c> 子树接到思考树最前面，让「搬物资 → 去集合点 → 随队出发」
	/// 压过家务、巡逻、索敌和回舱充电，避免整支远行队等一台机器人做完手头的活。
	/// </summary>
	public class ThinkNode_ConditionalFormingCaravan : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			// duty 为空时进 LordDuty 会让 ThinkNode_Duty 刷红字，这里先挡掉。
			return NannyBotUtility.IsFormingCaravan(pawn)
				&& pawn.mindState?.duty != null;
		}
	}
}
