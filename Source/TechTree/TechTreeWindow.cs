using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 独立研究树窗口：标签选择研究 tab、切换布局方式、滚动查看整棵树。
	/// 从设置「关于」页的按钮打开；不替换原版研究界面。
	/// </summary>
	public sealed class TechTreeWindow : Window
	{
		private ResearchTabDef tab;
		private TechTreeLayoutMode layoutMode = TechTreeLayoutMode.PrerequisiteTiers;
		private TechTree tree;
		private Vector2 scroll;
		private bool dirty = true;

		public TechTreeWindow()
		{
			doCloseX = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = false;

			// 前置条件被外部修改时，下一帧重建布局。
			TechTreeService.Changed += MarkDirty;
		}

		public override Vector2 InitialSize => new Vector2(1100f, 760f);

		public override void PreOpen()
		{
			base.PreOpen();

			// 默认跟随当前研究项目，否则回到原版主研究 tab。
			tab = Find.ResearchManager.GetProject()?.tab ?? ResearchTabDefOf.Main;
			Rebuild();
		}

		public override void PostClose()
		{
			TechTreeService.Changed -= MarkDirty;
			base.PostClose();
		}

		public override void DoWindowContents(Rect inRect)
		{
			Rect toolbarRect = new Rect(0f, 0f, inRect.width, 32f);
			DrawToolbar(toolbarRect);

			Rect canvasRect = new Rect(
				0f,
				toolbarRect.yMax + 6f,
				inRect.width,
				Mathf.Max(0f, inRect.height - toolbarRect.height - 6f));

			if (dirty || tree == null)
			{
				Rebuild();
			}

			if (tree == null)
			{
				Widgets.Label(canvasRect, "ModTemplate.TechTree.BuildFailed".Translate());
				return;
			}

			// 点击节点直接设为当前研究；是否真的能开始仍由原版 ResearchManager 判断。
			TechTreeView.Draw(canvasRect, tree, ref scroll, def => Find.ResearchManager.SetCurrentProject(def));
		}

		private void DrawToolbar(Rect inRect)
		{
			Rect tabRect = new Rect(inRect.x, inRect.y, 240f, 30f);
			if (Widgets.ButtonText(tabRect, tab != null ? tab.LabelCap.ToString() : "?"))
			{
				ShowTabMenu();
			}

			Rect modeRect = new Rect(tabRect.xMax + 10f, inRect.y, 220f, 30f);
			string modeKey = layoutMode == TechTreeLayoutMode.PrerequisiteTiers
				? "ModTemplate.TechTree.Mode.Tiers"
				: "ModTemplate.TechTree.Mode.Vanilla";
			if (Widgets.ButtonText(modeRect, modeKey.Translate()))
			{
				layoutMode = layoutMode == TechTreeLayoutMode.PrerequisiteTiers
					? TechTreeLayoutMode.VanillaCoordinates
					: TechTreeLayoutMode.PrerequisiteTiers;
				scroll = Vector2.zero;
				dirty = true;
			}

			Rect summaryRect = new Rect(
				modeRect.xMax + 10f,
				inRect.y,
				Mathf.Max(0f, inRect.xMax - modeRect.xMax - 10f),
				30f);
			Widgets.Label(summaryRect, "ModTemplate.TechTree.Summary".Translate(
				tree?.Nodes.Count ?? 0,
				tab != null ? tab.LabelCap.ToString() : "?").ToString());
		}

		private void ShowTabMenu()
		{
			List<FloatMenuOption> options = DefDatabase<ResearchTabDef>.AllDefsListForReading
				.Where(item => item != null)
				.Select(item => new FloatMenuOption(item.LabelCap.ToString(), () =>
				{
					tab = item;
					scroll = Vector2.zero;
					dirty = true;
				}))
				.ToList();

			if (options.Count > 0)
			{
				Find.WindowStack.Add(new FloatMenu(options));
			}
		}

		private void MarkDirty()
		{
			dirty = true;
		}

		private void Rebuild()
		{
			try
			{
				tree = TechTree.Build(tab ?? ResearchTabDefOf.Main, layoutMode);
				dirty = false;
			}
			catch (Exception exception)
			{
				ModLog.Error("TechTree", "research tree build failed", exception);
				tree = null;
			}
		}
	}
}
