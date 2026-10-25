using UnityEngine;
using Verse;
using RimWorld;
using rjw.RJWUI;

namespace rjw.MainTab;

[StaticConstructorOnStartup]
public class Dialog_DesignateHero : Window
{
	private const float InnerMargin = 17f;
	private const float IconSize = 32f;
	private const float ButtonHeight = 30f;

	private readonly Pawn pawn;
	private static Texture2D? _heroIcon;
	private static Texture2D HeroIcon => _heroIcon ??= ContentFinder<Texture2D>.Get("UI/Commands/Hero_off");

	public override Vector2 InitialSize => new(420f, 300f);

	public Dialog_DesignateHero(Pawn pawn)
	{
		this.pawn = pawn;
		soundClose = SoundDefOf.InfoCard_Close;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
		forcePause = true;
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

	public override void DoWindowContents(Rect inRect)
	{
		bool shouldClose = HandleCloseInput();

		Rect rect = inRect.ContractedBy(InnerMargin);
		float y = rect.y;

		// 标题行：图标 + 文本
		Rect iconRect = new(rect.x, y, IconSize, IconSize);
		GUI.DrawTexture(iconRect, HeroIcon);

		Rect titleRect = new(iconRect.xMax + 8f, y, rect.width - IconSize - 8f, IconSize);
		using (new UiScope(font: GameFont.Medium, anchor: TextAnchor.MiddleLeft))
		{
			Widgets.Label(titleRect, "RJW_Hero_DesignateDialog_Title".Translate());
		}
		y += IconSize + 16f;

		// 中间内容区域
		float buttonsAreaHeight = ButtonHeight + 10f;
		float midAreaHeight = rect.y + rect.height - y - buttonsAreaHeight;

		Rect midRect = new(rect.x, y, rect.width, midAreaHeight);
		midRect.SplitVertically(midRect.width / 2f, out Rect leftMidRect, out Rect rightMidRect);

		// 左侧：描述 + 说明
		leftMidRect.SplitHorizontally(leftMidRect.height / 2f, out Rect descRect, out Rect explainRect);
		using (new UiScope(anchor: TextAnchor.UpperLeft))
		{
			Widgets.Label(descRect, "RJW_Hero_DesignateDialog_Desc".Translate());
		}

		TaggedString explanation = RJWSettings.RPG_hero_control_Ironman
			? "RJW_Hero_DesignateDialog_Text_Ironman".Translate()
			: "RJW_Hero_DesignateDialog_Text_Normal".Translate();
		using (new UiScope(font: GameFont.Small))
		{
			Widgets.Label(explainRect, explanation);
		}

		// 右侧：头像 + 姓名
		float nameHeight = 22f;
		float portraitPadding = 10f;
		float portraitSize = Mathf.Min(
			rightMidRect.width - portraitPadding * 2f,
			rightMidRect.height - nameHeight - 4f
		);

		Rect portraitRect = new(
			rightMidRect.x + (rightMidRect.width - portraitSize) / 2f,
			rightMidRect.y,
			portraitSize,
			portraitSize
		);

		using (new UiScope(color: Command.LowLightBgColor))
		{
			Widgets.DrawBox(portraitRect);
		}
		using (new UiScope(color: Color.white))
		{
			GUI.DrawTexture(portraitRect, Command.BGTex);
		}

		RenderTexture portrait = PortraitsCache.Get(
			pawn,
			new Vector2(128f, 180f),
			Rot4.South,
			Vector3.zero,
			1f,
			supersample: true,
			compensateForUIScale: true,
			renderHeadgear: true,
			renderClothes: false
		);
		Widgets.DrawTextureFitted(portraitRect, portrait, 1f);

		Rect nameRect = new(
			rightMidRect.x,
			portraitRect.yMax + 4f,
			rightMidRect.width,
			nameHeight
		);
		using (new UiScope(font: GameFont.Small, anchor: TextAnchor.UpperCenter))
		{
			Widgets.LabelFit(nameRect, pawn.LabelShort);
		}

		// 按钮行
		y = rect.y + rect.height - ButtonHeight - 5f;
		float buttonWidth = 100f;
		float buttonSpacing = 20f;
		float buttonsWidth = buttonWidth * 2 + buttonSpacing;
		float buttonsX = rect.x + (rect.width - buttonsWidth) / 2f;

		Rect yesRect = new(buttonsX, y, buttonWidth, ButtonHeight);
		using (new UiScope(color: Color.green))
		{
			if (Widgets.ButtonText(yesRect, "Yes".Translate()))
			{
				pawn.ToggleHero();
				Close();
				MainTabWindow.Reloadtab();
			}
		}

		Rect noRect = new(buttonsX + buttonWidth + buttonSpacing, y, buttonWidth, ButtonHeight);
		if (Widgets.ButtonText(noRect, "No".Translate()))
		{
			Close();
			MainTabWindow.Reloadtab();
		}

		if (Widgets.CloseButtonFor(inRect) || shouldClose)
		{
			Close();
			MainTabWindow.Reloadtab();
		}
	}
}

