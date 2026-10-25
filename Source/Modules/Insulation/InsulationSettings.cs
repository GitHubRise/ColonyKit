using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>
	/// 多墙体保温设置：让墙体层数真正参与漏热计算。
	/// </summary>
	public class InsulationSettings : IColonyKitModuleSettings
	{
		public const float MinRatio = 0.5f;
		public const float MaxRatio = 10f;
		public const float DefaultRatio = 3f;

		public const int MinLayers = 1;
		public const int MaxLayers = 10;
		public const int DefaultLayers = 6;

		public string SettingsKey => "insulation";

		/// <summary>总开关；关闭时完全走原版结算（墙厚无效）。</summary>
		public bool enabled = true;

		/// <summary>墙体热阻与空气膜热阻之比 r：f(d) = (1 + r) / (1 + r * d)。</summary>
		public float wallResistanceRatio = DefaultRatio;

		/// <summary>最多计入多少层墙，超过后不再继续减漏。</summary>
		public int maxLayers = DefaultLayers;

		/// <summary>是否把自然岩层也算作墙体（山洞）。</summary>
		public bool includeNaturalRock;

		public void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "ck_insulation_enabled", true);
			Scribe_Values.Look(ref wallResistanceRatio, "ck_insulation_ratio", DefaultRatio);
			Scribe_Values.Look(ref maxLayers, "ck_insulation_maxLayers", DefaultLayers);
			Scribe_Values.Look(ref includeNaturalRock, "ck_insulation_naturalRock", false);
		}

		public bool Validate()
		{
			bool changed = false;

			if (wallResistanceRatio < MinRatio || wallResistanceRatio > MaxRatio)
			{
				wallResistanceRatio = DefaultRatio;
				changed = true;
			}

			if (maxLayers < MinLayers || maxLayers > MaxLayers)
			{
				maxLayers = DefaultLayers;
				changed = true;
			}

			if (changed)
			{
				ModLog.Warn("Insulation", "invalid insulation settings corrected.");
			}

			return changed;
		}

		public void Reset()
		{
			enabled = true;
			wallResistanceRatio = DefaultRatio;
			maxLayers = DefaultLayers;
			includeNaturalRock = false;
		}
	}

	/// <summary>取保温设置；设置根未就绪时返回 null，调用方按原版处理。</summary>
	public static class InsulationSettingsUtility
	{
		public static InsulationSettings Current =>
			ColonyKitMod.Settings?.GetModuleSettings<InsulationSettings>();
	}
}
