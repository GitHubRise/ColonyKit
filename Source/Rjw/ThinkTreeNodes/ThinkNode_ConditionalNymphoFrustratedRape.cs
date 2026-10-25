#nullable disable

﻿using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 花痴在欲求不满时是否会强暴？
	/// </summary>
	public class ThinkNode_ConditionalNymphoFrustratedRape : ThinkNode_Conditional
	{
		protected override bool Satisfied (Pawn p)
		{
			return RJWSettings.NymphFrustratedRape;
		}
	}
}
