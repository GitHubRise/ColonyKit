using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 「气泡对话」设置页：显示条件、外观尺寸、颜色三组参数。
	/// </summary>
	/// <remarks>
	/// 所有文字都走 <c>.Loc()</c>，因此跟随「Mod 语言」设置在中/英之间切换
	/// （见 <see cref="ModLoc"/>）；数值改动只写内存，关闭窗口时统一落盘。
	/// </remarks>
	public sealed class BubblesSettingsPage : SettingsPage
	{
		private readonly string[] colorBuffers = new string[4];

		public override string Id => "bubbles";

		public override string TitleKey => "ColonyKit.Settings.Tab.Bubbles";

		public override int Order => 40;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			BubblesSettings data = settings.GetModuleSettings<BubblesSettings>();

			SettingsWidgets.Description(listing, "ColonyKit.Settings.Bubbles.Hint".Loc());
			if (BubblesFeature.ExternalModActive)
			{
				SettingsWidgets.Label(listing, "ColonyKit.Settings.Bubbles.Conflict".Loc());
			}

			listing.Gap();

			SettingsWidgets.FrameScope display = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Bubbles.Section.Display".Loc());

			data.enabled = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Bubbles.Enabled".Loc(),
				ref data.enabled,
				"ColonyKit.Settings.Bubbles.Enabled.Desc".Loc());

			bool colored = data.doTextColors;
			data.doTextColors = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Bubbles.DoTextColors".Loc(),
				ref data.doTextColors,
				"ColonyKit.Settings.Bubbles.DoTextColors.Desc".Loc());
			if (colored != data.doTextColors)
			{
				Bubbler.Rebuild();
			}

			data.doNonPlayer = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Bubbles.DoNonPlayer".Loc(),
				ref data.doNonPlayer,
				"ColonyKit.Settings.Bubbles.DoNonPlayer.Desc".Loc());

			data.doAnimals = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Bubbles.DoAnimals".Loc(),
				ref data.doAnimals,
				"ColonyKit.Settings.Bubbles.DoAnimals.Desc".Loc());

			data.doDrafted = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Bubbles.DoDrafted".Loc(),
				ref data.doDrafted,
				"ColonyKit.Settings.Bubbles.DoDrafted.Desc".Loc());

			data.hearingCheck = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Bubbles.HearingCheck".Loc(),
				ref data.hearingCheck,
				"ColonyKit.Settings.Bubbles.HearingCheck.Desc".Loc());

			if (data.hearingCheck)
			{
				data.hearingRange = SettingsWidgets.Slider(
					listing,
					"ColonyKit.Settings.Bubbles.HearingRange".Loc(),
					data.hearingRange,
					BubblesSettings.MinHearingRange,
					BubblesSettings.MaxHearingRange,
					1f,
					value => value.ToString("0"));
				SettingsWidgets.Description(listing, "ColonyKit.Settings.Bubbles.HearingRange.Desc".Loc());
			}

			SettingsWidgets.FrameEnd(listing, display);

			SettingsWidgets.FrameScope look = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Bubbles.Section.Appearance".Loc());

			data.fontSize = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.FontSize".Loc(),
				data.fontSize,
				BubblesSettings.MinFontSize,
				BubblesSettings.MaxFontSize,
				1f);

			data.paddingX = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.PaddingX".Loc(),
				data.paddingX,
				BubblesSettings.MinPadding,
				BubblesSettings.MaxPadding,
				1f);

			data.paddingY = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.PaddingY".Loc(),
				data.paddingY,
				BubblesSettings.MinPadding,
				BubblesSettings.MaxPadding,
				1f);

			data.widthMax = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.WidthMax".Loc(),
				data.widthMax,
				BubblesSettings.MinWidthMax,
				BubblesSettings.MaxWidthMax,
				4f);

			data.scaleMax = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Bubbles.ScaleMax".Loc(),
				data.scaleMax,
				BubblesSettings.MinScaleMax,
				BubblesSettings.MaxScaleMax,
				0.05f,
				value => value.ToStringPercent());

			data.altitudeBase = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.AltitudeBase".Loc(),
				data.altitudeBase,
				BubblesSettings.MinAltitudeBase,
				BubblesSettings.MaxAltitudeBase,
				1f);

			data.altitudeMax = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.AltitudeMax".Loc(),
				data.altitudeMax,
				BubblesSettings.MinAltitudeMax,
				BubblesSettings.MaxAltitudeMax,
				1f);

			data.pawnMax = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.PawnMax".Loc(),
				data.pawnMax,
				BubblesSettings.MinPawnMax,
				BubblesSettings.MaxPawnMax,
				1f);

			data.offsetSpacing = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.OffsetSpacing".Loc(),
				data.offsetSpacing,
				BubblesSettings.MinOffsetSpacing,
				BubblesSettings.MaxOffsetSpacing,
				1f);

			data.offsetStart = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.OffsetStart".Loc(),
				data.offsetStart,
				BubblesSettings.MinOffsetStart,
				BubblesSettings.MaxOffsetStart,
				2f);

			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Bubbles.OffsetDirection".Loc(),
				data.offsetDirection,
				new[] { 0, 1, 2, 3 },
				index => ("ColonyKit.Settings.Bubbles.Direction." + DirectionName(index)).Loc(),
				value => data.offsetDirection = value);

			data.opacityStart = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Bubbles.OpacityStart".Loc(),
				data.opacityStart,
				BubblesSettings.MinOpacityStart,
				BubblesSettings.MaxOpacityStart,
				0.05f,
				value => value.ToStringPercent());

			data.opacityHover = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Bubbles.OpacityHover".Loc(),
				data.opacityHover,
				BubblesSettings.MinOpacityHover,
				BubblesSettings.MaxOpacityHover,
				0.05f,
				value => value.ToStringPercent());

			data.fadeStart = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.FadeStart".Loc(),
				data.fadeStart,
				BubblesSettings.MinFadeStart,
				BubblesSettings.MaxFadeStart,
				50f);

			data.fadeLength = SliderInt(
				listing,
				"ColonyKit.Settings.Bubbles.FadeLength".Loc(),
				data.fadeLength,
				BubblesSettings.MinFadeLength,
				BubblesSettings.MaxFadeLength,
				50f);

			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Bubbles.AutoHideSpeed".Loc(),
				data.autoHideSpeed,
				new[] { 1, 2, 3, 4 },
				value => AutoHideLabel(value).Loc(),
				value => data.autoHideSpeed = value);

			SettingsWidgets.Description(listing, "ColonyKit.Settings.Bubbles.AutoHideSpeed.Desc".Loc());

			SettingsWidgets.FrameEnd(listing, look);

			SettingsWidgets.FrameScope colors = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Bubbles.Section.Colors".Loc());

			ColorRow(
				listing,
				"ColonyKit.Settings.Bubbles.Color.Background".Loc(),
				ref colorBuffers[0],
				ref data.background);

			ColorRow(
				listing,
				"ColonyKit.Settings.Bubbles.Color.Foreground".Loc(),
				ref colorBuffers[1],
				ref data.foreground);

			ColorRow(
				listing,
				"ColonyKit.Settings.Bubbles.Color.BackgroundSelected".Loc(),
				ref colorBuffers[2],
				ref data.backgroundSelected);

			ColorRow(
				listing,
				"ColonyKit.Settings.Bubbles.Color.ForegroundSelected".Loc(),
				ref colorBuffers[3],
				ref data.foregroundSelected);

			SettingsWidgets.FrameEnd(listing, colors);
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.GetModuleSettings<BubblesSettings>().Reset();

			for (int i = 0; i < colorBuffers.Length; i++)
			{
				colorBuffers[i] = null;
			}

			Bubbler.Rebuild();
		}

		private static int SliderInt(
			Listing_Standard listing,
			string label,
			int value,
			int min,
			int max,
			float step)
		{
			float result = SettingsWidgets.Slider(
				listing,
				label,
				value,
				min,
				max,
				step,
				shown => Mathf.RoundToInt(shown).ToString());

			return Mathf.Clamp(Mathf.RoundToInt(result), min, max);
		}

		private static string DirectionName(int index)
		{
			switch (index)
			{
				case 1:
					return "Right";
				case 2:
					return "Up";
				case 3:
					return "Left";
				default:
					return "Down";
			}
		}

		private static string AutoHideLabel(int speed)
		{
			switch (speed)
			{
				case 2:
					return "ColonyKit.Settings.Bubbles.AutoHideSpeed.Fast";
				case 3:
					return "ColonyKit.Settings.Bubbles.AutoHideSpeed.Faster";
				case 4:
					return "ColonyKit.Settings.Bubbles.AutoHideSpeed.Fastest";
				default:
					return "ColonyKit.Settings.Bubbles.AutoHideSpeed.Off";
			}
		}

		/// <summary>
		/// 颜色行：左侧名称，右侧「#RRGGBB 输入框 + 色块预览」；输入非法时画红框并保留旧颜色。
		/// </summary>
		private static void ColorRow(Listing_Standard listing, string label, ref string buffer, ref Color color)
		{
			Rect rect = listing.GetRect(Text.LineHeight);
			Rect labelRect = rect.LeftHalf();
			Rect right = rect.RightHalf().Rounded();
			Rect fieldRect = right.LeftHalf().Rounded();
			Rect previewRect = right.RightHalf().Rounded();

			Widgets.Label(labelRect, label);

			Widgets.DrawBoxSolid(previewRect, color);
			Widgets.DrawBox(previewRect);

			buffer = Widgets.TextField(fieldRect, buffer ?? ColorUtility.ToHtmlStringRGB(color));

			Color parsed = color;
			bool valid = buffer != null
				&& buffer.Length == 6
				&& ColorUtility.TryParseHtmlString("#" + buffer, out parsed);

			if (valid)
			{
				parsed.a = 1f;
				color = parsed;
			}
			else
			{
				Color previous = GUI.color;
				GUI.color = Color.red;
				Widgets.DrawBox(fieldRect);
				GUI.color = previous;
			}
		}
	}
}
