#nullable disable

﻿using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 小人发情或欲求不满
	/// </summary>
	public class ThinkNode_ConditionalHornyOrFrustrated : ThinkNode_Conditional
	{
		protected override bool Satisfied (Pawn p)
		{
			return xxx.is_hornyorfrustrated(p);
		}
	}
}
