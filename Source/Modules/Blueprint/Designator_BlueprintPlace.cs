using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>
	/// 「放置蓝图」设计器：鼠标处显示整张蓝图的幽灵预览，Q/E 旋转、B 镜像、左键放置。
	/// </summary>
	/// <remarks>
	/// 放置是原子的：只要有一条不能放（研究/占用/地形/材料），整张蓝图都不落单，
	/// 避免出现缺墙少门的半成品；缺 Def 的条目会跳过并在面板/消息里提示。
	/// </remarks>
	public class Designator_BlueprintPlace : Designator
	{
		private readonly BlueprintRecord record;

		private readonly List<BlueprintPlacement> placements = new List<BlueprintPlacement>();

		private int rotation;
		private bool mirrored;

		private bool hasCache;
		private IntVec3 cachedMouse;
		private int cachedRotation;
		private bool cachedMirrored;
		private BlueprintMaterialMode cachedMode;
		private Map cachedMap;
		private bool cachedCanPlace;
		private string cachedFailure;
		private List<IntVec3> cachedFootprint = new List<IntVec3>();
		private List<ThingDefCountClass> cachedCosts = new List<ThingDefCountClass>();
		private int cachedMissing;
		private int cachedSubstituted;

		public Designator_BlueprintPlace(BlueprintRecord record)
		{
			this.record = record;
			defaultLabel = "CK.Blueprint.Place.Label".Translate();
			defaultDesc = "CK.Blueprint.Place.Desc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/Blueprint/Place", false);
			soundSucceeded = SoundDefOf.Designate_PlaceBuilding;
			useMouseIcon = false;
			isOrder = true;
		}

		public override bool CanRemainSelected()
		{
			return record != null
				&& BlueprintGameComponent.CurrentLibrary?.records.Contains(record) == true;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			if (record == null)
			{
				return "CK.Blueprint.NoRecord".Translate();
			}
			if (Map == null || !loc.InBounds(Map))
			{
				return false;
			}

			EnsurePlacements(loc);
			return cachedCanPlace
				? AcceptanceReport.WasAccepted
				: new AcceptanceReport(cachedFailure ?? "CK.Blueprint.Place.Blocked".Translate());
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			if (record == null || Map == null || !c.InBounds(Map))
			{
				Finalize(false);
				return;
			}

			// 点击时强制重算：连续放置后地图上已经多了蓝图/框架，不能沿用上一份缓存。
			hasCache = false;
			EnsurePlacements(c);
			if (!cachedCanPlace)
			{
				Messages.Message(
					cachedFailure ?? "CK.Blueprint.Place.Blocked".Translate(),
					MessageTypeDefOf.RejectInput,
					false);
				Finalize(false);
				return;
			}

			int placed = BlueprintPlacementUtility.PlaceAll(placements, Map, out int skipped, out int substituted);
			// 落地后地图已变；下一帧/下一次点击必须重新校验，避免在同一格重复叠蓝图。
			hasCache = false;
			cachedFootprint.Clear();
			cachedCosts.Clear();

			if (placed > 0)
			{
				IntVec3 anchor = BlueprintPlacementUtility.AnchorFor(c, record, rotation);
				IntVec2 size = BlueprintPlacementUtility.RotatedSize(record, rotation);
				FleckMaker.ThrowMetaPuffs(new CellRect(anchor.x, anchor.z, size.x, size.z), Map);

				string successKey = DebugSettings.godMode
					? "CK.Blueprint.Place.Built"
					: "CK.Blueprint.Place.Success";
				Messages.Message(
					successKey.Translate(record.name, placed),
					MessageTypeDefOf.PositiveEvent,
					false);
			}

			if (skipped > 0)
			{
				Messages.Message(
					"CK.Blueprint.Place.Skipped".Translate(skipped),
					MessageTypeDefOf.NegativeEvent,
					false);
			}

			if (substituted > 0)
			{
				Messages.Message(
					"CK.Blueprint.Place.Substituted".Translate(substituted),
					MessageTypeDefOf.NeutralEvent,
					false);
			}

			BlueprintSettings settings = BlueprintSettingsUtility.Current;
			if (settings.verboseLog && placed > 0)
			{
				ModLog.Info(
					"Blueprint",
					$"placed '{record.name}' {record.sizeX}x{record.sizeZ} at {c}, " +
					$"rot={rotation * 90}, mirrored={mirrored}, placed={placed}, skipped={skipped}, substituted={substituted}.");
			}

			Finalize(placed > 0);
		}

		public override void SelectedUpdate()
		{
			if (record == null || Map == null)
			{
				return;
			}

			IntVec3 mouse = UI.MouseCell();
			if (!mouse.InBounds(Map))
			{
				return;
			}

			EnsurePlacements(mouse);

			Color color = cachedCanPlace
				? Designator_Place.CanPlaceColor
				: Designator_Place.CannotPlaceColor;

			foreach (BlueprintPlacement placement in placements)
			{
				if (placement.def == null || !placement.report.Accepted)
				{
					continue;
				}

				GhostDrawer.DrawGhostThing(
					placement.position,
					placement.rotation,
					placement.def,
					null,
					color,
					AltitudeLayer.Blueprint,
					null,
					false,
					placement.stuff);
			}

			if (cachedFootprint.Count > 0)
			{
				GenDraw.DrawFieldEdges(cachedFootprint, color);
			}
		}

		public override void SelectedProcessInput(Event ev)
		{
			if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
			{
				Rotate(1);
			}
			if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
			{
				Rotate(-1);
			}

			KeyBindingDef mirrorKey = DefDatabase<KeyBindingDef>.GetNamedSilentFail("CK_BlueprintMirror");
			if (mirrorKey != null && mirrorKey.KeyDownEvent)
			{
				mirrored = !mirrored;
				hasCache = false;
				SoundDefOf.DragSlider.PlayOneShotOnCamera();
			}
		}

		public override void DoExtraGuiControls(float leftX, float bottomY)
		{
			// 复用原版旋转按钮（Q/E），鼠标玩家不需要记快捷键。
			DesignatorUtility.GUIDoRotationControls(leftX, bottomY, new Rot4(rotation), rot =>
			{
				rotation = rot.AsInt;
				hasCache = false;
			});
		}

		public override void DrawPanelReadout(ref float curY, float width)
		{
			Text.Font = GameFont.Small;
			float lineHeight = Text.LineHeight;

			Widgets.Label(
				new Rect(0f, curY, width, lineHeight * 2f),
				"CK.Blueprint.Panel.Header".Translate(record?.name ?? "", record?.sizeX ?? 0, record?.sizeZ ?? 0));
			curY += lineHeight * 2f + 2f;

			string mirrorText = mirrored ? "CK.Blueprint.Panel.Mirrored".Translate() : "";
			Widgets.Label(
				new Rect(0f, curY, width, lineHeight),
				"CK.Blueprint.Panel.Rotation".Translate(rotation * 90, mirrorText));
			curY += lineHeight + 2f;

			BlueprintSettings settings = BlueprintSettingsUtility.Current;
			Widgets.Label(
				new Rect(0f, curY, width, lineHeight),
				"CK.Blueprint.Panel.Mode".Translate(ModeLabel(settings.materialMode)));
			curY += lineHeight + 2f;

			if (cachedMissing > 0)
			{
				GUI.color = Color.yellow;
				Widgets.Label(
					new Rect(0f, curY, width, lineHeight),
					"CK.Blueprint.Panel.Missing".Translate(cachedMissing));
				GUI.color = Color.white;
				curY += lineHeight + 2f;
			}

			if (!cachedCanPlace && !cachedFailure.NullOrEmpty())
			{
				GUI.color = Color.red;
				Widgets.Label(new Rect(0f, curY, width, lineHeight * 2f), cachedFailure);
				GUI.color = Color.white;
				curY += lineHeight * 2f + 2f;
			}

			Map map = Map;
			int lines = 0;
			foreach (ThingDefCountClass cost in cachedCosts)
			{
				if (lines >= 4)
				{
					break;
				}

				Rect iconRect = new Rect(0f, curY, 20f, 20f);
				Widgets.ThingIcon(iconRect, cost.thingDef);

				int have = BlueprintMaterialPolicy.CountOnMap(map, cost.thingDef);
				GUI.color = have < cost.count ? Color.red : Color.white;
				Widgets.Label(
					new Rect(24f, curY, width - 24f, lineHeight),
					"CK.Blueprint.Panel.Material".Translate(cost.thingDef.LabelCap, cost.count, have));
				GUI.color = Color.white;

				curY += 22f;
				lines++;
			}

			if (cachedSubstituted > 0 && lines < 5)
			{
				Widgets.Label(
					new Rect(0f, curY, width, lineHeight),
					"CK.Blueprint.Panel.Substituted".Translate(cachedSubstituted));
				curY += lineHeight;
			}

			curY += 4f;
		}

		public override void Deselected()
		{
			hasCache = false;
			cachedFootprint.Clear();
			cachedCosts.Clear();
		}

		private void Rotate(int delta)
		{
			rotation = GenMath.PositiveMod(rotation + delta, 4);
			hasCache = false;
			SoundDefOf.DragSlider.PlayOneShotOnCamera();
		}

		private void EnsurePlacements(IntVec3 mouse)
		{
			BlueprintSettings settings = BlueprintSettingsUtility.Current;
			BlueprintMaterialMode mode = settings.materialMode;
			Map map = Map;

			if (hasCache
				&& cachedMouse == mouse
				&& cachedRotation == rotation
				&& cachedMirrored == mirrored
				&& cachedMode == mode
				&& cachedMap == map)
			{
				return;
			}

			cachedMouse = mouse;
			cachedRotation = rotation;
			cachedMirrored = mirrored;
			cachedMode = mode;
			cachedMap = map;
			hasCache = true;

			BlueprintPlacementUtility.Build(
				record,
				mouse,
				rotation,
				mirrored,
				map,
				mode,
				placements,
				out cachedCanPlace,
				out cachedFailure,
				out cachedFootprint);

			cachedCosts = BlueprintPlacementUtility.TotalCost(placements);
			cachedMissing = 0;
			cachedSubstituted = 0;

			foreach (BlueprintPlacement placement in placements)
			{
				if (placement.def == null)
				{
					cachedMissing++;
				}
				else if (placement.substituted)
				{
					cachedSubstituted++;
				}
			}
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
