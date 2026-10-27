using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>机器人的「角色」页：恢复型号、阵营、耐久、电量、武器、战斗与任务信息，并按两列对齐。</summary>
	public class ITab_Pawn_RobotInfo : ITab
	{
		public ITab_Pawn_RobotInfo()
		{
			labelKey = "CK.Robotics.Tab.RobotInfo";
			tutorTag = "RobotInfo";
		}

		public override bool IsVisible =>
			SelPawn != null && NannyBotUtility.IsNannyBot(SelPawn);

		protected override void UpdateSize()
		{
			size = new Vector2(460f, 350f);
		}

		protected override void FillTab()
		{
			Pawn pawn = SelPawn;
			if (pawn == null)
			{
				return;
			}

			CompNannyBot comp = pawn.TryGetComp<CompNannyBot>();
			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			CompMkVCombat combat = pawn.TryGetComp<CompMkVCombat>();

			Text.Font = GameFont.Small;
			float y = 18f;
			DrawRow(ref y, "CK.Robotics.RobotInfo.Name".Translate(), pawn.LabelShort);
			DrawRow(ref y, "CK.Robotics.RobotInfo.Kind".Translate(), pawn.kindDef?.LabelCap ?? pawn.def.LabelCap);
			DrawRow(ref y, "CK.Robotics.RobotInfo.Age".Translate(),
				pawn.ageTracker == null ? "-" : pawn.ageTracker.AgeBiologicalYears.ToString());
			DrawRow(ref y, "CK.Robotics.RobotInfo.BirthYear".Translate(),
				pawn.ageTracker == null ? "-" : pawn.ageTracker.BirthYear.ToString());
			DrawRow(ref y, "CK.Robotics.RobotInfo.Faction".Translate(), pawn.Faction?.Name ?? "-");
			DrawRow(ref y, "CK.Robotics.RobotInfo.Durability".Translate(),
				(comp?.Durability ?? 0f).ToString("0") + " / "
				+ (comp?.MaxDurability ?? CompNannyBot.DefaultMaxDurability).ToString("0"));
			DrawRow(ref y, "CK.Robotics.RobotInfo.Energy".Translate(),
				energy == null ? "-" : energy.CurLevel.ToString("0") + " / " + energy.MaxLevel.ToString("0"));
			DrawRow(ref y, "CK.Robotics.RobotInfo.Weapon".Translate(),
				pawn.equipment?.Primary?.LabelCap ?? "CK.Robotics.MkVCombat.NoWeapon".Translate());
			if (NannyBotUtility.IsMkV(pawn))
			{
				DrawRow(ref y, "CK.Robotics.RobotInfo.Melee".Translate(), "CK.Robotics.ArcBlade".Translate());
			}
			if (combat != null)
			{
				DrawRow(ref y, "CK.Robotics.RobotInfo.Combat".Translate(),
					combat.state == MkVCombatState.Engaged
						? "CK.Robotics.MkVCombat.Status.Engaged".Translate()
						: combat.state == MkVCombatState.Returning
							? "CK.Robotics.MkVCombat.Status.Returning".Translate()
							: "CK.Robotics.MkVCombat.Status.Idle".Translate());
			}
			DrawRow(ref y, "CK.Robotics.RobotInfo.Task".Translate(), NannyBotJobUtility.TaskText(pawn));
			Text.Anchor = TextAnchor.UpperLeft;
		}

		private void DrawRow(ref float y, string label, string value)
		{
			Rect labelRect = new Rect(18f, y, 150f, 28f);
			Rect valueRect = new Rect(174f, y, size.x - 192f, 28f);
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(labelRect, label);
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(valueRect, value);
			y += 30f;
		}
	}
}
