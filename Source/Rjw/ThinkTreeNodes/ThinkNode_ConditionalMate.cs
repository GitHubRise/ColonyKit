#nullable disable

using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 用于判断该动物能否与其他动物交配（原版繁殖性行为）。
	/// </summary>
	public class ThinkNode_ConditionalMate : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			if (xxx.is_animal(p) && RJWSettings.animal_on_animal_enabled)
			{
				if (!RJWSettings.wild_mating_enabled && p.Faction != Faction.OfPlayer)
				{
					return false;
				}
				return true;
			}
			return false;
		}
	}
}
