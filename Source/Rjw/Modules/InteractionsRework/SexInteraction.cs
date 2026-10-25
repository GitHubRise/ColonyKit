#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using rjw.Modules.Shared.Extensions;
using rjw.Modules.Shared;
using UnityEngine;

namespace rjw.Modules.Interactions
{
	public class SexInteraction
	{
		public InteractionDef Def { get; private set; }
		public SexInteractionExtension Extension { get; private set; }
		public SexInteraction(InteractionDef def)
		{
			Def = def;
			Extension = def.GetModExtension<SexInteractionExtension>();
		}

		public bool HasInteractionTag(SexInteractionTag tag)
		{
			return Extension.tags.Contains(tag);
		}

		private xxx.rjwSextype _sextype = xxx.rjwSextype.None;
		public xxx.rjwSextype Sextype
		{
			get
			{
				if (_sextype == xxx.rjwSextype.None)
				{
					_sextype = ParseHelper.FromString<xxx.rjwSextype>(Extension.rjwSextype);
				}
				return _sextype;

			}
		}
		public bool IsPenetrative()
		{
			switch (Sextype)
			{
				case xxx.rjwSextype.Vaginal:
				case xxx.rjwSextype.Anal:
				case xxx.rjwSextype.Oral:
				case xxx.rjwSextype.Fellatio:
				case xxx.rjwSextype.Fingering:
				case xxx.rjwSextype.Fisting:
				case xxx.rjwSextype.DoublePenetration:
					return true;
				default:
					return false;
			}
		}

		public void AddToPlayLog(Pawn initiator, Pawn recipient)
		{
			PlayLogEntry_Interaction playLogEntry = new PlayLogEntry_Interaction(Def, initiator, recipient, Extension.extraSentencePacks);
			Find.PlayLog.Add(playLogEntry);
		}
	}

	public class SexInteractionResolved
	{
		public Pawn Initiator;
		public Pawn Recipient;
		public SexInteraction Interaction;
		public List<ILewdablePart> InitiatorParts;
		public List<ILewdablePart> RecipientParts;
		public List<Tuple<ILewdablePart, ILewdablePart>> Penetrations; // <插入方, 腔口>
		public SexInteractionScorer.SexInteractionScore Score; // 若为强制行为则可能为 Null
	}

	public static class SexInteractionHelper
	{
		public static void LogInteraction(string message)
		{
			if (RJWSettings.DevMode && RJWSettings.DebugInteraction) ModLog.Message(message);
		}
		public static bool ChooseInteraction(SexProps props)
		{
			var preferences = SexInteractionScorer.GetPreferences(props);

			var available = SexInteractionFinder.FindInteractions(props);

			foreach (var i in available)
			{
				i.Score = SexInteractionScorer.ScoreInteraction(i, preferences, props);
			}

			// 每种互动只考虑得分最高的变体，以免稀释候选池。
			available = available.GroupBy(i => i.Interaction.Def).Select(g => g.MaxBy(i => i.Score.totalScore)).ToList();

			var scored = new List<Weighted<SexInteractionResolved>>();

			LogInteraction($"ChooseInteraction: {props.initiator.GetName()} -> {props.recipient.GetName()}. IsRape: {props.isRape}, IsWhoring: {props.isWhoring}, IsReceiver: {props.isReceiver}, Found: {available.Count()}");

			foreach (var i in available)
			{
				Internal.WriteInteractionToDebugLog(i);
				scored.Add(new(i.Score.totalScore, i));
			}

			if (scored.Count() == 0)
			{
				LogInteraction($"ChooseInteraction: No interactions found!");
				return false;
			}

			var choice = scored.RandomElementByWeight(i => i.Weight).Element;
			props.resolved = choice;
			props.interaction = choice.Interaction;

			LogInteraction($"ChooseInteraction: Result, {choice.Interaction.Def.defName}");

			return true;
		}

		public static SexInteractionResolved ResolveInteraction(SexProps props)
		{
			// 解析特定的 InteractionDef
			// 如果该互动不可行，则返回 null

			var cache = new SexInteractionFinder.Internal.FinderCache(props);
			var variants = SexInteractionFinder.Internal.ResolveInteraction(props.interaction, props, cache).ToList();

			if (variants.Count() == 0)
			{
				return null;
			}

			var preferences = SexInteractionScorer.GetPreferences(props);
			foreach (var i in variants)
			{
				i.Score = SexInteractionScorer.ScoreInteraction(i, preferences, props);
			}

			var choice = variants.MaxBy(i => i.Score.totalScore);

			LogInteraction($"ResolveInteraction: {props.initiator.GetName()} -> {props.recipient.GetName()}");
			Internal.WriteInteractionToDebugLog(choice);

			return choice;
		}

		public static class Internal
		{
			public class FinderCache
			{
				public LewdParts initiatorParts;
				public LewdParts recipientParts;

				public FinderCache(SexProps props)
				{
					initiatorParts = new LewdParts(props.initiator, populateAll: true);
					recipientParts = new LewdParts(props.recipient, populateAll: true);
				}
			}

			public static void WriteInteractionToDebugLog(SexInteractionResolved i)
			{
				LogInteraction($"  Interaction: {i.Interaction.Def.defName} ({i.Score.initiatorScore}, {i.Score.recieverScore}, {i.Score.settingsScore}) -> {i.Score.totalScore}");
				LogInteraction($"    Initiator: {i.InitiatorParts.Select(p => p.Label).ToCommaList()}");
				LogInteraction($"    Recipient: {i.RecipientParts.Select(p => p.Label).ToCommaList()}");
				LogInteraction($"    Penetrations: {i.Penetrations.Select(p => $"{p.Item1.Label} -> {p.Item2.Label}").ToCommaList()}");
			}
		}

		// InteractionTag <-> InteractionType 的转换
		public static SexInteractionTag AsInteractionTag(this SexInteractionType interactionType)
		{
			return (SexInteractionTag)Enum.Parse(typeof(SexInteractionTag), interactionType.ToString());
		}

		public static SexInteractionType AsInteractionType(this SexInteractionTag interactionTag)
		{
			if (Enum.IsDefined(typeof(SexInteractionType), interactionTag.ToString()))
			{
				return (SexInteractionType)Enum.Parse(typeof(SexInteractionType), interactionTag.ToString());
			}
			return SexInteractionType.Undefined;
		}
	}
}
