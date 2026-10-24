using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 底部「模板演示」按钮的抽屉：贴着底栏向上展开，不暂停、不吸收窗口外的输入，
	/// 因此打开时仍然可以拖动地图；再次点击底部按钮或右上角 X 关闭。
	/// </summary>
	public sealed class DemoDrawerWindow : Window
	{
		private const float BottomBarHeight = 35f;
		private const float SlideDuration = 0.18f;

		private float slideStartY;
		private float slideTargetY;
		private float slideStartedAt = -1f;

		public DemoDrawerWindow()
		{
			doCloseX = true;
			doCloseButton = false;
			closeOnClickedOutside = false;
			closeOnAccept = false;
			closeOnCancel = false;
			forcePause = false;
			preventCameraMotion = false;
			absorbInputAroundWindow = false;
			drawShadow = false;
		}

		public override Vector2 InitialSize => new Vector2(UI.screenWidth, 240f);

		protected override void SetInitialSizeAndPosition()
		{
			base.SetInitialSizeAndPosition();

			slideTargetY = UI.screenHeight - BottomBarHeight - InitialSize.y;
			slideStartY = slideTargetY + 36f;
			windowRect = new Rect(0f, slideStartY, UI.screenWidth, InitialSize.y);
			slideStartedAt = Time.time;
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();

			if (slideStartedAt < 0f)
			{
				return;
			}

			float progress = Mathf.Clamp01((Time.time - slideStartedAt) / SlideDuration);
			windowRect.y = Mathf.Lerp(slideStartY, slideTargetY, progress);
			if (progress >= 1f)
			{
				slideStartedAt = -1f;
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "ModTemplate.DemoDrawer.Title".Loc());

			Text.Font = GameFont.Small;
			Widgets.Label(
				new Rect(inRect.x, inRect.y + 38f, inRect.width, 56f),
				"ModTemplate.DemoDrawer.Text".Loc());

			if (Widgets.ButtonText(
				new Rect(inRect.x, inRect.yMax - 34f, 180f, 30f),
				"ModTemplate.DemoDrawer.OpenResearchTree".Loc()))
			{
				Find.WindowStack.Add(new TechTreeWindow());
			}

			if (Widgets.ButtonText(
				new Rect(inRect.xMax - 120f, inRect.yMax - 34f, 120f, 30f),
				"ModTemplate.DemoDrawer.Close".Loc()))
			{
				Close();
			}
		}
	}
}
