#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Multiplayer.API;
using rjw.Modules.Interactions;
using rjw.RJWUI;
using rjw.RJWUI.RJWidgets;

namespace rjw;

/// <summary>
/// 供外部模组向 Sexcard 对话框添加自定义绘制区域的注册表。
/// 回调绘制在性取向行与头像区域之间。
/// </summary>
public static class SexcardCallbackRegistry
{
	private static readonly List<CallbackEntry> _callbacks = new();

	private readonly struct CallbackEntry
	{
		public readonly string Label;
		public readonly Action<Listing_Standard, Pawn> Draw;

		public CallbackEntry(string label, Action<Listing_Standard, Pawn> draw)
		{
			Label = label;
			Draw = draw;
		}
	}

	/// <summary>
	/// 注册一个绘制回调，它会在性取向行与头像之间渲染。
	/// </summary>
	/// <param name="label">内容区块的标题标签</param>
	/// <param name="draw">绘制函数，接收用于绘制的 Listing_Standard</param>
	/// <returns>如果添加了回调则返回 true，否则返回 false</returns>
	public static bool RegisterDrawCallback(string label, Action<Listing_Standard> draw)
	{
		return RegisterDrawCallback(label, (listing, _) => draw?.Invoke(listing));
	}

	/// <summary>
	/// 注册一个绘制回调，它会在性取向行与头像之间渲染。
	/// 这个重载还会提供正在显示的小人。
	/// </summary>
	/// <param name="label">内容区块的标题标签</param>
	/// <param name="draw">绘制函数，接收 Listing_Standard 和当前 Pawn</param>
	/// <returns>如果添加了回调则返回 true，否则返回 false</returns>
	public static bool RegisterDrawCallback(string label, Action<Listing_Standard, Pawn> draw)
	{
		if (string.IsNullOrEmpty(label))
		{
			ModLog.Error("SexcardCallbackRegistry: Cannot register callback with null or empty label");
			return false;
		}
		if (draw == null)
		{
			ModLog.Error($"SexcardCallbackRegistry: Cannot register null callback for label '{label}'");
			return false;
		}

		// 移除同名的现有回调以便更新
		_callbacks.RemoveAll(c => c.Label == label);
		_callbacks.Add(new CallbackEntry(label, draw));

		return true;
	}

	/// <summary>
	/// 注销先前注册的绘制回调。
	/// </summary>
	/// <param name="label">要移除的回调的标签</param>
	/// <returns>若移除了回调则返回 true，否则返回 false</returns>
	public static bool UnregisterDrawCallback(string label)
	{
		return _callbacks.RemoveAll(c => c.Label == label) > 0;
	}

	/// <summary>
	/// 清除所有已注册的回调。适用于模组重载的场景。
	/// </summary>
	public static void ClearAllCallbacks()
	{
		_callbacks.Clear();
	}

	internal static void DrawAllCallbacks(Listing_Standard listing, Pawn pawn)
	{
		foreach (var callback in _callbacks)
		{
			try
			{
				listing.GapLine(6f);
				using (new UiScope(font: GameFont.Medium))
				{
					listing.Label(callback.Label);
				}
				listing.Gap(4f);
				using (new UiScope())
				{
					callback.Draw(listing, pawn);
				}
			}
			catch (Exception ex)
			{
				ModLog.Error($"Error in Sexcard draw callback '{callback.Label}': {ex}");
			}
		}
	}

	internal static bool HasCallbacks => _callbacks.Count > 0;
	internal static int CallbackCount => _callbacks.Count;
}

/// <summary>
/// 用于编辑 RJW 部件的通用对话框窗口。
/// </summary>
public class Dialog_PartcardNG : Window
{
	private new const float Margin = 17f;
	private const float RowHeight = 24f;
	private const float SliderHeight = 30f;

	private readonly Pawn pawn;
	private readonly List<ISexPartHediff> parts;
	private readonly string label;
	private int selectedIndex;

	private static readonly StringBuilder stringBuilder = new();

	public override Vector2 InitialSize => new(600f, 750f);

	public Dialog_PartcardNG(Pawn pawn, List<ISexPartHediff> parts, string label)
	{
		this.pawn = pawn;
		this.parts = parts;
		this.label = label;
		this.selectedIndex = 0;

		// 窗口设置
		soundClose = SoundDefOf.InfoCard_Close;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = false;
		forcePause = true;
		preventCameraMotion = false;
	}

