#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

// 为束缚装备的右键菜单添加选项，以把装备穿在囚犯／倒地小人身上
namespace rjw
{
	public class CompBondageGear : CompUsable
	{
		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
		{
			if ((pawn.Map != null) && (pawn.Map == Find.CurrentMap))// && (pawn.Map.mapPawns.PrisonersOfColonyCount > 0)
			{
				if (!pawn.CanReserve(parent))
				{
					var text = "RJW_RMB_BG_Put_On".Translate(pawn, $"({"Reserverd".Translate()})").CapitalizeFirst();
					yield return new FloatMenuOption(text, null, MenuOptionPriority.DisabledOption);
				}
				else if (pawn.CanReach(parent, PathEndMode.Touch, Danger.Some))
					foreach (Pawn other in pawn.Map.mapPawns.AllPawns)
						if ((other != pawn) && other.Spawned && (other.Downed || other.IsPrisonerOfColony || xxx.is_slave(other)))
						{
							var text = "RJW_RMB_BG_Put_On".Translate(pawn, other).CapitalizeFirst();
							yield return this.make_option(text, pawn, other, (other.IsPrisonerOfColony || xxx.is_slave(other)) ? WorkTypeDefOf.Warden : null);
						}
			}
		}
	}
}
