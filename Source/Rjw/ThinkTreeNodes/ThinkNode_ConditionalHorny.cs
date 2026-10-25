#nullable disable

﻿using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 小人发情
	/// </summary>
	public class ThinkNode_ConditionalHorny : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			return xxx.is_horny(p);
		}
	}
}