	public override void DoWindowContents(Rect inRect)
	{
		bool shouldClose = HandleCloseInput();

		Rect windowRect = inRect.ContractedBy(Margin);
		Rect mainRect = new(windowRect.x, windowRect.y, windowRect.width, windowRect.height);

		DrawMainContent(mainRect);

		using (new UiScope(font: GameFont.Small))
		{
			if (Widgets.CloseButtonFor(inRect) || shouldClose)
			{
				Close();
			}
		}
	}

	private bool HandleCloseInput()
	{
		if (Event.current.type == EventType.KeyDown &&
			(Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape))
		{
			Event.current.Use();
			return true;
		}
		return false;
	}

	private void DrawMainContent(Rect rect)
	{
		bool devMode = RJWSettings.DevMode || Prefs.DevMode;

		Text.Font = GameFont.Medium;
		Rect titleRect = new(8f, 4f, rect.width - 8f, 30f);
		Widgets.Label(titleRect, "PartsEditLabel".Translate(label));

		Text.Font = GameFont.Tiny;
		float yOffset = titleRect.yMax + 10f;

		Rect partSelectorRect = new(10f, yOffset, rect.width, RowHeight);
		DrawPartSelector(partSelectorRect);
		yOffset += RowHeight + 10f;

		if (selectedIndex >= parts.Count)
		{
			selectedIndex = 0;
		}

		var part = parts[selectedIndex];
		var comp = part.AsHediff.TryGetComp<HediffComp_SexPart>();
		if (comp == null) return;

		Rect sizeSliderRect = new(10f, yOffset, rect.width, SliderHeight);
		DrawSizeSlider(sizeSliderRect, part, comp);
		yOffset += SliderHeight + 5f;

		Rect fluidSliderRect = new(10f, yOffset, rect.width, SliderHeight);
		DrawFluidMultiplierSlider(fluidSliderRect, comp, devMode);
		yOffset += SliderHeight + 5f;

		if (devMode)
		{
			Rect fluidTypeRect = new(10f, yOffset, rect.width, RowHeight + 24f);
			DrawFluidTypeDropdown(fluidTypeRect, comp);
			yOffset += RowHeight + 30f;
		}

		Rect tooltipRect = new(10f, yOffset, rect.width, rect.yMax - yOffset - 10f);
		DrawPartTooltip(tooltipRect, comp);
	}

	private void DrawPartSelector(Rect rect)
	{
		bool hasDuplicates = parts.GroupBy(x => x.Def).Any(g => g.Count() > 1);

		string GetPartLabel(int index)
		{
			var partLabel = parts[index].AsHediff.LabelBaseCap;
			return hasDuplicates ? $"{partLabel} ({index + 1})" : partLabel;
		}

		List<Widgets.DropdownMenuElement<ISexPartHediff>> GetDropdownMenu(int _)
		{
			var menu = new List<Widgets.DropdownMenuElement<ISexPartHediff>>();
			for (int i = 0; i < parts.Count; i++)
			{
				int index = i;
				menu.Add(new Widgets.DropdownMenuElement<ISexPartHediff>
				{
					payload = parts[index],
					option = new FloatMenuOption(GetPartLabel(index), () => selectedIndex = index)
				});
			}
			return menu;
		}

		Widgets.Dropdown(rect, selectedIndex, i => parts[i], GetDropdownMenu, GetPartLabel(selectedIndex));
	}

	private static void DrawSizeSlider(Rect rect, ISexPartHediff part, HediffComp_SexPart comp)
	{
		var def = part.Def;
		float minSeverity = def.stages.First().minSeverity;
		float maxSeverity = def.stages.Last().minSeverity;
		float currentSeverity = comp.GetSeverity();

		float newSeverity = Widgets.HorizontalSlider(
			rect, currentSeverity, minSeverity, maxSeverity,
			label: part.AsHediff.LabelInBrackets,
			leftAlignedLabel: $"{minSeverity:F2}",
			rightAlignedLabel: $"{maxSeverity:F2}"
		);

		if (!Mathf.Approximately(newSeverity, currentSeverity))
		{
			comp.SetSeverity(newSeverity);
		}
	}

	private static void DrawFluidMultiplierSlider(Rect rect, HediffComp_SexPart comp, bool devMode)
	{
		float currentFluid = comp.partFluidMultiplier;
		float maxFluid = devMode ? (currentFluid < 10f ? 10f : 100f) : 2f;

		float newFluid = Widgets.HorizontalSlider(
			rect, currentFluid, 0f, maxFluid,
			label: "PartsEditFluidMultiplierLabel".Translate(),
			leftAlignedLabel: "0.00",
			rightAlignedLabel: $"{maxFluid:F2}"
		);

		if (!Mathf.Approximately(newFluid, currentFluid))
		{
			comp.partFluidMultiplier = newFluid;
		}
	}

