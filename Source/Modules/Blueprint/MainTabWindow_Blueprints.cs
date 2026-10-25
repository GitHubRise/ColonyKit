using System.Collections.Generic;
using System.IO;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>底部「蓝图」主标签页：本存档蓝图库 + 全局模板 + 捕获/放置入口。</summary>
	public class MainTabWindow_Blueprints : MainTabWindow
	{
		private const float ListWidth = 300f;
		private const float RowHeight = 48f;

		private static readonly Color ShortageColor = new Color(1f, 0.42f, 0.42f);

		private BlueprintRecord selected;
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

			Rect outRect = new Rect(inner.x, inner.y + 24f, inner.width, inner.height - 24f);
			if (library.records.Count == 0)
			{
				Widgets.Label(new Rect(outRect.x, outRect.y, outRect.width, 40f), "CK.Blueprint.Tab.Empty".Translate());
				return;
			}

			float viewHeight = Mathf.Max(outRect.height, library.records.Count * RowHeight);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, viewHeight);

			Widgets.BeginScrollView(outRect, ref listScroll, viewRect);
			float y = 0f;
			foreach (BlueprintRecord record in library.records)
			{
				Rect row = new Rect(0f, y, viewRect.width, RowHeight - 4f);
				if (record == selected)
				{
					Widgets.DrawHighlightSelected(row);
				}
				else
				{
					Widgets.DrawHighlightIfMouseover(row);
				}

				Widgets.Label(
					new Rect(row.x + 4f, row.y + 3f, row.width - 8f, 20f),
					record.name ?? "");
				Widgets.Label(
					new Rect(row.x + 4f, row.y + 22f, row.width - 8f, 18f),
					"CK.Blueprint.Tab.RowMeta".Translate(record.sizeX, record.sizeZ, record.entries.Count));

				if (Widgets.ButtonInvisible(row))
				{
					Select(record);
				}

				y += RowHeight;
			}
			Widgets.EndScrollView();
		}

		private void DrawDetails(Rect rect, BlueprintLibrary library, BlueprintSettings settings)
		{
			Widgets.DrawMenuSection(rect);
			Rect inner = rect.ContractedBy(8f);

			if (selected == null)
			{
				Widgets.Label(new Rect(inner.x, inner.y, inner.width, 30f), "CK.Blueprint.Tab.NoneSelected".Translate());
				DrawTemplateSection(inner, library);
				return;
			}

			float y = inner.y;

			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inner.x, y, inner.width, 30f), selected.name ?? "");
			Text.Font = GameFont.Small;
			y += 30f;

			Widgets.Label(
				new Rect(inner.x, y, inner.width, 20f),
				"CK.Blueprint.Tab.Details".Translate(
					selected.sizeX,
					selected.sizeZ,
					selected.entries.Count,
					selected.createdTick));
			y += 26f;

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

			if (Widgets.ButtonText(new Rect(x, rect.y, 76f, 30f), "CK.Blueprint.Tab.Rename".Translate()))
			{
				Find.WindowStack.Add(new Dialog_RenameBlueprint(record));
			}
			x += 80f;

			if (Widgets.ButtonText(new Rect(x, rect.y, 76f, 30f), "CK.Blueprint.Tab.Delete".Translate()))
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

					if (Widgets.ButtonText(new Rect(row.xMax - 72f, row.y + 3f, 72f, 24f), "CK.Blueprint.Tab.Delete".Translate()))
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
