#nullable disable

using RimWorld;
using Verse;
using UnityEngine;
using Multiplayer.API;


namespace rjw
{
	///<summary>
	///提交按钮
	///</summary>
	[StaticConstructorOnStartup]
	public class SubmitGizmo : Command_Action
	{
		private static readonly Texture2D submitIcon = ContentFinder<Texture2D>.Get("UI/Commands/Submit", true);

		public SubmitGizmo(Pawn pawn) : base()
		{
			defaultLabel = "CommandSubmit".Translate();
			icon = submitIcon;
			defaultDesc = "CommandSubmitDesc".Translate();
			action = delegate
			{
				LayDownAndAccept(pawn);
			};
			hotKey = KeyBindingDefOf.Misc3;
		}

		[SyncMethod]
		static void LayDownAndAccept(Pawn pawn)
		{
			//ModLog.Message("Submit button is pressed for " + pawn);
			pawn.health.AddHediff(RJWHediffDefOf.Submitting);
		}
	}
}
