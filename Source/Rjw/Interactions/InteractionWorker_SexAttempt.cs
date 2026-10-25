#nullable disable

﻿using System.Collections.Generic;
using System.Text;

using RimWorld;
using Verse;

namespace rjw
{
	internal class InteractionWorker_AnalSexAttempt : InteractionWorker
	{
		//发起者 - 强暴者
		//接受者 - 受害者
		public static bool AttemptAnalSex(Pawn initiator, Pawn recipient)
		{
			//--ModLog.Message(xxx.get_pawnname(initiator) + " is attempting to anally rape " + xxx.get_pawnname(recipient));
			return true;
		}

		public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
		{
			// 此互动由工作驱动触发
			if (initiator == null || recipient == null)
				return 0.0f;

			return 0.0f; // base.RandomSelectionWeight(initiator, recipient);
		}

		public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
		{
			//以后在这里加点花样？
			letterText = null;
			letterLabel = null;
			letterDef = null;
			lookTargets = recipient;
			//Find.LetterStack.ReceiveLetter("Rape attempt", "A wandering nymph has decided to join your colony.", LetterDefOf.NegativeEvent, recipient);

			if (initiator == null || recipient == null)
				return;
			//--ModLog.Message(" InteractionWorker_AnalRapeAttempt::Interacted( " + xxx.get_pawnname(initiator) + ", " + xxx.get_pawnname(recipient) + " ) called");
			AttemptAnalSex(initiator, recipient);
		}
	}

	internal class InteractionWorker_VaginalSexAttempt : InteractionWorker
	{
		//发起者 - 强暴者
		//接受者 - 受害者
		public static bool AttemptAnalSex(Pawn initiator, Pawn recipient)
		{
			//--ModLog.Message(xxx.get_pawnname(initiator) + " is attempting to anally rape " + xxx.get_pawnname(recipient));
			return false;
		}

		public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
		{
			// 此互动由工作驱动触发
			if (initiator == null || recipient == null)
				return 0.0f;

			return 0.0f; // base.RandomSelectionWeight(initiator, recipient);
		}

		public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
		{
			//以后在这里加点花样？
			letterText = null;
			letterLabel = null;
			letterDef = null;
			lookTargets = recipient;
			//Find.LetterStack.ReceiveLetter("Rape attempt", "A wandering nymph has decided to join your colony.", LetterDefOf.NegativeEvent, recipient);

			if (initiator == null || recipient == null)
				return;
			//--ModLog.Message(" InteractionWorker_AnalRapeAttempt::Interacted( " + xxx.get_pawnname(initiator) + ", " + xxx.get_pawnname(recipient) + " ) called");
			AttemptAnalSex(initiator, recipient);
		}
	}

	internal class InteractionWorker_OtherSexAttempt : InteractionWorker
	{
		//发起者 - 强暴者
		//接受者 - 受害者
		public static bool AttemptAnalSex(Pawn initiator, Pawn recipient)
		{
			//--ModLog.Message(xxx.get_pawnname(initiator) + " is attempting to anally rape " + xxx.get_pawnname(recipient));
			return false;
		}

		public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
		{
			// 此互动由工作驱动触发
			if (initiator == null || recipient == null)
				return 0.0f;

			return 0.0f; // base.RandomSelectionWeight(initiator, recipient);
		}

		public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
		{
			//以后在这里加点花样？
			letterText = null;
			letterLabel = null;
			letterDef = null;
			lookTargets = recipient;
			//Find.LetterStack.ReceiveLetter("Rape attempt", "A wandering nymph has decided to join your colony.", LetterDefOf.NegativeEvent, recipient);

			if (initiator == null || recipient == null)
				return;
			//--ModLog.Message(" InteractionWorker_AnalRapeAttempt::Interacted( " + xxx.get_pawnname(initiator) + ", " + xxx.get_pawnname(recipient) + " ) called");
			AttemptAnalSex(initiator, recipient);
		}
	}
}
