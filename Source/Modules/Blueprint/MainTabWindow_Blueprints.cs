using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>底部「蓝图」主标签页：本存档蓝图库 + 全局模板 + 捕获/放置入口。</summary>
	public class MainTabWindow_Blueprints : MainTabWindow
	{
		private const float ListWidth = 300f;
		private const float RowHeight = 64f;
		private const float HeaderHeight = 22f;

		private static readonly Color ShortageColor = new Color(1f, 0.42f, 0.42f);
		private static readonly Color HeaderColor = new Color(0.68f, 0.68f, 0.68f);
		private static readonly Color TagColor = new Color(0.66f, 0.78f, 0.92f);
		private static readonly Color PinnedColor = new Color(1f, 0.82f, 0.35f);

		private BlueprintRecord selected;
		private string filter = "";
		private Vector2 listScroll;
		private Vector2 materialScroll;
		private Vector2 templateScroll;
		private List<BlueprintTemplate> templates = new List<BlueprintTemplate>();
		private bool templatesLoaded;

		public override Vector2 RequestedTabSize => new Vector2(920f, 650f);

		/// <summary>捕获/载入完成后打开本页并选中记录。</summary>
		public static void OpenAndSelect(BlueprintRecord record)
		{
			MainButtonDef def = DefDatabase<MainButtonDef>.GetNamedSilentFail("CK_Blueprints");
			if (def == null || !(def.TabWindow is MainTabWindow_Blueprints window))
			{
				return;
			}

			Find.MainTabsRoot.SetCurrentTab(def);
			window.Select(record);
		}

		public void Select(BlueprintRecord record)
		{
			selected = record;
			listScroll = Vector2.zero;
			materialScroll = Vector2.zero;
		}

		public override void DoWindowContents(Rect inRect)
		{
			BlueprintLibrary library = BlueprintGameComponent.CurrentLibrary;
			BlueprintSettings settings = BlueprintSettingsUtility.Current;

			if (library == null)
			{
				Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "CK.Blueprint.NoLibrary".Translate());
				return;
			}

			if (selected == null && library.records.Count > 0)
			{
				selected = library.records[0];
			}
			if (selected != null && !library.records.Contains(selected))
			{
				selected = null;
			}

			DrawHeader(inRect, library, settings);

			float top = 44f;
			float height = inRect.height - top;

			Rect listRect = new Rect(0f, top, ListWidth, height);
			Rect detailRect = new Rect(ListWidth + 12f, top, inRect.width - ListWidth - 12f, height);

			DrawLibraryList(listRect, library);
			DrawDetails(detailRect, library, settings);
		}

		private void DrawHeader(Rect inRect, BlueprintLibrary library, BlueprintSettings settings)
		{
			Text.Font = GameFont.Medium;
			Widgets.Label(
				new Rect(0f, 0f, 300f, 32f),
				"CK.Blueprint.Tab.Title".Translate());
			Text.Font = GameFont.Small;

			Widgets.Label(
				new Rect(0f, 26f, 300f, 20f),
				"CK.Blueprint.Tab.Count".Translate(library.records.Count, settings.maxRecords));

			float modeWidth = 104f;
			float modeX = 310f;
			DrawModeButton(new Rect(modeX, 4f, modeWidth, 30f), settings, BlueprintMaterialMode.PreferSource);
			DrawModeButton(new Rect(modeX + modeWidth + 4f, 4f, modeWidth, 30f), settings, BlueprintMaterialMode.Available);
			DrawModeButton(new Rect(modeX + (modeWidth + 4f) * 2f, 4f, modeWidth, 30f), settings, BlueprintMaterialMode.Cheapest);

			if (Widgets.ButtonText(new Rect(inRect.width - 132f, 4f, 132f, 30f), "CK.Blueprint.Tab.Capture".Translate()))
			{
				Find.MainTabsRoot.EscapeCurrentTab(false);
				Find.DesignatorManager.Select(new Designator_BlueprintCapture());
			}
		}

		private void DrawModeButton(Rect rect, BlueprintSettings settings, BlueprintMaterialMode mode)
		{
			bool active = settings.materialMode == mode;
			if (active)
			{
				Widgets.DrawHighlightSelected(rect);
			}

			if (Widgets.ButtonText(rect, ModeLabel(mode)))
			{
				if (settings.materialMode != mode)
				{
					settings.materialMode = mode;
					ColonyKitMod.Instance?.WriteSettings();
				}
			}
		}

		private void DrawLibraryList(Rect rect, BlueprintLibrary library)
		{
			Widgets.DrawMenuSection(rect);
			Rect inner = rect.ContractedBy(6f);

			Widgets.Label(
				new Rect(inner.x, inner.y, inner.width, 22f),
				"CK.Blueprint.Tab.Library".Translate());

			Rect filterRect = new Rect(inner.x, inner.y + 24f, inner.width, 24f);
			Widgets.Label(
				new Rect(filterRect.x, filterRect.y + 2f, 40f, 20f),
				"CK.Blueprint.Filter.Label".Translate());
			Rect filterField = new Rect(filterRect.x + 42f, filterRect.y, filterRect.width - 42f, 24f);
			filter = Widgets.TextField(filterField, filter ?? "");
			TooltipHandler.TipRegion(filterRect, "CK.Blueprint.Filter.Tip".Translate());

			Rect outRect = new Rect(inner.x, inner.y + 54f, inner.width, inner.height - 54f);
			if (library.records.Count == 0)
			{
				Widgets.Label(new Rect(outRect.x, outRect.y, outRect.width, 40f), "CK.Blueprint.Tab.Empty".Translate());
				return;
			}

			List<BlueprintListRow> rows = BuildRows(library);
			int visible = 0;
			foreach (BlueprintListRow row in rows)
			{
				if (!row.IsHeader)
				{
					visible++;
				}
			}

			if (!filter.NullOrEmpty())
			{
				Widgets.Label(
					new Rect(inner.x + inner.width - 104f, inner.y, 104f, 20f),
					"CK.Blueprint.Filter.Count".Translate(visible, library.records.Count));
			}

			if (rows.Count == 0)
			{
				Widgets.Label(
					new Rect(outRect.x, outRect.y, outRect.width, 40f),
					"CK.Blueprint.Filter.NoMatch".Translate());
				return;
			}

			float contentHeight = 0f;
			foreach (BlueprintListRow row in rows)
			{
				contentHeight += row.IsHeader ? HeaderHeight : RowHeight;
			}

			float viewHeight = Mathf.Max(outRect.height, contentHeight);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, viewHeight);

			Widgets.BeginScrollView(outRect, ref listScroll, viewRect);
			float y = 0f;
			foreach (BlueprintListRow row in rows)
			{
				if (row.IsHeader)
				{
					DrawListHeader(new Rect(0f, y, viewRect.width, HeaderHeight), row.header);
					y += HeaderHeight;
					continue;
				}

				BlueprintRecord record = row.record;
				Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight - 4f);
				if (record == selected)
				{
					Widgets.DrawHighlightSelected(rowRect);
				}
				else
				{
					Widgets.DrawHighlightIfMouseover(rowRect);
				}

				Widgets.Label(
					new Rect(rowRect.x + 4f, rowRect.y + 3f, rowRect.width - 26f, 20f),
					record.name ?? "");
				if (record.pinned)
				{
					GUI.color = PinnedColor;
					Widgets.Label(new Rect(rowRect.xMax - 20f, rowRect.y + 3f, 18f, 20f), "★");
					GUI.color = Color.white;
				}

				Widgets.Label(
					new Rect(rowRect.x + 4f, rowRect.y + 22f, rowRect.width - 8f, 18f),
					"CK.Blueprint.Tab.RowMeta".Translate(record.sizeX, record.sizeZ, record.entries.Count));

				string tagText = TagsText(record.tags, 3);
				if (!tagText.NullOrEmpty())
				{
					Text.Font = GameFont.Tiny;
					GUI.color = TagColor;
					Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 39f, rowRect.width - 8f, 16f), tagText);
					GUI.color = Color.white;
					Text.Font = GameFont.Small;
				}
				else if (!record.group.NullOrEmpty())
				{
					Text.Font = GameFont.Tiny;
					GUI.color = HeaderColor;
					Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 39f, rowRect.width - 8f, 16f), record.group);
					GUI.color = Color.white;
					Text.Font = GameFont.Small;
				}

				if (Event.current.type == EventType.MouseDown
					&& Event.current.button == 1
					&& Mouse.IsOver(rowRect))
				{
					ShowRecordMenu(record);
					Event.current.Use();
					y += RowHeight;
					continue;
				}

				if (Widgets.ButtonInvisible(rowRect))
				{
					Select(record);
				}

				y += RowHeight;
			}
			Widgets.EndScrollView();
		}

		private void DrawListHeader(Rect rect, string label)
		{
			Text.Font = GameFont.Tiny;
			GUI.color = HeaderColor;
			Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 18f), label);
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			Widgets.DrawLineHorizontal(rect.x, rect.y + rect.height - 2f, rect.width);
		}

		private List<BlueprintListRow> BuildRows(BlueprintLibrary library)
		{
			List<BlueprintListRow> rows = new List<BlueprintListRow>();
			List<BlueprintRecord> pinned = new List<BlueprintRecord>();
			List<BlueprintRecord> ungrouped = new List<BlueprintRecord>();
			Dictionary<string, List<BlueprintRecord>> groups =
				new Dictionary<string, List<BlueprintRecord>>(StringComparer.OrdinalIgnoreCase);

			foreach (BlueprintRecord record in library.records)
			{
				if (!MatchesFilter(record))
				{
					continue;
				}

				if (record.pinned)
				{
					pinned.Add(record);
				}
				else if (!record.group.NullOrEmpty())
				{
					if (!groups.TryGetValue(record.group, out List<BlueprintRecord> list))
					{
						list = new List<BlueprintRecord>();
						groups[record.group] = list;
					}
					list.Add(record);
				}
				else
				{
					ungrouped.Add(record);
				}
			}

			if (pinned.Count > 0)
			{
				rows.Add(HeaderRow("CK.Blueprint.Pinned".Translate()));
				foreach (BlueprintRecord record in pinned)
				{
					rows.Add(RecordRow(record));
				}
			}

			List<string> groupNames = new List<string>(groups.Keys);
			groupNames.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (string group in groupNames)
			{
				rows.Add(HeaderRow(group));
				foreach (BlueprintRecord record in groups[group])
				{
					rows.Add(RecordRow(record));
				}
			}

			if (ungrouped.Count > 0)
			{
				if (pinned.Count > 0 || groupNames.Count > 0)
				{
					rows.Add(HeaderRow("CK.Blueprint.Ungrouped".Translate()));
				}
				foreach (BlueprintRecord record in ungrouped)
				{
					rows.Add(RecordRow(record));
				}
			}

			return rows;
		}

		private static BlueprintListRow HeaderRow(string header)
		{
			return new BlueprintListRow { header = header };
		}

		private static BlueprintListRow RecordRow(BlueprintRecord record)
		{
			return new BlueprintListRow { record = record };
		}

		private bool MatchesFilter(BlueprintRecord record)
		{
			string query = filter?.Trim();
			if (query.NullOrEmpty())
			{
				return true;
			}

			bool tagsOnly = query.StartsWith("#");
			string needle = tagsOnly ? query.Substring(1).Trim() : query;
			if (needle.NullOrEmpty())
			{
				return true;
			}

			if (!tagsOnly)
			{
				if (!record.name.NullOrEmpty()
					&& record.name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
				if (!record.group.NullOrEmpty()
					&& record.group.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			if (record.tags != null)
			{
				foreach (string tag in record.tags)
				{
					if (!tag.NullOrEmpty()
						&& tag.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static string TagsText(List<string> tags, int max)
		{
			if (tags == null || tags.Count == 0)
			{
				return "";
			}

			int count = Math.Min(max, tags.Count);
			StringBuilder builder = new StringBuilder();
			for (int index = 0; index < count; index++)
			{
				if (index > 0)
				{
					builder.Append(' ');
				}
				builder.Append('#').Append(tags[index]);
			}
			if (tags.Count > count)
			{
				builder.Append(" +").Append(tags.Count - count);
			}
			return builder.ToString();
		}

		private void ShowRecordMenu(BlueprintRecord record)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>
			{
				new FloatMenuOption(
					(record.pinned ? "CK.Blueprint.Tab.Unpin" : "CK.Blueprint.Tab.Pin").Translate(),
					() => record.pinned = !record.pinned),
				new FloatMenuOption(
					"CK.Blueprint.Menu.Edit".Translate(),
					() => Find.WindowStack.Add(new Dialog_BlueprintProperties(record)))
			};
			Find.WindowStack.Add(new FloatMenu(options));
		}

		private sealed class BlueprintListRow
		{
			public string header;
			public BlueprintRecord record;

			public bool IsHeader => record == null && header != null;
		}

		private void DrawDetails(Rect rect, BlueprintLibrary library, BlueprintSettings settings)
		{
			Widgets.DrawMenuSection(rect);
			Rect inner = rect.ContractedBy(8f);

			if (selected == null)
			{
				Widgets.Label(new Rect(inner.x, inner.y, inner.width, 30f), "CK.Blueprint.Tab.NoneSelected".Translate());
				// 空选择提示占一行，模板区必须从提示下方开始，否则两个标题会叠在一起。
				DrawTemplateSection(
					new Rect(inner.x, inner.y + 30f, inner.width, inner.height - 30f),
					library);
				return;
			}

			float y = inner.y;

			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inner.x, y, inner.width - 24f, 30f), selected.name ?? "");
			Text.Font = GameFont.Small;
			if (selected.pinned)
			{
				GUI.color = PinnedColor;
				Widgets.Label(new Rect(inner.xMax - 22f, y + 4f, 20f, 22f), "★");
				GUI.color = Color.white;
			}
			y += 30f;

			Widgets.Label(
				new Rect(inner.x, y, inner.width, 20f),
				"CK.Blueprint.Tab.Details".Translate(
					selected.sizeX,
					selected.sizeZ,
					selected.entries.Count,
					selected.createdTick));
			y += 26f;

			if (!selected.group.NullOrEmpty())
			{
				Widgets.Label(
					new Rect(inner.x, y, inner.width, 20f),
					"CK.Blueprint.Details.Group".Translate(selected.group));
				y += 20f;
			}
			if (selected.tags != null && selected.tags.Count > 0)
			{
				Widgets.Label(
					new Rect(inner.x, y, inner.width, 20f),
					"CK.Blueprint.Details.Tags".Translate(TagsText(selected.tags, 99)));
				y += 20f;
			}

			if (DrawActionButtons(new Rect(inner.x, y, inner.width, 32f), library, settings))
			{
				return;
			}
			y += 40f;

			Widgets.Label(new Rect(inner.x, y, inner.width, 20f), "CK.Blueprint.Tab.Materials".Translate());
			y += 22f;

			Rect materialRect = new Rect(inner.x, y, inner.width, 148f);
			DrawMaterials(materialRect, library, settings);
			y += 156f;

			DrawTemplateSection(new Rect(inner.x, y, inner.width, inner.yMax - y), library);
		}

		/// <summary>返回 true 表示本帧记录已被删除，调用方应停止绘制后续内容。</summary>
		private bool DrawActionButtons(Rect rect, BlueprintLibrary library, BlueprintSettings settings)
		{
			BlueprintRecord record = selected;
			float x = rect.x;

			if (Widgets.ButtonText(new Rect(x, rect.y, 96f, 30f), "CK.Blueprint.Tab.Place".Translate()))
			{
				Find.MainTabsRoot.EscapeCurrentTab(false);
				Find.DesignatorManager.Select(new Designator_BlueprintPlace(record));
				return true;
			}
			x += 100f;

			if (Widgets.ButtonText(new Rect(x, rect.y, 72f, 30f), "CK.Blueprint.Tab.Edit".Translate()))
			{
				Find.WindowStack.Add(new Dialog_BlueprintProperties(record));
			}
			x += 76f;

			if (Widgets.ButtonText(
				new Rect(x, rect.y, 88f, 30f),
				(record.pinned ? "CK.Blueprint.Tab.Unpin" : "CK.Blueprint.Tab.Pin").Translate()))
			{
				record.pinned = !record.pinned;
			}
			x += 92f;

			if (Widgets.ButtonText(new Rect(x, rect.y, 72f, 30f), "CK.Blueprint.Tab.Delete".Translate()))
			{
				BlueprintRecord toDelete = record;
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
					"CK.Blueprint.Delete.Confirm".Translate(toDelete.name),
					() =>
					{
						library.Remove(toDelete);
						if (selected == toDelete)
						{
							selected = null;
						}
					},
					true));
			}
			x += 80f;

			if (Widgets.ButtonText(new Rect(x, rect.y, 116f, 30f), "CK.Blueprint.Tab.SaveTemplate".Translate()))
			{
				SaveTemplate(record);
			}

			return false;
		}

		private void DrawMaterials(Rect rect, BlueprintLibrary library, BlueprintSettings settings)
		{
			List<BlueprintSubstitution> substitutions = new List<BlueprintSubstitution>();
			List<ThingDefCountClass> costs = BlueprintPlacementUtility.CostSummary(
				selected,
				Find.CurrentMap,
				settings.materialMode,
				substitutions);

			Rect inner = rect.ContractedBy(4f);
			if (costs.Count == 0)
			{
				Widgets.Label(new Rect(inner.x, inner.y, inner.width, 24f), "CK.Blueprint.Tab.NoMaterials".Translate());
				return;
			}

			float viewHeight = Mathf.Max(inner.height, costs.Count * 24f + substitutions.Count * 20f);
			Rect viewRect = new Rect(0f, 0f, inner.width - 18f, viewHeight);

			Widgets.BeginScrollView(inner, ref materialScroll, viewRect);
			float y = 0f;
			Map map = Find.CurrentMap;

			foreach (ThingDefCountClass cost in costs)
			{
				Rect iconRect = new Rect(0f, y, 20f, 20f);
				Widgets.ThingIcon(iconRect, cost.thingDef);

				int have = BlueprintMaterialPolicy.CountOnMap(map, cost.thingDef);
				Color oldColor = GUI.color;
				if (have < cost.count)
				{
					GUI.color = ShortageColor;
				}

				Widgets.Label(
					new Rect(24f, y, viewRect.width - 24f, 22f),
					"CK.Blueprint.Tab.MaterialLine".Translate(cost.thingDef.LabelCap, cost.count, have));
				GUI.color = oldColor;
				y += 24f;
			}

			foreach (BlueprintSubstitution substitution in substitutions)
			{
				Widgets.Label(
					new Rect(0f, y, viewRect.width, 20f),
					"CK.Blueprint.Tab.Substitution".Translate(substitution.source.LabelCap, substitution.target.LabelCap));
				y += 20f;
			}
			Widgets.EndScrollView();
		}

		private void DrawTemplateSection(Rect rect, BlueprintLibrary library)
		{
			if (!templatesLoaded)
			{
				RefreshTemplates();
			}

			if (rect.height < 80f)
			{
				return;
			}

			Widgets.Label(new Rect(rect.x, rect.y, 200f, 22f), "CK.Blueprint.Tab.Templates".Translate());

			if (Widgets.ButtonText(new Rect(rect.xMax - 76f, rect.y, 76f, 24f), "CK.Blueprint.Tab.Refresh".Translate()))
			{
				RefreshTemplates();
			}

			Rect outRect = new Rect(rect.x, rect.y + 26f, rect.width, rect.yMax - rect.y - 26f);
			if (templates.Count == 0)
			{
				Widgets.Label(new Rect(outRect.x, outRect.y, outRect.width, 24f), "CK.Blueprint.Tab.NoTemplates".Translate());
				return;
			}

			float viewHeight = Mathf.Max(outRect.height, templates.Count * 32f);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, viewHeight);

			Widgets.BeginScrollView(outRect, ref templateScroll, viewRect);
			float y = 0f;
			foreach (BlueprintTemplate template in templates)
			{
				Rect row = new Rect(0f, y, viewRect.width, 30f);
				Widgets.DrawHighlightIfMouseover(row);

				string label = template.record != null
					? template.record.name + "  " + template.record.sizeX + "×" + template.record.sizeZ
					: Path.GetFileNameWithoutExtension(template.path);
				if (template.record != null)
				{
					if (!template.record.group.NullOrEmpty())
					{
						label = "[" + template.record.group + "] " + label;
					}

					string templateTags = TagsText(template.record.tags, 3);
					if (!templateTags.NullOrEmpty())
					{
						label += "  " + templateTags;
					}
				}

				if (template.record == null)
				{
					GUI.color = ShortageColor;
					Widgets.Label(new Rect(row.x, row.y + 5f, row.width - 150f, 22f), label + " — " + template.error);
					GUI.color = Color.white;
				}
				else
				{
					Widgets.Label(new Rect(row.x, row.y + 5f, row.width - 150f, 22f), label);

					if (Widgets.ButtonText(new Rect(row.xMax - 146f, row.y + 3f, 70f, 24f), "CK.Blueprint.Tab.LoadTemplate".Translate()))
					{
						BlueprintRecord imported = library.Import(template.record);
						if (imported != null)
						{
							selected = imported;
							Messages.Message(
								"CK.Blueprint.Template.Loaded".Translate(imported.name),
								MessageTypeDefOf.PositiveEvent,
								false);
						}
					}

					if (template.isBuiltIn)
					{
						Rect tag = new Rect(row.xMax - 72f, row.y + 5f, 72f, 22f);
						GUI.color = new Color(0.65f, 0.65f, 0.65f);
						Widgets.Label(tag, "CK.Blueprint.Template.BuiltIn".Translate());
						GUI.color = Color.white;
						TooltipHandler.TipRegion(tag, "CK.Blueprint.Template.BuiltInTip".Translate());
					}
					else if (Widgets.ButtonText(new Rect(row.xMax - 72f, row.y + 3f, 72f, 24f), "CK.Blueprint.Tab.Delete".Translate()))
					{
						BlueprintTemplate toDelete = template;
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
							"CK.Blueprint.Template.DeleteConfirm".Translate(label),
							() =>
							{
								if (BlueprintTemplateStorage.Delete(toDelete, out string error))
								{
									RefreshTemplates();
								}
								else if (!error.NullOrEmpty())
								{
									Messages.Message(error, MessageTypeDefOf.RejectInput, false);
								}
							},
							true));
					}
				}

				y += 32f;
			}
			Widgets.EndScrollView();
		}

		private void RefreshTemplates()
		{
			templates = BlueprintTemplateStorage.LoadAll();
			templatesLoaded = true;
			templateScroll = Vector2.zero;
		}

		private void SaveTemplate(BlueprintRecord record)
		{
			string path = BlueprintTemplateStorage.Save(record, out string error);
			if (path.NullOrEmpty())
			{
				Messages.Message(error, MessageTypeDefOf.RejectInput, false);
				return;
			}

			templatesLoaded = false;
			Messages.Message(
				"CK.Blueprint.Template.Saved".Translate(Path.GetFileName(path)),
				MessageTypeDefOf.PositiveEvent,
				false);
		}

		private static string ModeLabel(BlueprintMaterialMode mode)
		{
			switch (mode)
			{
				case BlueprintMaterialMode.Available:
					return "CK.Blueprint.Mode.Available".Translate();
				case BlueprintMaterialMode.Cheapest:
					return "CK.Blueprint.Mode.Cheapest".Translate();
				default:
					return "CK.Blueprint.Mode.PreferSource".Translate();
			}
		}
	}
}
