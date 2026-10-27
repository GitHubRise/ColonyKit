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

		private const float StatusHeaderHeight = 30f;
		private const float StatusRowHeight = 38f;
		private const float StatusNameWidth = 150f;
		private const float StatusStateWidth = 80f;
		private const float StatusTaskWidth = 196f;
		private const float StatusBarWidth = 112f;
		private const float StatusStationWidth = 132f;
		private const float StatusActionWidth = 120f;

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

			float headerY = rect.y + HintHeight + 2f;
			DrawStatusHeader(new Rect(0f, headerY, rect.width, StatusHeaderHeight));

			Rect outRect = new Rect(
				0f,
				headerY + StatusHeaderHeight + 2f,
				rect.width,
				rect.height - HintHeight - StatusHeaderHeight - 4f);
			float viewHeight = Mathf.Max(outRect.height, 6f + robots.Count * StatusRowHeight);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, viewHeight);

			Widgets.BeginScrollView(outRect, ref statusScroll, viewRect);
			if (robots.Count == 0)
			{
				Widgets.Label(new Rect(0f, 4f, viewRect.width, 24f),
					"CK.RobotManagement.None".Translate());
			}
			else
			{
				float y = 2f;
				for (int i = 0; i < robots.Count; i++)
				{
					DrawStatusRow(new Rect(0f, y, viewRect.width, StatusRowHeight), robots[i], i);
					y += StatusRowHeight;
				}
			}
			Widgets.EndScrollView();
		}

		private void DrawStatusHeader(Rect rect)
		{
			Widgets.DrawBoxSolid(rect, new Color(0.16f, 0.16f, 0.16f, 0.9f));

			float x = 0f;
			DrawStatusHeaderCell(
				new Rect(x, rect.y, StatusNameWidth, rect.height),
				"CK.RobotManagement.Column.Robot",
				TextAnchor.MiddleLeft);
			x += StatusNameWidth;
			DrawStatusColumnLine(x, rect);

			DrawStatusHeaderCell(
				new Rect(x, rect.y, StatusStateWidth, rect.height),
				"CK.RobotManagement.Column.Status",
				TextAnchor.MiddleCenter);
			x += StatusStateWidth;
			DrawStatusColumnLine(x, rect);

			DrawStatusHeaderCell(
				new Rect(x, rect.y, StatusTaskWidth, rect.height),
				"CK.RobotManagement.Column.Task",
				TextAnchor.MiddleLeft);
			x += StatusTaskWidth;
			DrawStatusColumnLine(x, rect);

			DrawStatusHeaderCell(
				new Rect(x, rect.y, StatusBarWidth, rect.height),
				"CK.RobotManagement.Column.Battery",
				TextAnchor.MiddleCenter);
			x += StatusBarWidth;
			DrawStatusColumnLine(x, rect);

			DrawStatusHeaderCell(
				new Rect(x, rect.y, StatusBarWidth, rect.height),
				"CK.RobotManagement.Column.Durability",
				TextAnchor.MiddleCenter);
			x += StatusBarWidth;
			DrawStatusColumnLine(x, rect);

			DrawStatusHeaderCell(
				new Rect(x, rect.y, StatusStationWidth, rect.height),
				"CK.RobotManagement.Column.Station",
				TextAnchor.MiddleLeft);
			x += StatusStationWidth;
			DrawStatusColumnLine(x, rect);

			DrawStatusHeaderCell(
				new Rect(x, rect.y, StatusActionWidth, rect.height),
				"CK.RobotManagement.Column.Actions",
				TextAnchor.MiddleCenter);
		}

		private static void DrawStatusHeaderCell(Rect rect, string key, TextAnchor anchor)
		{
			using (new TextBlock(anchor))
			{
				Widgets.Label(rect.ContractedBy(6f, 0f), key.Translate());
			}
		}

		private static void DrawStatusColumnLine(float x, Rect rect)
		{
			Widgets.DrawBoxSolid(
				new Rect(x, rect.y + 4f, 1f, rect.height - 8f),
				new Color(1f, 1f, 1f, 0.08f));
		}

		private void DrawStatusRow(Rect row, Pawn robot, int rowIndex)
		{
			if (rowIndex % 2 == 1)
			{
				Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
			}
			Widgets.DrawHighlightIfMouseover(row);

			CompNannyBot comp = NannyBotUtility.BotComp(robot);
			Need_RobotEnergy energy = NannyBotUtility.Energy(robot);

			float x = 0f;

			Rect nameRect = new Rect(x, row.y, StatusNameWidth, row.height);
			if (Widgets.ButtonInvisible(nameRect))
			{
				CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(robot));
			}
			using (new TextBlock(TextAnchor.MiddleLeft))
			{
				Widgets.Label(
					new Rect(nameRect.x + 6f, nameRect.y, nameRect.width - 10f, nameRect.height),
					robot.LabelShort.Truncate(nameRect.width - 10f));
			}
			TooltipHandler.TipRegion(nameRect, "CK.RobotManagement.Select".Translate());
			x += StatusNameWidth;
			DrawStatusColumnLine(x, row);

			Rect statusRect = new Rect(x, row.y, StatusStateWidth, row.height);
			string status;
			Color statusColor;
			GetStatus(robot, comp, energy, out status, out statusColor);
			using (new TextBlock(TextAnchor.MiddleCenter, statusColor))
			{
				Widgets.Label(statusRect, status);
			}
			x += StatusStateWidth;
			DrawStatusColumnLine(x, row);

			Rect taskRect = new Rect(x, row.y, StatusTaskWidth, row.height);
			string task = NannyBotJobUtility.TaskText(robot);
			using (new TextBlock(TextAnchor.MiddleLeft))
			{
				Widgets.Label(
					new Rect(taskRect.x + 6f, taskRect.y, taskRect.width - 10f, taskRect.height),
					task.Truncate(taskRect.width - 10f));
			}
			TooltipHandler.TipRegion(taskRect, task);
			x += StatusTaskWidth;
			DrawStatusColumnLine(x, row);

			Rect energyRect = new Rect(x, row.y, StatusBarWidth, row.height);
			string energyText = energy == null
				? "-"
				: energy.CurLevel.ToString("0") + " / " + energy.MaxLevel.ToString("0") + " W";
			string energyTip = energy == null
				? null
				: "CK.Robotics.Inspect.Battery".Translate(
					energy.CurLevel.ToString("0"),
					energy.MaxLevel.ToString("0"));
			DrawStatusBarCell(
				energyRect,
				energy == null ? 0f : energy.CurLevelPercentage,
				energyText,
				energyTip);
			x += StatusBarWidth;
			DrawStatusColumnLine(x, row);

			Rect durabilityRect = new Rect(x, row.y, StatusBarWidth, row.height);
			float durability = comp == null ? 0f : comp.Durability;
			float maxDurability = comp?.MaxDurability ?? CompNannyBot.DefaultMaxDurability;
			DrawStatusBarCell(
				durabilityRect,
				durability / maxDurability,
				durability.ToString("0") + " / " + maxDurability.ToString("0"),
				"CK.Robotics.Inspect.Durability".Translate(
					durability.ToString("0"),
					maxDurability.ToString("0")));
			x += StatusBarWidth;
			DrawStatusColumnLine(x, row);

			Rect stationRect = new Rect(x, row.y, StatusStationWidth, row.height);
			Building station = comp?.homeStation;
			string stationText = station != null
				? station.LabelShort
				: "CK.RobotManagement.NoStation".Translate();
			using (new TextBlock(TextAnchor.MiddleLeft))
			{
				Widgets.Label(
					new Rect(stationRect.x + 6f, stationRect.y, stationRect.width - 10f, stationRect.height),
					stationText.Truncate(stationRect.width - 10f));
			}
			x += StatusStationWidth;
			DrawStatusColumnLine(x, row);

			Rect selectRect = new Rect(x, row.y + 5f, 56f, 28f);
			if (Widgets.ButtonText(selectRect, "CK.RobotManagement.Select".Translate()))
			{
				CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(robot));
			}

			bool previousEnabled = GUI.enabled;
			GUI.enabled = previousEnabled && station != null;
			if (Widgets.ButtonText(
				new Rect(selectRect.x + 60f, selectRect.y, 56f, 28f),
				"CK.RobotManagement.Station".Translate())
				&& station != null)
			{
				CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(station));
			}
			GUI.enabled = previousEnabled;
		}

		private static void DrawStatusBarCell(Rect rect, float value, string text, string tip)
		{
			float percent = Mathf.Clamp01(value);
			Rect bar = new Rect(rect.x + 6f, rect.y + 21f, rect.width - 12f, 8f);
			Widgets.DrawBoxSolid(bar, new Color(0.34f, 0.34f, 0.34f, 0.9f));
			Rect inner = bar.ContractedBy(1f);
			Widgets.DrawBoxSolid(inner, new Color(0.08f, 0.08f, 0.08f, 0.9f));
			Rect fill = inner;
			fill.width *= percent;
			if (fill.width > 0f)
			{
				Widgets.DrawBoxSolid(fill, StatusBarColor(percent));
			}

			using (new TextBlock(TextAnchor.MiddleCenter))
			{
				Text.Font = GameFont.Tiny;
				Widgets.Label(new Rect(rect.x, rect.y + 2f, rect.width, 16f), text);
			}

			if (!tip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tip);
			}
		}

		private static Color StatusBarColor(float percent)
		{
			if (percent <= 0.2f)
			{
				return new Color(0.88f, 0.32f, 0.28f);
			}
			if (percent <= 0.5f)
			{
				return new Color(0.95f, 0.78f, 0.30f);
			}
			return new Color(0.42f, 0.82f, 0.45f);
		}

		private static void GetStatus(
			Pawn robot,
			CompNannyBot comp,
			Need_RobotEnergy energy,
			out string text,
			out Color color)
		{
			if (robot.Downed)
			{
				text = "CK.RobotManagement.Status.Downed".Translate();
				color = new Color(0.88f, 0.45f, 0.40f);
				return;
			}
			if (comp != null && comp.repairing)
			{
				text = "CK.RobotManagement.Status.Repairing".Translate();
				color = new Color(0.95f, 0.78f, 0.30f);
				return;
			}
			if (comp != null && comp.paused)
			{
				text = "CK.RobotManagement.Status.Paused".Translate();
				color = Color.grey;
				return;
			}
			CompMkVCombat combat = robot.TryGetComp<CompMkVCombat>();
			if (combat != null && combat.state == MkVCombatState.Engaged)
			{
				text = "CK.RobotManagement.Status.Combat".Translate();
				color = new Color(0.92f, 0.52f, 0.35f);
				return;
			}
			if (combat != null && combat.state == MkVCombatState.Returning)
			{
				text = "CK.RobotManagement.Status.Returning".Translate();
				color = new Color(0.45f, 0.80f, 0.95f);
				return;
			}
			if (energy != null && energy.IsCharging)
			{
				text = "CK.RobotManagement.Status.Charging".Translate();
				color = new Color(0.45f, 0.80f, 0.95f);
				return;
			}
			if (energy != null && energy.CurLevelPercentage <= 0.001f)
			{
				text = "CK.RobotManagement.Status.Empty".Translate();
				color = new Color(0.88f, 0.32f, 0.28f);
				return;
			}
			if (robot.CurJobDef == null)
			{
				text = "CK.RobotManagement.Status.Idle".Translate();
				color = Color.grey;
				return;
			}
			text = "CK.RobotManagement.Status.Working".Translate();
			color = new Color(0.50f, 0.85f, 0.50f);
		}
	}
}
