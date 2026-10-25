using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>「捕获蓝图」设计器：拉一个矩形，把范围内的玩家建筑存进蓝图库。</summary>
	public class Designator_BlueprintCapture : Designator
	{
		public Designator_BlueprintCapture()
		{
			defaultLabel = "CK.Blueprint.Capture.Label".Translate();
			defaultDesc = "CK.Blueprint.Capture.Desc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/Blueprint/Capture", false);
			soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
			soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
			soundSucceeded = SoundDefOf.Designate_ZoneAdd;
			useMouseIcon = true;
			isOrder = true;
		}

		/// <summary>拖框需要 DrawStyleCategory；借用原版的「实心矩形」样式。</summary>
		public override DrawStyleCategoryDef DrawStyleCategory =>
			DefDatabase<DrawStyleCategoryDef>.GetNamedSilentFail("CK_BlueprintCapture");

		public override bool DragDrawMeasurements => true;

		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			if (!loc.InBounds(Map))
			{
				return false;
			}
			if (loc.Fogged(Map))
			{
				return false;
			}
			return true;
		}

		public override void RenderHighlight(List<IntVec3> dragCells)
		{
			// 接受所有可见格，所以直接把拖框范围画出来；空地上也能看到选择区域。
			DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
		{
			List<IntVec3> list = cells?.ToList() ?? new List<IntVec3>();
			if (list.Count == 0)
			{
				Finalize(false);
				return;
			}

			CellRect rect = CellRect.FromLimits(list[0], list[0]);
			for (int index = 1; index < list.Count; index++)
			{
				rect = rect.Encapsulate(list[index]);
			}

			BlueprintLibrary library = BlueprintGameComponent.CurrentLibrary;
			if (library == null)
			{
				Messages.Message("CK.Blueprint.NoLibrary".Translate(), MessageTypeDefOf.RejectInput, false);
				Finalize(false);
				return;
			}

			BlueprintSettings settings = BlueprintSettingsUtility.Current;
			if (library.records.Count >= settings.maxRecords)
			{
				Messages.Message(
					"CK.Blueprint.Capture.LibraryFull".Translate(library.records.Count, settings.maxRecords),
					MessageTypeDefOf.RejectInput,
					false);
				Finalize(false);
				return;
			}

			BlueprintRecord record = BlueprintRecord.Capture(
				Map,
				rect,
				null,
				settings.maxSide,
				settings.maxEntries,
				out string error);

			if (record == null)
			{
				Messages.Message(error, MessageTypeDefOf.RejectInput, false);
				Finalize(false);
				return;
			}

			record.name = library.NextDefaultName();
			library.Add(record);

			Messages.Message(
				"CK.Blueprint.Capture.Success".Translate(
					record.name,
					record.sizeX,
					record.sizeZ,
					record.entries.Count),
				MessageTypeDefOf.PositiveEvent,
				false);

			if (settings.verboseLog)
			{
				ModLog.Info(
					"Blueprint",
					$"captured '{record.name}' {record.sizeX}x{record.sizeZ} at {rect.Min}, entries={record.entries.Count}.");
			}

			Finalize(true);
			Find.DesignatorManager.Deselect();
			MainTabWindow_Blueprints.OpenAndSelect(record);
		}
	}
}
