#nullable disable

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace rjw
{
	/// <summary>
	/// 性行为进行中显示在指令栏的「邀请加入」按钮。
	/// 不依赖英雄模式/右键菜单开关，避免右键入口被设置挡住时完全看不到功能。
	/// </summary>
	public class Gizmo_InviteJoin : Gizmo
	{
		private readonly Pawn pawn;

		public Gizmo_InviteJoin(Pawn pawn)
		{
			this.pawn = pawn;
		}

		public override float GetWidth(float maxWidth) => 150f;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Widgets.DrawWindowBackground(rect);

			Rect buttonRect = rect.ContractedBy(6f);
			buttonRect.height = 30f;
			buttonRect.y += 2f;

			if (Widgets.ButtonText(buttonRect, "RJW_InviteJoin".Translate()))
			{
				List<FloatMenuOption> options = RMB_Invite.GetMenuOptions(pawn).ToList();
				if (options.Count == 0)
				{
					RMB_Invite.LogDiagnostics(pawn);

					if (!RMB_Invite.TryGetReceiver(pawn, out _))
					{
						Messages.Message("RJW_InviteJoin_NoReceiver".Translate(), MessageTypeDefOf.RejectInput, false);
					}
					else
					{
						RMB_Invite.DescribeFailures(pawn, out int nearby, out int noRoom, out int notInterested, out int other);
						Messages.Message(
							"RJW_InviteJoin_NoneDetail".Translate(nearby, noRoom, notInterested, other),
							MessageTypeDefOf.RejectInput,
							false);

						if (RJWSettings.DevMode)
							ModLog.Message($"[InviteJoin] no options: nearby={nearby}, noRoom={noRoom}, notInterested={notInterested}, other={other}");
					}
				}
				else
				{
					Find.WindowStack.Add(new FloatMenu(options));
				}
			}

			Rect descRect = new Rect(buttonRect.x, buttonRect.yMax + 3f, buttonRect.width, 28f);
			GameFont previousFont = Text.Font;
			Text.Font = GameFont.Tiny;
			Widgets.Label(descRect, "RJW_InviteJoin_Desc".Translate());
			Text.Font = previousFont;

			return new GizmoResult(GizmoState.Clear);
		}
	}
}
