#nullable disable

﻿using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 通过整棵思考树后再对随意性行为限流
	/// </summary>
	public class ThinkNode_ConditionalThrottle : ThinkNode_Conditional
	{
		protected override bool Satisfied (Pawn p)
		{
			if (SexUtility.ReadyForHookup(p))
			{
				SexUtility.IncreaseTicksToNextHookup(p);
			}
			return false;
		}
	}
}
