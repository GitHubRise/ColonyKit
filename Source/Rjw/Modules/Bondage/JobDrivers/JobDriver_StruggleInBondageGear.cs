#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	public class JobDriver_StruggleInBondageGear : JobDriver
	{
		public Apparel target_gear
		{
			get
			{
				return (Apparel)TargetA.Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return this.pawn.Reserve(this.target_gear, this.job, 1, -1, null, errorOnFailed);
		}


		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return new Toil
			{
				initAction = delegate
				{
					pawn.pather.StopDead();
				},
				defaultCompleteMode = ToilCompleteMode.Delay,
				defaultDuration = 60
			};
			yield return new Toil
			{
				initAction = delegate
				{
					if (PawnUtility.ShouldSendNotificationAbout(pawn))
					{
						var text = "RJW_BG_Message_FailedRemove".Translate(pawn, target_gear.def.label).CapitalizeFirst();
						Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
