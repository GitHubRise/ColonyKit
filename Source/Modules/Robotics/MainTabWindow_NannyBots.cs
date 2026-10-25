using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>底部「机器人」页：列出本图保姆机器人、电量、耐久、任务与充电桩。</summary>
	public class MainTabWindow_NannyBots : MainTabWindow
	{
		private const float RowHeight = 64f;

		private Vector2 scroll;

		public override Vector2 RequestedTabSize => new Vector2(880f, 640f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "CK.RobotManagement.Title".Translate());
			Widgets.Label(new Rect(0f, 30f, inRect.width, 24f), "CK.RobotManagement.Hint".Translate());

			List<Pawn> robots = new List<Pawn>();
			Map map = Find.CurrentMap;
			if (map != null)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (NannyBotUtility.IsNannyBot(pawn))
					{
						robots.Add(pawn);
					}
				}
			}

			Rect outRect = new Rect(0f, 62f, inRect.width, inRect.height - 62f);
			float viewHeight = Mathf.Max(outRect.height, robots.Count * RowHeight + 8f);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, viewHeight);

			Widgets.BeginScrollView(outRect, ref scroll, viewRect);
			if (robots.Count == 0)
			{
				Widgets.Label(new Rect(0f, 0f, viewRect.width, 24f), "CK.RobotManagement.None".Translate());
			}
			else
			{
				float y = 0f;
				foreach (Pawn robot in robots)
				{
					DrawRow(new Rect(0f, y, viewRect.width, RowHeight - 6f), robot);
					y += RowHeight;
				}
			}
			Widgets.EndScrollView();
		}

		private void DrawRow(Rect row, Pawn robot)
		{
			Widgets.DrawHighlightIfMouseover(row);
			CompNannyBot comp = robot.TryGetComp<CompNannyBot>();
			Need_RobotEnergy energy = NannyBotUtility.Energy(robot);

			string status = Status(robot, comp);
			string job = robot.CurJobDef != null
				? robot.CurJobDef.label
				: "CK.RobotManagement.IdleJob".Translate();
			string station = comp?.homeStation != null
				? comp.homeStation.LabelShort
				: "CK.RobotManagement.NoStation".Translate();

			string text = "CK.RobotManagement.Row".Translate(
				robot.LabelShort,
				energy == null ? "-" : energy.CurLevel.ToString("0"),
				energy == null ? "-" : energy.MaxLevel.ToString("0"),
				comp == null ? "-" : comp.Durability.ToString("0"),
				CompNannyBot.MaxDurability.ToString("0"),
				job,
				station,
				status);
			Widgets.Label(row.LeftPart(0.72f), text);

			if (Widgets.ButtonText(new Rect(row.x + row.width * 0.73f, row.y, 90f, 28f),
				"CK.RobotManagement.Select".Translate()))
			{
				CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(robot));
			}
			if (comp?.homeStation != null
				&& Widgets.ButtonText(new Rect(row.x + row.width * 0.73f, row.y + 30f, 90f, 28f),
					"CK.RobotManagement.Station".Translate()))
			{
				CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(comp.homeStation));
			}
		}

		private static string Status(Pawn robot, CompNannyBot comp)
		{
			if (robot.Downed)
			{
				return "CK.RobotManagement.Status.Downed".Translate();
			}
			if (comp != null && comp.paused)
			{
				return "CK.RobotManagement.Status.Paused".Translate();
			}
			Need_RobotEnergy energy = NannyBotUtility.Energy(robot);
			if (energy != null && energy.IsCharging)
			{
				return "CK.RobotManagement.Status.Charging".Translate();
			}
			if (energy != null && energy.CurLevelPercentage <= 0.001f)
			{
				return "CK.RobotManagement.Status.Empty".Translate();
			}
			return "CK.RobotManagement.Status.Working".Translate();
		}
	}
}
