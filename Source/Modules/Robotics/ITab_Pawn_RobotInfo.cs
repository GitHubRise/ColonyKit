using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>机器人的「角色」页：显示型号、阵营、耐久、电量、武器和当前状态。</summary>
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
			size = new Vector2(430f, 330f);
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

			string text = "CK.Robotics.RobotInfo.Name".Translate(pawn.LabelShort) + "\n";
			text += "CK.Robotics.RobotInfo.Kind".Translate(pawn.kindDef?.LabelCap ?? pawn.def.LabelCap) + "\n";
			text += "CK.Robotics.RobotInfo.Faction".Translate(pawn.Faction?.Name ?? "-") + "\n";
			text += "CK.Robotics.RobotInfo.Durability".Translate(
				(comp?.Durability ?? 0f).ToString("0"),
				(comp?.MaxDurability ?? CompNannyBot.DefaultMaxDurability).ToString("0")) + "\n";
			text += "CK.Robotics.RobotInfo.Energy".Translate(
				energy == null ? "-" : energy.CurLevel.ToString("0") + " / " + energy.MaxLevel.ToString("0")) + "\n";
			text += "CK.Robotics.RobotInfo.Weapon".Translate(
				pawn.equipment?.Primary?.LabelCap ?? "CK.Robotics.MkVCombat.NoWeapon".Translate()) + "\n";
			if (NannyBotUtility.IsMkV(pawn))
			{
				text += "CK.Robotics.RobotInfo.Melee".Translate(
					"CK.Robotics.ArcBlade".Translate()) + "\n";
			}
			if (combat != null)
			{
				text += "CK.Robotics.RobotInfo.Combat".Translate(
					combat.state == MkVCombatState.Engaged
						? "CK.Robotics.MkVCombat.Status.Engaged".Translate()
						: combat.state == MkVCombatState.Returning
							? "CK.Robotics.MkVCombat.Status.Returning".Translate()
							: "CK.Robotics.MkVCombat.Status.Idle".Translate()) + "\n";
			}
			text += "CK.Robotics.RobotInfo.Task".Translate(NannyBotJobUtility.TaskText(pawn));

			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(12f, 12f, size.x - 24f, size.y - 24f), text);
		}
	}
}
