#nullable disable

﻿using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using rjw.MainTab.DefModExtensions;
using System;
using rjw.RJWUI;

namespace rjw.MainTab;

public class MainTabWindow : MainTabWindow_PawnTable
{
	private static List<PawnTableDef> allTables;
	private static List<PawnTableDef> AllTables => allTables ??=
		DefDatabase<PawnTableDef>.AllDefs
			.Where(d => d.HasModExtension<RJW_PawnTable>())
			.ToList();

	protected override float ExtraBottomSpace => 53f;
	protected override float ExtraTopSpace => 40f;
	protected override PawnTableDef PawnTableDef => pawnTableDef;
	protected override IEnumerable<Pawn> Pawns => pawns;

	public IEnumerable<Pawn> pawns;
	public PawnTableDef pawnTableDef = AllTables.FirstOrDefault();

	private static IEnumerable<Pawn> DefaultPawns =>
		Find.CurrentMap.mapPawns.AllPawns
			.Where(p => p.RaceProps.Humanlike && p.IsColonist && !xxx.is_slave(p));

	/// <summary>
	/// 绘制表格
	/// </summary>
	/// <param name="rect"></param>
	public override void DoWindowContents(Rect rect)
	{
		base.DoWindowContents(rect);

		// 标签页尺寸
		float tabHeight = 32f;
		float tabY = rect.y + 5f;
		float tabSpacing = 5f;

		List<PawnTableDef> tables = AllTables;

		float availableWidth = (rect.width - (2 * tabSpacing));
		float tabWidth = (availableWidth - (tables.Count - 1) * tabSpacing) / tables.Count;
		float startX = rect.x + tabSpacing;

		// 绘制每个标签页
		for (int i = 0; i < tables.Count; i++)
		{
			PawnTableDef tableDef = tables[i];
			bool isSelected = pawnTableDef == tableDef;

			Rect tabRect = new(
				startX + i * (tabWidth + tabSpacing),
				tabY,
				tabWidth,
				tabHeight
			);

			// 绘制标签页背景（选中时高亮）
			Widgets.DrawOptionBackground(tabRect, isSelected);

			if (Widgets.ButtonInvisible(tabRect, true))
			{
				SwitchToTable(tableDef);
			}

			using (new UiScope(anchor: TextAnchor.MiddleCenter))
			{
				Widgets.LabelFit(tabRect, tableDef.GetModExtension<RJW_PawnTable>().label);
			}
		}
	}

	public override void PostOpen()
	{
		pawns = GetFilteredPawns(pawnTableDef);
		base.PostOpen();
		Find.World.renderer.wantedMode = WorldRenderMode.None;
	}

	[Obsolete("No longer necessary")]
	public List<FloatMenuOption> MakeOptions()
	{
		return [];
	}

	/// <summary>
	/// 重新加载/更新标签页
	/// </summary>
	public static void Reloadtab()
	{
		var rjwtab = DefDatabase<MainButtonDef>.GetNamed("RJW_MainButton");
		Find.MainTabsRoot.ToggleTab(rjwtab, false);//关闭
		Find.MainTabsRoot.ToggleTab(rjwtab, false);//开启
	}

	/// <summary>
	/// 切换到指定表格
	/// </summary>
	private void SwitchToTable(PawnTableDef tableDef)
	{
		pawnTableDef = tableDef;
		pawns = GetFilteredPawns(tableDef);
		Notify_ResolutionChanged();
		Reloadtab();
	}

	private static IEnumerable<Pawn> GetFilteredPawns(PawnTableDef tableDef)
	{
		var filterDef = tableDef?.GetModExtension<RJW_PawnTable>()?.pawnFilter;
		return filterDef?.Filter(Find.CurrentMap.mapPawns.AllPawnsSpawned) ?? DefaultPawns;
	}
}