	private static void DrawFluidTypeDropdown(Rect rect, HediffComp_SexPart comp)
	{
		var fluids = DefDatabase<SexFluidDef>.AllDefs.ToList();
		fluids.Insert(0, null);

		string GetFluidLabel(SexFluidDef fluid)
		{
			return fluid?.LabelCap ?? "None".Translate();
		}

		string GetDropdownLabel(int index)
		{
			var fluid = fluids[index];
			var label = GetFluidLabel(fluid);
			return fluid == comp.Def.fluid ? label + "SC_FluidTypeDefault".Translate() : label;
		}

		List<Widgets.DropdownMenuElement<SexFluidDef>> GetDropdownMenu(int _)
		{
			var menu = new List<Widgets.DropdownMenuElement<SexFluidDef>>();
			for (int i = 0; i < fluids.Count; i++)
			{
				int index = i;
				menu.Add(new Widgets.DropdownMenuElement<SexFluidDef>
				{
					payload = fluids[index],
					option = new FloatMenuOption(GetDropdownLabel(index), () => comp.Fluid = fluids[index])
				});
			}
			return menu;
		}

		using (new UiScope(font: GameFont.Small, anchor: TextAnchor.UpperCenter))
		{
			Rect labelRect = new(rect.x, rect.y, rect.width, RowHeight);
			Widgets.Label(labelRect, "SC_FluidType".Translate());
		}

		Rect dropdownRect = new(rect.x, rect.y + RowHeight, rect.width, RowHeight);
		int currentIndex = fluids.IndexOf(comp.Fluid);
		Widgets.Dropdown(dropdownRect, currentIndex, i => fluids[i], GetDropdownMenu, GetFluidLabel(comp.Fluid));
	}

	private void DrawPartTooltip(Rect rect, HediffComp_SexPart comp)
	{
		stringBuilder.Clear();

		foreach (string line in comp.GetTipString(extended: true))
		{
			stringBuilder.AppendLine(line);
		}

		stringBuilder.AppendLine("RJW_PartInfo_severity".Translate($"{comp.GetSeverity():F2}"));
		stringBuilder.AppendLine();

		if (!RJWSettings.DevMode)
		{
			stringBuilder.AppendLine("RJW_PartInfo_fluidMultiplier".Translate($"{comp.partFluidMultiplier:F2}"));
		}

		var scrollPosition = Vector2.zero;
		using (new UiScope(font: GameFont.Small))
		{
			Widgets.LabelScrollable(rect, stringBuilder.ToString(), ref scrollPosition);
		}
	}
}

/// <summary>
/// 性向卡片对话框窗口。
/// </summary>
[StaticConstructorOnStartup]
public class Dialog_SexcardNG : Window
{
	private new const float Margin = 17f;
	private const float TitleHeight = 40f;
	private const float RowHeight = 24f;
	private const float BottomSectionHeight = 184f; // 部件编辑：24f * 4，控件：24 * 2，间隔：8，边距：8 * 4
	private const int FillableBarBorderWidth = 3;

	private readonly Pawn pawn;
	private Vector2 scrollPosition;
	private static Rot4 currentRotation = Rot4.South;

	private float HeightModifier = 0f;

	public override Vector2 InitialSize => new(600f, 750f);

