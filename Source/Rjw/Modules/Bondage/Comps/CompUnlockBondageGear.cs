#nullable disable

﻿using RimWorld;
using Verse;

namespace rjw
{
	public class CompUnlockBondageGear : CompUseEffect
	{
		public override float OrderPriority
		{
			get
			{
				return -69;
			}
		}

		public override void DoEffect(Pawn p)
		{
			base.DoEffect(p);
			var key_stamp = parent.GetComp<CompHoloCryptoStamped>();

			if ((key_stamp == null) || (p.MapHeld == null) || (p.apparel == null))
				return;

			Apparel locked_app = null;
			var any_locked = false;
			{
				foreach (var app in p.apparel.WornApparel)
				{
					var app_stamp = app.GetComp<CompHoloCryptoStamped>();
					if (app_stamp != null)
					{
						any_locked = true;
						if (app_stamp.matches(key_stamp))
						{
							locked_app = app;
							break;
						}
					}
				}
			}

			if (locked_app != null)
			{
				//locked_app.Notify_Stripped (p); // TODO This was removed. Necessary?

				p.apparel.Remove(locked_app);
				Thing dropped = null;
				GenThing.TryDropAndSetForbidden(locked_app, p.Position, p.MapHeld, ThingPlaceMode.Near, out dropped, false); //这样会以某种方式创建一把新钥匙。
				if (dropped != null)
				{
					var text = "RJW_BG_Message_Unlock".Translate(locked_app.def.label);
					Messages.Message(text, p, MessageTypeDefOf.SilentInput);
					IntVec3 keyPostition = parent.Position;
					parent.Destroy();
				}
				else if (PawnUtility.ShouldSendNotificationAbout(p))
				{
					var text = "RJW_BG_Message_CouldntDrop".Translate(locked_app.def.label);
					Messages.Message(text, p, MessageTypeDefOf.NegativeEvent);
				}
			}
			else if (any_locked)
			{
				var text = "RJW_BG_Message_WrongKey".Translate();
				Messages.Message(text, p, MessageTypeDefOf.NegativeEvent);
			}
		}
	}
}
