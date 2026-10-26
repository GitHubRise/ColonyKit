using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 底部「机器人」页：工作管控 + 机器人状态两个页签。
	/// 工作管控模仿原版 Work 页：每行一台机器人，每列一项家务，数字 1–8 为优先级，0 为关闭。
	/// </summary>
	public class MainTabWindow_NannyBots : MainTabWindow
	{
		private const float TabHeight = 34f;
		private const float HintHeight = 22f;
		private const float HeaderHeight = 50f;
		private const float NameWidth = 190f;
		private const float ChoreWidth = 64f;
		private const float RowHeight = 32f;

		private readonly List<Pawn> robots = new List<Pawn>();

		private int tab;
		private Vector2 workScroll;
		private Vector2 statusScroll;

		public override Vector2 RequestedTabSize => new Vector2(980f, 700f);

		public void SelectTab(int value)
		{
			tab = Mathf.Clamp(value, 0, 1);
		}

		public override void DoWindowContents(Rect inRect)
		{
			robots.Clear();
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

			DrawTabs(new Rect(0f, 0f, inRect.width, TabHeight));
			Rect content = new Rect(
				0f,
				TabHeight + 4f,
				inRect.width,
				inRect.height - TabHeight - 4f);
			if (tab == 0)
			{
				DrawWorkTab(content);
			}
			else
			{
				DrawStatusTab(content);
			}
		}

		private void DrawTabs(Rect rect)
		{
			string[] labels =
			{
				"CK.RobotManagement.Tab.Work".Translate(),
				"CK.RobotManagement.Tab.Status".Translate()
			};
			float x = 0f;
			for (int i = 0; i < labels.Length; i++)
			{
				Rect tabRect = new Rect(x, rect.y, 130f, TabHeight);
				Widgets.DrawOptionBackground(tabRect, tab == i);
				if (Widgets.ButtonInvisible(tabRect, true))
				{
					tab = i;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
				using (new TextBlock(TextAnchor.MiddleCenter))
				{
					Widgets.Label(tabRect, labels[i]);
				}
				x += tabRect.width + 4f;
			}
		}

		private void DrawWorkTab(Rect rect)
		{
			Widgets.Label(
				new Rect(0f, rect.y, rect.width, HintHeight),
				"CK.RobotManagement.WorkHint".Translate());
			float headerY = rect.y + HintHeight + 2f;
			Rect header = new Rect(0f, headerY, rect.width, HeaderHeight);
			DrawWorkHeader(header);

			Rect outRect = new Rect(
				0f,
				headerY + HeaderHeight + 2f,
				rect.width,
				rect.height - HintHeight - HeaderHeight - 4f);
			float viewWidth = NameWidth + RobotChores.Count * ChoreWidth;
			float viewHeight = Mathf.Max(
				outRect.height,
				8f + robots.Count * RowHeight);
			Rect viewRect = new Rect(0f, 0f, Mathf.Max(viewWidth, outRect.width - 20f), viewHeight);

			Widgets.BeginScrollView(outRect, ref workScroll, viewRect);
			if (robots.Count == 0)
			{
				Widgets.Label(new Rect(0f, 0f, viewRect.width, 24f),
					"CK.RobotManagement.None".Translate());
			}
			else
			{
				float y = 4f;
				for (int i = 0; i < robots.Count; i++)
				{
					DrawWorkRow(new Rect(0f, y, viewRect.width, RowHeight), robots[i], i);
					y += RowHeight;
				}
			}
			Widgets.EndScrollView();
		}

		private void DrawWorkHeader(Rect rect)
		{
			Widgets.DrawBoxSolid(rect, new Color(0.16f, 0.16f, 0.16f, 0.85f));
			using (new TextBlock(TextAnchor.MiddleLeft))
			{
				Widgets.Label(
					new Rect(8f, rect.y, NameWidth - 12f, rect.height),
					"CK.RobotManagement.NameColumn".Translate());
			}

			for (int i = 0; i < RobotChores.Count; i++)
			{
				RobotChore chore = (RobotChore)i;
				Rect cell = new Rect(
					NameWidth + i * ChoreWidth,
					rect.y,
					ChoreWidth,
					rect.height);
				bool allOn = AllHavePriority(chore);
				bool newValue = allOn;
				Rect checkbox = new Rect(cell.x + 4f, cell.y + 3f, 18f, 18f);
				Widgets.Checkbox(checkbox.position, ref newValue, 18f);
				if (newValue != allOn)
				{
					SetAllPriorities(chore, newValue ? 3 : 0);
				}

				using (new TextBlock(TextAnchor.MiddleCenter))
				{
					Widgets.Label(
						new Rect(cell.x, cell.y + 22f, cell.width, 24f),
						RobotChores.LabelKey(chore).Translate());
				}
				TooltipHandler.TipRegion(
					cell,
					RobotChores.DescKey(chore).Translate()
						+ "\n\n"
						+ "CK.RobotManagement.WorkHint".Translate());
			}
		}

		private void DrawWorkRow(Rect row, Pawn robot, int rowIndex)
		{
			if (rowIndex % 2 == 1)
			{
				Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
			}
			Widgets.DrawHighlightIfMouseover(row);

			Rect nameRect = new Rect(row.x, row.y, NameWidth, row.height);
			if (Widgets.ButtonInvisible(nameRect))
			{
				CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(robot));
			}
			using (new TextBlock(TextAnchor.MiddleLeft))
			{
				Widgets.Label(nameRect.ContractedBy(8f, 0f), robot.LabelShort);
			}
			TooltipHandler.TipRegion(nameRect, "CK.RobotManagement.Select".Translate());

			CompNannyBot comp = NannyBotUtility.BotComp(robot);
			if (comp == null)
			{
				return;
			}

			for (int i = 0; i < RobotChores.Count; i++)
			{
				RobotChore chore = (RobotChore)i;
				Rect cell = new Rect(
					NameWidth + i * ChoreWidth + (ChoreWidth - 25f) / 2f,
					row.y + 3f,
					25f,
					25f);
				DrawPriorityCell(cell, robot, comp, chore);
			}
		}

		private static void DrawPriorityCell(
			Rect cell,
			Pawn robot,
			CompNannyBot comp,
			RobotChore chore)
		{
			int priority = comp.GetPriority(chore);
			bool globallyEnabled = RobotChores.IsEnabledBySettings(chore);
			Widgets.DrawBoxSolid(cell, new Color(0.1f, 0.1f, 0.1f, 0.85f));
			Widgets.DrawBox(cell);

			if (priority > 0)
			{
				using (new TextBlock(TextAnchor.MiddleCenter, globallyEnabled
					? PriorityColor(priority)
					: Color.grey))
				{
					Text.Font = GameFont.Medium;
					Widgets.Label(cell, priority.ToStringCached());
				}
			}
			else
			{
				using (new TextBlock(TextAnchor.MiddleCenter, Color.grey))
				{
					Widgets.Label(cell, "×");
				}
			}

			if (Event.current.type == EventType.MouseDown && Mouse.IsOver(cell))
			{
				int next = Event.current.button == 0 ? priority - 1 : priority + 1;
				if (next < 0)
				{
					next = 8;
				}
				if (next > 8)
				{
					next = 0;
				}
				comp.SetPriority(chore, next);
				SoundDefOf.DragSlider.PlayOneShotOnCamera();
				Event.current.Use();
			}

			TooltipHandler.TipRegion(
				cell,
				robot.LabelShort
					+ "\n"
					+ RobotChores.LabelKey(chore).Translate()
					+ ": "
					+ priority
					+ (globallyEnabled ? string.Empty : "\n" + "CK.RobotManagement.DisabledBySettings".Translate()));
		}

		private static Color PriorityColor(int priority)
		{
			switch (priority)
			{
				case 1:
					return new Color(0.15f, 1f, 0.25f);
				case 2:
					return new Color(0.55f, 1f, 0.30f);
				case 3:
					return new Color(0.85f, 1f, 0.40f);
				case 4:
					return new Color(1f, 0.92f, 0.45f);
				case 5:
					return new Color(1f, 0.75f, 0.35f);
				case 6:
					return new Color(0.95f, 0.58f, 0.40f);
				case 7:
					return new Color(0.82f, 0.60f, 0.55f);
				case 8:
					return new Color(0.68f, 0.68f, 0.68f);
				default:
					return Color.grey;
			}
		}

		private bool AllHavePriority(RobotChore chore)
		{
			if (robots.Count == 0)
			{
				return false;
			}
			foreach (Pawn robot in robots)
			{
				CompNannyBot comp = NannyBotUtility.BotComp(robot);
				if (comp == null || comp.GetPriority(chore) <= 0)
				{
					return false;
				}
			}
			return true;
		}

		private void SetAllPriorities(RobotChore chore, int priority)
		{
			foreach (Pawn robot in robots)
			{
				NannyBotUtility.BotComp(robot)?.SetPriority(chore, priority);
			}
		}

		private void DrawStatusTab(Rect rect)
		{
			Widgets.Label(
				new Rect(0f, rect.y, rect.width, HintHeight),
				"CK.RobotManagement.Hint".Translate());
			float top = rect.y + HintHeight + 2f;
			Rect outRect = new Rect(0f, top, rect.width, rect.height - HintHeight - 2f);
			float viewHeight = Mathf.Max(outRect.height, robots.Count * 64f + 8f);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, viewHeight);

			Widgets.BeginScrollView(outRect, ref statusScroll, viewRect);
			if (robots.Count == 0)
			{
				Widgets.Label(new Rect(0f, 0f, viewRect.width, 24f),
					"CK.RobotManagement.None".Translate());
			}
			else
			{
				float y = 0f;
				foreach (Pawn robot in robots)
				{
					DrawStatusRow(new Rect(0f, y, viewRect.width, 58f), robot);
					y += 64f;
				}
			}
			Widgets.EndScrollView();
		}

		private void DrawStatusRow(Rect row, Pawn robot)
		{
			Widgets.DrawHighlightIfMouseover(row);
			CompNannyBot comp = robot.TryGetComp<CompNannyBot>();
			Need_RobotEnergy energy = NannyBotUtility.Energy(robot);

			string status = Status(robot, comp);
			string job = robot.CurJobDef != null
				? (robot.CurJobDef.label.NullOrEmpty()
					? robot.CurJobDef.defName
					: robot.CurJobDef.label)
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

			if (Widgets.ButtonText(
				new Rect(row.x + row.width * 0.73f, row.y, 90f, 26f),
				"CK.RobotManagement.Select".Translate()))
			{
				CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(robot));
			}
			if (comp?.homeStation != null
				&& Widgets.ButtonText(
					new Rect(row.x + row.width * 0.73f, row.y + 28f, 90f, 26f),
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
			if (comp != null && comp.repairing)
			{
				return "CK.RobotManagement.Status.Repairing".Translate();
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
