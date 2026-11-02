using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 「气泡对话」设置：小人社交时在头顶显示带文字的对话气泡。
	/// </summary>
	/// <remarks>
	/// 数据来源是 Interaction Bubbles 4.2（作者 Jaxe，packageId <c>Jaxe.Bubbles</c>）的
	/// <c>Settings</c> 静态字段；这里改成 <see cref="IColonyKitModuleSettings"/> 实例，
	/// 由 <see cref="ColonyKitSettings.moduleSettings"/> 统一持久化，键名加 <c>ck_bubbles_</c> 前缀。
	/// 原 Mod 的「显示气泡」开关是运行时状态，这里也持久化，重开游戏不会自己恢复。
	/// </remarks>
	public sealed class BubblesSettings : IColonyKitModuleSettings
	{
		public const int AutoHideSpeedOff = 1;

		public const float MinHearingRange = 1f;
		public const float MaxHearingRange = 32f;
		public const float DefaultHearingRange = 10f;

		public const int MinAltitudeBase = 3;
		public const int MaxAltitudeBase = 44;
		public const int DefaultAltitudeBase = 11;

		public const int MinAltitudeMax = 20;
		public const int MaxAltitudeMax = 60;
		public const int DefaultAltitudeMax = 40;

		public const float MinScaleMax = 1f;
		public const float MaxScaleMax = 5f;
		public const float DefaultScaleMax = 1.25f;

		public const int MinPawnMax = 1;
		public const int MaxPawnMax = 15;
		public const int DefaultPawnMax = 3;

		public const int MinFontSize = 5;
		public const int MaxFontSize = 30;
		public const int DefaultFontSize = 12;

		public const int MinPadding = 1;
		public const int MaxPadding = 40;
		public const int DefaultPaddingX = 7;
		public const int DefaultPaddingY = 5;

		public const int MinWidthMax = 100;
		public const int MaxWidthMax = 500;
		public const int DefaultWidthMax = 256;

		public const int MinOffsetSpacing = 2;
		public const int MaxOffsetSpacing = 12;
		public const int DefaultOffsetSpacing = 2;

		public const int MinOffsetStart = 0;
		public const int MaxOffsetStart = 400;
		public const int DefaultOffsetStart = 14;

		public const int MinOffsetDirection = 0;
		public const int MaxOffsetDirection = 3;
		public const int DefaultOffsetDirection = 0;

		public const float MinOpacityStart = 0.3f;
		public const float MaxOpacityStart = 1f;
		public const float DefaultOpacityStart = 0.9f;

		public const float MinOpacityHover = 0.05f;
		public const float MaxOpacityHover = 1f;
		public const float DefaultOpacityHover = 0.2f;

		public const int MinFadeStart = 100;
		public const int MaxFadeStart = 5000;
		public const int DefaultFadeStart = 500;

		public const int MinFadeLength = 50;
		public const int MaxFadeLength = 2500;
		public const int DefaultFadeLength = 100;

		public string SettingsKey => "bubbles";

		/// <summary>模块总开关；关闭后补丁仍在，但不再记录/绘制气泡。</summary>
		public bool enabled = true;

		/// <summary>游戏内「显示气泡」图标的状态；关闭时清空当前气泡。</summary>
		public bool activated = true;

		/// <summary>显示其他派系（非玩家阵营）的互动。</summary>
		public bool doNonPlayer = true;

		/// <summary>显示动物互动。</summary>
		public bool doAnimals = true;

		/// <summary>显示被征召的小人。</summary>
		public bool doDrafted;

		/// <summary>保留互动文字自带的颜色标签。</summary>
		public bool doTextColors;

		/// <summary>只显示玩家控制的小人能听到的互动（实验性）。</summary>
		public bool hearingCheck;

		/// <summary>听觉判定的最大距离（格）。</summary>
		public float hearingRange = DefaultHearingRange;

		/// <summary>游戏速度达到该档时自动隐藏；1 表示不隐藏。</summary>
		public int autoHideSpeed = AutoHideSpeedOff;

		public int altitudeBase = DefaultAltitudeBase;
		public int altitudeMax = DefaultAltitudeMax;
		public float scaleMax = DefaultScaleMax;
		public int pawnMax = DefaultPawnMax;

		public int fontSize = DefaultFontSize;
		public int paddingX = DefaultPaddingX;
		public int paddingY = DefaultPaddingY;
		public int widthMax = DefaultWidthMax;

		public int offsetSpacing = DefaultOffsetSpacing;
		public int offsetStart = DefaultOffsetStart;

		/// <summary>气泡相对小人的方向：0 下、1 右、2 上、3 左（对应 Rot4 的 int 值）。</summary>
		public int offsetDirection = DefaultOffsetDirection;

		public float opacityStart = DefaultOpacityStart;
		public float opacityHover = DefaultOpacityHover;

		public int fadeStart = DefaultFadeStart;
		public int fadeLength = DefaultFadeLength;

		public Color background = Color.white;
		public Color foreground = Color.black;
		public Color backgroundSelected = new Color(1f, 1f, 0.75f);
		public Color foregroundSelected = Color.black;

		public void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "ck_bubbles_enabled", true);
			Scribe_Values.Look(ref activated, "ck_bubbles_activated", true);
			Scribe_Values.Look(ref doNonPlayer, "ck_bubbles_doNonPlayer", true);
			Scribe_Values.Look(ref doAnimals, "ck_bubbles_doAnimals", true);
			Scribe_Values.Look(ref doDrafted, "ck_bubbles_doDrafted", false);
			Scribe_Values.Look(ref doTextColors, "ck_bubbles_doTextColors", false);
			Scribe_Values.Look(ref hearingCheck, "ck_bubbles_hearingCheck", false);
			Scribe_Values.Look(ref hearingRange, "ck_bubbles_hearingRange", DefaultHearingRange);
			Scribe_Values.Look(ref autoHideSpeed, "ck_bubbles_autoHideSpeed", AutoHideSpeedOff);
			Scribe_Values.Look(ref altitudeBase, "ck_bubbles_altitudeBase", DefaultAltitudeBase);
			Scribe_Values.Look(ref altitudeMax, "ck_bubbles_altitudeMax", DefaultAltitudeMax);
			Scribe_Values.Look(ref scaleMax, "ck_bubbles_scaleMax", DefaultScaleMax);
			Scribe_Values.Look(ref pawnMax, "ck_bubbles_pawnMax", DefaultPawnMax);
			Scribe_Values.Look(ref fontSize, "ck_bubbles_fontSize", DefaultFontSize);
			Scribe_Values.Look(ref paddingX, "ck_bubbles_paddingX", DefaultPaddingX);
			Scribe_Values.Look(ref paddingY, "ck_bubbles_paddingY", DefaultPaddingY);
			Scribe_Values.Look(ref widthMax, "ck_bubbles_widthMax", DefaultWidthMax);
			Scribe_Values.Look(ref offsetSpacing, "ck_bubbles_offsetSpacing", DefaultOffsetSpacing);
			Scribe_Values.Look(ref offsetStart, "ck_bubbles_offsetStart", DefaultOffsetStart);
			Scribe_Values.Look(ref offsetDirection, "ck_bubbles_offsetDirection", DefaultOffsetDirection);
			Scribe_Values.Look(ref opacityStart, "ck_bubbles_opacityStart", DefaultOpacityStart);
			Scribe_Values.Look(ref opacityHover, "ck_bubbles_opacityHover", DefaultOpacityHover);
			Scribe_Values.Look(ref fadeStart, "ck_bubbles_fadeStart", DefaultFadeStart);
			Scribe_Values.Look(ref fadeLength, "ck_bubbles_fadeLength", DefaultFadeLength);
			Scribe_Values.Look(ref background, "ck_bubbles_background", Color.white);
			Scribe_Values.Look(ref foreground, "ck_bubbles_foreground", Color.black);
			Scribe_Values.Look(ref backgroundSelected, "ck_bubbles_backgroundSelected", new Color(1f, 1f, 0.75f));
			Scribe_Values.Look(ref foregroundSelected, "ck_bubbles_foregroundSelected", Color.black);
		}

		public bool Validate()
		{
			bool changed = false;

			changed |= ClampInt(ref autoHideSpeed, AutoHideSpeedOff, 4, AutoHideSpeedOff);
			changed |= ClampInt(ref altitudeBase, MinAltitudeBase, MaxAltitudeBase, DefaultAltitudeBase);
			changed |= ClampInt(ref altitudeMax, MinAltitudeMax, MaxAltitudeMax, DefaultAltitudeMax);
			changed |= ClampInt(ref pawnMax, MinPawnMax, MaxPawnMax, DefaultPawnMax);
			changed |= ClampInt(ref fontSize, MinFontSize, MaxFontSize, DefaultFontSize);
			changed |= ClampInt(ref paddingX, MinPadding, MaxPadding, DefaultPaddingX);
			changed |= ClampInt(ref paddingY, MinPadding, MaxPadding, DefaultPaddingY);
			changed |= ClampInt(ref widthMax, MinWidthMax, MaxWidthMax, DefaultWidthMax);
			changed |= ClampInt(ref offsetSpacing, MinOffsetSpacing, MaxOffsetSpacing, DefaultOffsetSpacing);
			changed |= ClampInt(ref offsetStart, MinOffsetStart, MaxOffsetStart, DefaultOffsetStart);
			changed |= ClampInt(ref offsetDirection, MinOffsetDirection, MaxOffsetDirection, DefaultOffsetDirection);
			changed |= ClampInt(ref fadeStart, MinFadeStart, MaxFadeStart, DefaultFadeStart);
			changed |= ClampInt(ref fadeLength, MinFadeLength, MaxFadeLength, DefaultFadeLength);
			changed |= ClampFloat(ref hearingRange, MinHearingRange, MaxHearingRange, DefaultHearingRange);
			changed |= ClampFloat(ref scaleMax, MinScaleMax, MaxScaleMax, DefaultScaleMax);
			changed |= ClampFloat(ref opacityStart, MinOpacityStart, MaxOpacityStart, DefaultOpacityStart);
			changed |= ClampFloat(ref opacityHover, MinOpacityHover, MaxOpacityHover, DefaultOpacityHover);

			if (changed)
			{
				ModLog.Warn("Bubbles", "invalid bubble settings corrected.");
			}

			return changed;
		}

		public void Reset()
		{
			enabled = true;
			activated = true;
			doNonPlayer = true;
			doAnimals = true;
			doDrafted = false;
			doTextColors = false;
			hearingCheck = false;
			hearingRange = DefaultHearingRange;
			autoHideSpeed = AutoHideSpeedOff;
			altitudeBase = DefaultAltitudeBase;
			altitudeMax = DefaultAltitudeMax;
			scaleMax = DefaultScaleMax;
			pawnMax = DefaultPawnMax;
			fontSize = DefaultFontSize;
			paddingX = DefaultPaddingX;
			paddingY = DefaultPaddingY;
			widthMax = DefaultWidthMax;
			offsetSpacing = DefaultOffsetSpacing;
			offsetStart = DefaultOffsetStart;
			offsetDirection = DefaultOffsetDirection;
			opacityStart = DefaultOpacityStart;
			opacityHover = DefaultOpacityHover;
			fadeStart = DefaultFadeStart;
			fadeLength = DefaultFadeLength;
			background = Color.white;
			foreground = Color.black;
			backgroundSelected = new Color(1f, 1f, 0.75f);
			foregroundSelected = Color.black;
		}

		private static bool ClampInt(ref int value, int min, int max, int fallback)
		{
			int clamped = value < min || value > max ? fallback : value;
			if (clamped == value)
			{
				return false;
			}

			value = clamped;
			return true;
		}

		private static bool ClampFloat(ref float value, float min, float max, float fallback)
		{
			float clamped = float.IsNaN(value) || value < min || value > max ? fallback : value;
			if (Mathf.Approximately(clamped, value))
			{
				return false;
			}

			value = clamped;
			return true;
		}
	}

	/// <summary>取气泡设置；设置根未就绪时返回 null，调用方按「不显示」处理。</summary>
	public static class BubblesSettingsUtility
	{
		public static BubblesSettings Current =>
			ColonyKitMod.Settings?.GetModuleSettings<BubblesSettings>();
	}
}
