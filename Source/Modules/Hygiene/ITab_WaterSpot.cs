using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>免费取水点的「清单」页：添加取水 N 次 / 维持 N 个满桶任务。</summary>
	public class ITab_WaterSpot : ITab
	{
		private static readonly Vector2 WinSize = new Vector2(430f, 320f);

		public ITab_WaterSpot()
		{
			size = WinSize;
			labelKey = "CK.Hygiene.WaterSpot.Tab";
		}

		private Building_WaterSpot Spot => SelThing as Building_WaterSpot;

		protected override void FillTab()
		{
			Building_WaterSpot spot = Spot;
			if (spot == null)
			{
				return;
			}
			Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
			Widgets.BeginGroup(rect);
			Text.Font = GameFont.Small;
			float y = 0f;

			Widgets.Label(new Rect(0f, y, rect.width, 24f), "CK.Hygiene.WaterSpot.TabTitle".Translate());
			y += 28f;

			if (Widgets.ButtonText(new Rect(0f, y, 100f, 28f), "CK.Hygiene.WaterSpot.AddDraw".Translate(1)))
			{
				spot.AddTask(WaterSpotTaskType.Draw, 1);
			}
			if (Widgets.ButtonText(new Rect(106f, y, 100f, 28f), "CK.Hygiene.WaterSpot.AddDraw".Translate(5)))
			{
				spot.AddTask(WaterSpotTaskType.Draw, 5);
			}
			if (Widgets.ButtonText(new Rect(212f, y, 100f, 28f), "CK.Hygiene.WaterSpot.AddMaintain".Translate(1)))
			{
				spot.AddTask(WaterSpotTaskType.Maintain, 1);
			}
			if (Widgets.ButtonText(new Rect(318f, y, 100f, 28f), "CK.Hygiene.WaterSpot.AddMaintain".Translate(5)))
			{
				spot.AddTask(WaterSpotTaskType.Maintain, 5);
			}
			y += 36f;

			Widgets.Label(
				new Rect(0f, y, rect.width, 24f),
				"CK.Hygiene.WaterSpot.FilledBuckets".Translate(spot.CountFilledBuckets()));
			y += 28f;

			List<WaterSpotTask> tasks = spot.Tasks;
			if (tasks == null || tasks.Count == 0)
			{
				GUI.color = ColoredText.SubtleGrayColor;
				Widgets.Label(new Rect(0f, y, rect.width, 60f), "CK.Hygiene.WaterSpot.NoTasks".Translate());
				GUI.color = Color.white;
				y += 34f;
			}
			else
			{
				for (int i = 0; i < tasks.Count; i++)
				{
					WaterSpotTask task = tasks[i];
					string label = task.type == WaterSpotTaskType.Draw
						? "CK.Hygiene.WaterSpot.DrawTask".Translate(task.completed, task.target)
						: "CK.Hygiene.WaterSpot.MaintainTask".Translate(spot.CountFilledBuckets(), task.target);
					Widgets.Label(new Rect(0f, y + 4f, 220f, 24f), label);
					if (Widgets.ButtonText(new Rect(224f, y, 40f, 26f), "-1"))
					{
						spot.AdjustTask(task, -1);
					}
					if (Widgets.ButtonText(new Rect(270f, y, 40f, 26f), "+1"))
					{
						spot.AdjustTask(task, 1);
					}
					if (Widgets.ButtonText(new Rect(316f, y, 90f, 26f), "CK.Hygiene.WaterSpot.Delete".Translate()))
					{
						spot.RemoveTask(task);
					}
					y += 30f;
				}
			}

			y += 6f;
			GUI.color = ColoredText.SubtleGrayColor;
			Widgets.Label(new Rect(0f, y, rect.width, 60f), "CK.Hygiene.WaterSpot.Hint".Translate());
			GUI.color = Color.white;
			Widgets.EndGroup();
		}
	}
}