	private static readonly Texture2D sexFrequencyBarColor = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.2f, 0.2f));
	private static readonly Texture2D vulnerabilityBarColor = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.2f, 0.2f));
	private static readonly Texture2D sexSatisfactionBarColor = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.2f, 0.2f));
	private static readonly Texture2D sexWildnessAversionBarColor = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.2f, 0.2f));
	private static readonly Texture2D fertilityBarColor = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.5f, 0.2f));

	public Dialog_SexcardNG(Pawn editFor)
	{
		pawn = editFor;

		// 窗口设置
		soundClose = SoundDefOf.InfoCard_Close;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = false;
		forcePause = true;
		preventCameraMotion = false;
	}

	public override void DoWindowContents(Rect inRect)
	{
		bool shouldClose = HandleCloseInput();

		Rect windowRect = inRect.ContractedBy(Margin);
		Rect mainRect = new(windowRect.x, windowRect.y, windowRect.width, windowRect.height);
		//Widgets.DrawBoxSolid(mainRect, Color.black);

		DrawSexualityCard(mainRect, pawn);

		if (Widgets.CloseButtonFor(inRect) || shouldClose)
		{
			Close();
		}
	}

	private bool HandleCloseInput()
	{
		if (Event.current.type == EventType.KeyDown &&
			(Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape))
		{
			Event.current.Use();
			return true;
		}
		return false;
	}

	private void DrawSexualityCard(Rect rect, Pawn pawn)
	{
		CompRJW comp = pawn.GetCompRJW();
		if (pawn == null || comp == null) return;

		Rect contentRect = new(rect.x, rect.y, rect.width, rect.height);
		//Widgets.DrawBoxSolid(contentRect, Color.blue);
		contentRect.SplitVertically(contentRect.width * 0.66f, out Rect leftRect, out Rect rightRect);

		Rect titleRect = new(rect.x, rect.y, rect.width, TitleHeight);
		//Widgets.DrawBoxSolid(titleRect, Color.white);
		DrawTitle(titleRect);

		float yOffset = titleRect.yMax + 8f;

		Rect genderRect = new(rect.x, yOffset, leftRect.width, RowHeight);
		using (new UiScope(font: GameFont.Small))
		{
			DrawGender(genderRect, pawn);
		}

		yOffset += genderRect.height + 8f;

		Rect sexualityRect = new(rect.x, yOffset, leftRect.width, RowHeight);
		//Widgets.DrawBoxSolid(sexualityRect, Color.magenta);
		using (new UiScope(font: GameFont.Small))
		{
			DrawSexuality(sexualityRect, pawn);
		}

		yOffset += sexualityRect.height + 8f;

		Rect kinseyRect = new(rect.x, yOffset, leftRect.width, 80f);
		//Widgets.DrawBoxSolid(kinseyRect, Color.black);
		DrawKinsey(kinseyRect);

		Rect statsRect = new(sexualityRect.xMax + 8f, titleRect.yMax + 8f, rightRect.width, genderRect.height + sexualityRect.height + kinseyRect.height);
		//Widgets.DrawBoxSolid(statsRect, Color.green);

		DrawStats(statsRect);

		yOffset += Math.Max(statsRect.height, kinseyRect.height) + 8f;

		// 计算可滚动内容的可用空间
		float bottomSectionY = contentRect.height - BottomSectionHeight;
		float scrollableHeight = bottomSectionY - yOffset;

		// 可滚动的中间区域（自定义回调）
		Rect scrollOuterRect = new(rect.x, yOffset, contentRect.width, scrollableHeight);
		//Widgets.DrawBoxSolid(scrollOuterRect, Color.gray);
		DrawMiddleSection(scrollOuterRect, pawn);

		//Rect bottomRect = new(rect.x, scrollOuterRect.y + scrollOuterRect.height, contentRect.width, BottomSectionHeight);
		Rect bottomRect = new(rect.x, contentRect.yMax - BottomSectionHeight, contentRect.width, BottomSectionHeight);
		//Widgets.DrawBoxSolid(bottomRect, Color.red);
		DrawBottomSection(bottomRect, pawn);
	}

	private static void DrawTitle(Rect rect)
	{
		float logoSize = 32f;
		float logoTitleSeparation = 4f;
		float titleWidth;

		using (new UiScope(font: GameFont.Medium, anchor: TextAnchor.UpperCenter, color: Color.yellow))
		{
			Widgets.Label(rect, "SC_title".Translate());
			titleWidth = Text.CalcSize("SC_title".Translate()).x;
		}

		Rect logoLeftRect = new((rect.width / 2f) - (titleWidth / 2f) - logoTitleSeparation - logoSize, rect.y, logoSize, logoSize);
		GUI.DrawTexture(logoLeftRect, ContentFinder<Texture2D>.Get("RJW-LOGO"));
		Rect logoRightRect = new((rect.width / 2f) + (titleWidth / 2f) + logoTitleSeparation + logoSize, rect.y, logoSize, logoSize);
		GUI.DrawTexture(logoRightRect, ContentFinder<Texture2D>.Get("RJW-LOGO"));
	}

	private void DrawStats(Rect rect)
	{
		float barHeight = RowHeight + 2 * FillableBarBorderWidth;
		Rect sexFrequencyRect = new(rect.x, rect.y, rect.width, barHeight);
		Rect vulnerabilityRect = new(rect.x, sexFrequencyRect.yMax + 4f, rect.width, barHeight);
		Rect sexSatisfactionRect = new(rect.x, vulnerabilityRect.yMax + 4f, rect.width, barHeight);
		Rect sexWildnessAversionRect = new(rect.x, sexSatisfactionRect.yMax + 4f, rect.width, barHeight);
		Rect fertilityRect = new(rect.x, sexWildnessAversionRect.yMax + 4f, rect.width, barHeight);

		StatFillableBarLabeled(
			sexFrequencyRect,
			RJWStatDefOf.SexFrequency,
			sexFrequencyBarColor,
			Texture2D.blackTexture
		);
		StatFillableBarLabeled(
			vulnerabilityRect,
			RJWStatDefOf.Vulnerability,
			vulnerabilityBarColor,
			Texture2D.blackTexture
		);
		StatFillableBarLabeled(
			sexSatisfactionRect,
			RJWStatDefOf.SexSatisfaction,
			sexSatisfactionBarColor,
			Texture2D.blackTexture
		);
		StatFillableBarLabeled(
			sexWildnessAversionRect,
			RJWStatDefOf.SexWildnessAversion,
			sexWildnessAversionBarColor,
			Texture2D.blackTexture
		);
		if (StatDefOf.Fertility != null)
		{
			StatFillableBarLabeled(
				fertilityRect,
				StatDefOf.Fertility,
				fertilityBarColor,
				Texture2D.blackTexture
			);
		}
	}

	private void StatFillableBarLabeled(Rect rect, StatDef stat, Texture2D filltexture, Texture2D bgtexture)
	{
		float statValue;
		try
		{
			statValue = pawn.GetStatValue(stat);
		}
		catch (NullReferenceException ex)
		{
			// 小人未被写入属性数据，对非人形生物/尸体而言是错误。
			var msg = $"Pawn {xxx.get_pawnname(pawn)} does not have {stat}: {ex}";
			ModLog.WarningOnce(msg, msg.GetHashCode());
			GUI.DrawTexture(rect, BaseContent.BlackTex);
			using (new UiScope(anchor: TextAnchor.MiddleCenter))
			{
				Widgets.LabelFit(rect, "SC_StatUnavailable".Translate());
			}
			return;
		}

		Widgets.FillableBar(rect, Math.Min(statValue, 1.0f), filltexture, bgtexture, true);
		Rect labelRect = rect.ContractedBy(FillableBarBorderWidth);
		using (new UiScope(anchor: TextAnchor.MiddleLeft))
		{
			Widgets.Label(labelRect, $" {stat.LabelCap.CapitalizeFirst()}");
		}
		using (new UiScope(anchor: TextAnchor.MiddleRight))
		{
			Widgets.Label(labelRect, $"{statValue.ToStringPercent()} ");
		}
		Widgets.DrawHighlightIfMouseover(rect);
		string statDescription = stat.description;
		if (!statDescription.NullOrEmpty()) TooltipHandler.TipRegion(rect, statDescription);
	}

	private void DrawMiddleSection(Rect rect, Pawn pawn)
	{
		//Widgets.DrawBoxSolid(outerRect, Color.blue);

		Rect scrollRect = new(rect.x, rect.y, rect.width, rect.height);
		Rect viewRect = new(0f, 0f, rect.width, rect.height + HeightModifier);
		Widgets.AdjustRectsForScrollView(rect, ref scrollRect, ref viewRect);

		if (SexcardCallbackRegistry.HasCallbacks)
		{
			Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

			Listing_Standard listing = new();
			listing.Begin(viewRect);

			SexcardCallbackRegistry.DrawAllCallbacks(listing, pawn);
			listing.Gap(8f);

			listing.End();
			HeightModifier = Math.Max(0, listing.MaxColumnHeightSeen/* - scrollRect.height*//* - outerRect.height*/);
			Widgets.EndScrollView();
		}
	}

	private void DrawBottomSection(Rect rect, Pawn pawn)
	{
		rect.SplitVertically(rect.width / 2f, out Rect leftRect, out Rect rightRect);

		float portraitSize = Math.Min(leftRect.width, BottomSectionHeight);
		Rect portraitRect = new(leftRect.x, leftRect.y, portraitSize, portraitSize);
		using (new UiScope(color: Command.LowLightBgColor))
		{
			Widgets.DrawBox(portraitRect);
		}
		using (new UiScope(color: Color.white))
		{
			GUI.DrawTexture(portraitRect, Command.BGTex);
		}

		using (new UiScope(font: GameFont.Small))
		{
			DrawPortrait(portraitRect, pawn);
		}
		if (Widgets.ButtonImage(portraitRect.ContractedBy(8).RightPartPixels(16).TopPartPixels(16), TexUI.RotRightTex))
			currentRotation.Rotate(RotationDirection.Counterclockwise);
		if (Widgets.InfoCardButtonWorker(portraitRect.ContractedBy(8).LeftPartPixels(16).TopPartPixels(16)))
			Find.WindowStack.Add(new Dialog_InfoCard(pawn));

		// 右侧的按钮
		Rect pawnEditRect = rightRect.ContractedBy(8);
		Widgets.DrawBoxSolid(rightRect, new Color(64f / 255f, 65f / 255f, 66f / 255f));

		float buttonHeight = 24f;
		Rect partEditRect = new(pawnEditRect.x, pawnEditRect.y, pawnEditRect.width, buttonHeight * 4);
		Rect controlRect = new(pawnEditRect.x, partEditRect.yMax + 8f, pawnEditRect.width, buttonHeight * 2);
		DrawPartEditButtons(partEditRect.ContractedBy(8), pawn, buttonHeight);
		DrawControlButtons(controlRect.ContractedBy(8), pawn, buttonHeight);
	}

	private void DrawPartEditButtons(Rect rect, Pawn pawn, float buttonHeight)
	{
		if (MP.IsInMultiplayer)
			return;

		if (!pawn.IsColonistPlayerControlled && !pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony && !xxx.is_animal(pawn) &&
			!RJWSettings.DevMode && Current.ProgramState == ProgramState.Playing)
			return;

		var pawnParts = pawn.GetLewdParts();
		pawnParts.PopulateAll();

		Rect breastButton = new(rect.x, rect.y, rect.width, buttonHeight);
		Rect vaginaButton = new(rect.x, rect.y + buttonHeight, rect.width, buttonHeight);
		Rect anusButton = new(rect.x, rect.y + buttonHeight * 2, rect.width, buttonHeight);
		Rect penisButton = new(rect.x, rect.y + buttonHeight * 3, rect.width, buttonHeight);

		MakePartButton(breastButton, pawn, pawnParts.Breasts.AsSexPartHediffs(), HediffDef.Named("Breasts").LabelCap);
		MakePartButton(vaginaButton, pawn, pawnParts.Vaginas.AsSexPartHediffs(), HediffDef.Named("Vagina").LabelCap);
		MakePartButton(anusButton, pawn, pawnParts.Anuses.AsSexPartHediffs(), HediffDef.Named("Anus").LabelCap);
		MakePartButton(penisButton, pawn, pawnParts.Penises.AsSexPartHediffs(), HediffDef.Named("Penis").LabelCap);
	}

	private void DrawControlButtons(Rect rect, Pawn pawn, float buttonHeight)
	{
		float yOffset = 0f;

		// 远古科技生育力开关
		var genitalBPR = Genital_Helper.get_genitalsBPR(pawn);
		var parts = Genital_Helper.get_PartsHediffList(pawn, genitalBPR)?.Cast<ISexPartHediff>() ?? Enumerable.Empty<ISexPartHediff>();
		bool hasArchotechBits = false;
		foreach (var part in parts)
		{
			if (part.Def.partTags.Contains("FertilityToggle"))
			{
				hasArchotechBits = true;
				break;
			}
		}

		Rect archotechButton = new(rect.x, rect.y + yOffset, rect.width, buttonHeight);
		DrawFertilityToggleButton(archotechButton, pawn, hasArchotechBits);
		yOffset += buttonHeight;

		// 重新随机性向按钮（开发者模式或角色创建时）
		if (RJWSettings.DevMode || Current.ProgramState != ProgramState.Playing)
		{
			Rect resexualizeButton = new(rect.x, rect.y + yOffset, rect.width, buttonHeight);
			string buttonLabel = Current.ProgramState != ProgramState.Playing
				? "SC_Reroll".Translate()
				: $"[DEV] {"SC_Reroll".Translate()}";

			if (Widgets.ButtonText(resexualizeButton, buttonLabel))
			{
				ReSexualize(pawn);
			}
		}
	}

	private static void DrawFertilityToggleButton(Rect rect, Pawn pawn, bool isClickable)
	{
		bool hasBlocker = pawn.health.hediffSet.HasHediff(HediffDef.Named("ImpregnationBlocker"));
		bool hasEnhancer = pawn.health.hediffSet.HasHediff(HediffDef.Named("FertilityEnhancer"));

		string buttonLabel;
		if (hasBlocker)
			buttonLabel = "SC_Fertility_Enable".Translate();
		else if (!hasEnhancer)
			buttonLabel = "SC_Fertility_Enhance".Translate();
		else
			buttonLabel = "SC_Fertility_Disable".Translate();

		var buttonStyle = isClickable ? new UiScope(color: new Color(0f, 1f, 0f)) : new UiScope(color: new Color(0.7f, 0.7f, 0.7f));

		using (buttonStyle)
		{
			if (Widgets.ButtonText(rect, buttonLabel) && isClickable)
			{
				ChangeFertility(pawn);
			}
		}
	}

	private static void DrawGender(Rect row, Pawn pawn)
	{
		var genderIcon = pawn.gender.GetIcon();
		var genderLabel = pawn.gender.GetLabel(pawn.IsAnimal()).CapitalizeFirst();

		row.SplitVertically(row.width / 2.5f, out Rect leftRect, out Rect rightRect);
		rightRect.SplitVertically(rightRect.height, out Rect iconRect, out Rect labelRect);

		Widgets.LabelFit(leftRect, "SC_Gender".Translate().Colorize(Color.yellow));
		Widgets.DrawTextureFitted(iconRect, genderIcon, 1);
		Widgets.LabelFit(labelRect, $" {genderLabel}");
		if (Mouse.IsOver(row))
			Widgets.DrawHighlight(row);
	}

	private static void DrawSexuality(Rect row, Pawn pawn)
	{
		row.SplitVertically(row.width / 2.5f, out Rect leftRect, out Rect rightRect);

		string sexuality;
		switch (pawn.GetCompRJW().orientation)
		{
			case Orientation.Asexual:
				sexuality = "SC_Sexuality_Asexual".Translate();
				break;
			case Orientation.Bisexual:
				sexuality = "SC_Sexuality_Bisexual".Translate();
				break;
			case Orientation.Heterosexual:
				sexuality = "SC_Sexuality_Heterosexual".Translate();
				break;
			case Orientation.Homosexual:
				sexuality = "SC_Sexuality_Homosexual".Translate();
				break;
			case Orientation.LeaningHeterosexual:
				sexuality = "SC_Sexuality_LeaningHeterosexual".Translate();
				break;
			case Orientation.LeaningHomosexual:
				sexuality = "SC_Sexuality_LeaningHomosexual".Translate();
				break;
			case Orientation.MostlyHeterosexual:
				sexuality = "SC_Sexuality_MostlyHeterosexual".Translate();
				break;
			case Orientation.MostlyHomosexual:
				sexuality = "SC_Sexuality_MostlyHomosexual".Translate();
				break;
			case Orientation.Pansexual:
				sexuality = "SC_Sexuality_Pansexual".Translate();
				break;
			default:
				sexuality = "None".Translate();
				break;
		}

		bool canEdit = RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.RimJobWorld &&
			((Current.ProgramState == ProgramState.Playing && pawn.IsDesignatedHero() && pawn.IsHeroOwner()) ||
			 Prefs.DevMode ||
			 Current.ProgramState == ProgramState.Entry);

		Widgets.LabelFit(leftRect, "SC_Sexuality".Translate().Colorize(Color.yellow));
		if (canEdit)
		{
			if (Widgets.ButtonText(rightRect, sexuality, drawBackground: true))
			{
				Find.WindowStack.Add(new FloatMenu(GetOrientationMenuOptions(pawn)));
			}
		}
		else
		{
			Widgets.LabelFit(rightRect, sexuality);
		}
		if (Mouse.IsOver(row))
			Widgets.DrawHighlight(row);
	}


	private static List<FloatMenuOption> GetOrientationMenuOptions(Pawn pawn)
	{
		return
		[
			new FloatMenuOption("SC_Sexuality_Asexual".Translate(), () => ChangeOrientation(pawn, Orientation.Asexual)),
			new FloatMenuOption("SC_Sexuality_Pansexual".Translate(), () => ChangeOrientation(pawn, Orientation.Pansexual)),
			new FloatMenuOption("SC_Sexuality_Heterosexual".Translate(), () => ChangeOrientation(pawn, Orientation.Heterosexual)),
			new FloatMenuOption("SC_Sexuality_MostlyHeterosexual".Translate(), () => ChangeOrientation(pawn, Orientation.MostlyHeterosexual)),
			new FloatMenuOption("SC_Sexuality_LeaningHeterosexual".Translate(), () => ChangeOrientation(pawn, Orientation.LeaningHeterosexual)),
			new FloatMenuOption("SC_Sexuality_Bisexual".Translate(), () => ChangeOrientation(pawn, Orientation.Bisexual)),
			new FloatMenuOption("SC_Sexuality_LeaningHomosexual".Translate(), () => ChangeOrientation(pawn, Orientation.LeaningHomosexual)),
			new FloatMenuOption("SC_Sexuality_MostlyHomosexual".Translate(), () => ChangeOrientation(pawn, Orientation.MostlyHomosexual)),
			new FloatMenuOption("SC_Sexuality_Homosexual".Translate(), () => ChangeOrientation(pawn, Orientation.Homosexual)),
		];
	}

	private static void DrawPortrait(Rect rect, Pawn pawn)
	{
		RenderTexture portrait = PortraitsCache.Get(
			pawn,
			new Vector2(128f, 180f),
			currentRotation,
			Vector3.zero,
			1f,
			supersample: true,
			compensateForUIScale: true,
			renderHeadgear: true,
			renderClothes: false
		);
		Widgets.DrawTextureFitted(rect, portrait, 1f);
	}

	private void DrawKinsey(Rect rect)
	{
		bool allowKinseyEdits = RJWSettings.DevMode || Current.ProgramState != ProgramState.Playing;

		using (new UiScope(font: GameFont.Small))
		{
			Rect kinseyTitleRect = new Rect(rect.x, rect.y, rect.width, Text.LineHeight);
			using (new UiScope(color: Color.yellow))
			{
				Widgets.Label(kinseyTitleRect, "SC_Kinsey".Translate());
				TooltipHandler.TipRegion(kinseyTitleRect, "SC_Kinsey_desc".Translate());
			}
			Rect kinseyScaleRect = new Rect(rect.x, kinseyTitleRect.yMax, rect.width, 80f);

			DrawKinseyScale(kinseyScaleRect, pawn, allowKinseyEdits);
		}
	}

	private void DrawKinseyScale(Rect rect, Pawn pawn, bool allowEdits)
	{
		int currentKinseyValue = pawn.GetCompRJW().orientation.ToKinseyRating();

		Rect kinseyRect = new(rect.x, rect.y + 10f, rect.width, 60f);

		int newValue = KinseyScaleDrawer.DrawKinseyScale(kinseyRect, currentKinseyValue, interactive: true);
		if (newValue != currentKinseyValue && allowEdits)
		{
			ChangeOrientation(pawn, OrientationExtensions.FromKinseyRating(newValue));
		}
	}

	[SyncMethod]
	private static void ChangeOrientation(Pawn pawn, Orientation orientation)
	{
		pawn.GetCompRJW().orientation = orientation;
		CompRJW.UpdateOrientation(pawn);
	}

	[SyncMethod]
	private static void ChangeFertility(Pawn pawn)
	{
		BodyPartRecord genitalia = Genital_Helper.get_genitalsBPR(pawn);
		Hediff blocker = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ImpregnationBlocker"));
		Hediff enhancer = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FertilityEnhancer"));

		if (blocker != null)
		{
			pawn.health.RemoveHediff(blocker);
		}
		else if (enhancer == null)
		{
			pawn.health.AddHediff(HediffDef.Named("FertilityEnhancer"), genitalia);
		}
		else
		{
			pawn.health.RemoveHediff(enhancer);
			pawn.health.AddHediff(HediffDef.Named("ImpregnationBlocker"), genitalia);
		}
	}

	[SyncMethod]
	private static void ReSexualize(Pawn pawn)
	{
		pawn.GetCompRJW().Sexualize(true);
	}

	private static void MakePartButton(Rect rect, Pawn pawn, List<ISexPartHediff> parts, string label)
	{
		var editableParts = parts
			.Where(part => part.Def.partTags.Contains("Resizable") ||
						  RJWSettings.DevMode ||
						  Current.ProgramState != ProgramState.Playing)
			.ToList();

		bool isClickable = editableParts.Count > 0;

		using (isClickable ? new UiScope() : new UiScope(color: new Color(0.7f, 0.7f, 0.7f)))
		{
			if (Widgets.ButtonText(rect, "PartsEditLabel".Translate(label)) && isClickable)
			{
				Find.WindowStack.Add(new Dialog_PartcardNG(pawn, editableParts, label));
			}
		}
	}
}
