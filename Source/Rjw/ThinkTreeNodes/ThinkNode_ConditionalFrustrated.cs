#nullable disable

﻿using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 小人欲求不满
	/// </summary>
	public class ThinkNode_ConditionalFrustrated : ThinkNode_Conditional
	{
		protected override bool Satisfied (Pawn p)
		{
			return xxx.is_frustrated(p);
		}
	}
}
