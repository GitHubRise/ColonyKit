#nullable disable

﻿using System.Linq;
using RimWorld;
using Verse;

namespace rjw
{
	public class MapCom_Injector : MapComponent
	{
		public bool injected_designator = false;

		public bool triggered_after_load = false;

		public MapCom_Injector(Map m) : base(m)
		{
		}

		public override void MapComponentUpdate()
		{
		}

		public override void MapComponentTick()
		{
			RMB_Invite.TickVerification();
		}

		public override void MapComponentOnGUI()
		{
			var currently_visible = Find.CurrentMap == map;

			if ((!injected_designator) && currently_visible)
			{
				//Find.ReverseDesignatorDatabase.AllDesignators.Add(new Designator_ComfortPrisoner());
				//Find.ReverseDesignatorDatabase.AllDesignators.Add(new Designator_Breed());
				injected_designator = true;
			}
			else if (injected_designator && (!currently_visible))
				injected_designator = false;
		}

		public override void ExposeData()
		{
		}

		public override void FinalizeInit()
		{
			//ModLog.Message("FixRjwHediffsOnLoad in MapCom_Injector");
			FixRjwHediffsOnLoad();
		}
		
		private void FixRjwHediffsOnLoad()
		{
			foreach (var pawn in PawnsFinder.All_AliveOrDead)
			{
				//ModLog.Message($"FixRjwHediffsOnLoad for {pawn}");
				foreach (var part in pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				{
					//ModLog.Message($"FixRjwHediffsOnLoad for {pawn}, {part}");
					part.GetPartComp()?.updatepartposition();
				}
			}
		}
	}
}
