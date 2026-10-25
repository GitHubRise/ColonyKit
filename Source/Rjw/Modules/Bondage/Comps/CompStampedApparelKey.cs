#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

// 为全息钥匙的右键菜单添加解锁选项。
namespace rjw
{
	public class CompStampedApparelKey : CompUsable
	{
		protected string make_label(Pawn pawn, Pawn other)
		{
			var text = "RJW_RMB_BG_Put_On"
				.Translate(pawn, 
					(other == null) ? "self".Translate() : other).CapitalizeFirst();
			return text;
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
		{
			if (!pawn.CanReserve(parent))
			{
				var text = "RJW_RMB_BG_Put_On".Translate(pawn, $"({"Reserved".Translate()})").CapitalizeFirst();
				yield return new FloatMenuOption(text, null, MenuOptionPriority.DisabledOption);
			}	
			else if (pawn.CanReach(parent, PathEndMode.Touch, Danger.Some))
			{
				// 供小人对自己使用钥匙的选项
				if (!pawn.is_wearing_locked_apparel())
				{
					var text = "RJW_RMB_BG_NotWearingBondage".Translate().CapitalizeFirst();
					yield return new FloatMenuOption(text, null, MenuOptionPriority.DisabledOption);
				}
				else
					yield return this.make_option(make_label(pawn, null), pawn, null, null);

				if ((pawn.Map != null) && (pawn.Map == Find.CurrentMap))
				{
					// 用于殖民者的选项
					foreach (var other in pawn.Map.mapPawns.FreeColonists)
						if ((other != pawn) && other.is_wearing_locked_apparel())
							yield return this.make_option(make_label(pawn, other), pawn, other, null);

					// 用于囚犯的选项
					foreach (var prisoner in pawn.Map.mapPawns.PrisonersOfColony)
						if (prisoner.is_wearing_locked_apparel())
							yield return this.make_option(make_label(pawn, prisoner), pawn, prisoner, WorkTypeDefOf.Warden);

					// 用于尸体的选项
					foreach (var q in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
					{
						var corpse = q as Corpse;
						if (corpse.InnerPawn.is_wearing_locked_apparel())
							yield return this.make_option(make_label(pawn, corpse.InnerPawn), pawn, corpse, null);
					}
				}
			}
		}
	}
}
