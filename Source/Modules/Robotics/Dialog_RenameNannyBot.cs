using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>保姆机器人改名窗口；只改 NameSingle，不影响 PawnKind。</summary>
	public class Dialog_RenameNannyBot : Window
	{
		private readonly Pawn pawn;
		private string curName;

		public override Vector2 InitialSize => new Vector2(320f, 180f);

		public Dialog_RenameNannyBot(Pawn pawn)
		{
			this.pawn = pawn;
			curName = pawn.Name?.ToStringFull ?? pawn.LabelShort;
			doCloseX = true;
			forcePause = true;
			absorbInputAroundWindow = true;
			closeOnAccept = false;
			closeOnClickedOutside = true;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0f, 0f, inRect.width, 32f), "Rename".Translate());
			Text.Font = GameFont.Small;

			curName = Widgets.TextField(new Rect(0f, 42f, inRect.width, 35f), curName, 28);

			if (Widgets.ButtonText(new Rect(0f, inRect.height - 35f, 140f, 35f), "OK".Translate()))
			{
				if (curName.NullOrEmpty())
				{
					Messages.Message(
						"CK.Robotics.Name.Invalid".Translate(),
						MessageTypeDefOf.RejectInput,
						false);
					return;
				}
				pawn.Name = new NameSingle(curName.Trim(), true);
				Close();
			}

			if (Widgets.ButtonText(new Rect(150f, inRect.height - 35f, 140f, 35f), "Cancel".Translate()))
			{
				Close();
			}
		}
	}
}
