using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>机器人的「概况」页：按原版概览样式列出关键能力。</summary>
	public class ITab_Pawn_RobotInfo : ITab
	{
		private static readonly (string key, PawnCapacityDef capacity)[] Rows =
		{
			("CK.Robotics.Overview.DataProcessing", PawnCapacityDefOf.Consciousness),
			("CK.Robotics.Overview.Moving", PawnCapacityDefOf.Moving),
			("CK.Robotics.Overview.Manipulation", PawnCapacityDefOf.Manipulation),
			("CK.Robotics.Overview.Sight", PawnCapacityDefOf.Sight),
			("CK.Robotics.Overview.Hearing", PawnCapacityDefOf.Hearing),
			("CK.Robotics.Overview.Filtration", PawnCapacityDefOf.BloodFiltration),
			("CK.Robotics.Overview.Power", PawnCapacityDefOf.BloodPumping)
		};

		public ITab_Pawn_RobotInfo()
		{
			labelKey = "CK.Robotics.Tab.RobotInfo";
			tutorTag = "RobotInfo";
		}

		public override bool IsVisible =>
			SelPawn != null && NannyBotUtility.IsNannyBot(SelPawn);

		protected override void UpdateSize()
		{
			size = new Vector2(280f, 420f);
		}

		protected override void FillTab()
		{
			Pawn pawn = SelPawn;
			if (pawn?.health?.capacities == null)
			{
				return;
			}

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;

			float y = 18f;
			for (int i = 0; i < Rows.Length; i++)
			{
				PawnCapacityDef capacity = Rows[i].capacity;
				if (capacity == null)
				{
					continue;
				}
				float level = Mathf.Clamp01(pawn.health.capacities.GetLevel(capacity));
				Rect row = new Rect(18f, y, size.x - 36f, 30f);
				Widgets.Label(row, Rows[i].key.Translate());
				Text.Anchor = TextAnchor.MiddleRight;
				Widgets.Label(row, level.ToStringPercent());
				Text.Anchor = TextAnchor.MiddleLeft;
				y += 34f;
			}

			Text.Anchor = TextAnchor.UpperLeft;
		}
	}
}
