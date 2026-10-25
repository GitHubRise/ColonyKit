#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

using rjw.Modules.Shared;
using rjw.Modules.Interactions.Preferences;
using rjw.Modules.Shared.Extensions;

namespace rjw.Modules.Interactions
{
	public static class SexInteractionScorer
	{
		public class SexInteractionScore
		{
			public float initiatorScore;
			public float recieverScore;
			public float settingsScore;
			public float totalScore;
		}

		public static SexInteractionScore ScoreInteraction(SexInteractionResolved interaction, InteractionPreferencesResolved preferences, SexProps props)
		{
			var score = new SexInteractionScore();

			//SexInteractionHelper.LogInteraction($"ScoreInteraction: {resolved.Interaction.Def.defName} ({resolved.Interaction.Sextype})");

			score.settingsScore = Internal.SettingSexTypeScore(interaction.Interaction.Sextype);
			score.initiatorScore = Internal.GetScore(true, preferences.initiatorPreferences, interaction);
			score.recieverScore = Internal.GetScore(false, preferences.recieverPreferences, interaction);
			score.totalScore = Internal.GetTotalScore(score, props);

			return score;
		}

		public static InteractionPreferencesResolved GetPreferences(SexProps props)
		{
			var preferences = new InteractionPreferencesResolved();

			preferences.initiatorPreferences = GetPawnPreferences(true, props);
			preferences.recieverPreferences = GetPawnPreferences(false, props);

			return preferences;
		}
		public static InteractionPreferences[] AllPreferences = GenTypes.AllSubclassesNonAbstract(typeof(InteractionPreferences))
			.Select(t => (InteractionPreferences)Activator.CreateInstance(t))
			.ToArray();

		public static List<SexPreference> GetPawnPreferences(bool isInitiator, SexProps props)
		{
			var pawn = isInitiator ? props.initiator : props.recipient;
			var partner = isInitiator ? props.recipient : props.initiator;

			var preferences = new List<SexPreference>();

			if (pawn == null)
				return preferences;



			var logging = RJWSettings.DevMode && RJWSettings.DebugInteraction;
			SexInteractionHelper.LogInteraction($"GetPawnPreferences: Finding preferences of {pawn.GetName()} (with {partner.GetName()})");
			foreach (var rule in AllPreferences)
			{
				if (rule.IsActive(pawn, partner, props))
				{
					var newPreferences = rule.GetPreferences(pawn, partner, props).ToList();
					preferences.AddRange(newPreferences);

					if (logging)
					{
						var preferencesString = $"GetPawnPreferences: {rule.GetType().Name} Active. Got {newPreferences.Count()} preferences.";
						for (var i = 0; i < preferences.Count; i++)
						{
							preferencesString += $"\n{i}:{preferences[i].ToString(1)}";
						}
						SexInteractionHelper.LogInteraction(preferencesString);
					}
				}
			}
			return preferences;
		}

		public static class Internal
		{
			public static float GetScore(bool isInitiator, List<SexPreference> preferences, SexInteractionResolved resolved)
			{
				var score = 1.0f;
				foreach (var preference in preferences)
				{
					score *= preference.GetScore(isInitiator, resolved);
				}
				return score;
			}

			public static float GetTotalScore(SexInteractionScore score, SexProps props)
			{
				// 平均分
				var avg = score.settingsScore * (score.initiatorScore + score.recieverScore) / 2f;

				if (props.isRape)
				{
					return score.settingsScore * score.initiatorScore;
				}

				if (!props.hasPartner()) // 自慰
				{
					return score.settingsScore * score.initiatorScore * score.recieverScore;
				}

				var mx = Math.Max(score.initiatorScore, score.recieverScore);
				var mn = Math.Min(score.initiatorScore, score.recieverScore);

				var love = mx > 5f && mn > 0.8f;
				var hate = mn < 0.1;

				// 双方自愿。因此要更共情地考虑小人的偏好。
				if (props.isWhoring)
				{
					// 卖淫。对方可能真的想要某些东西。
					if (love)
						return score.settingsScore * mx;
					if (hate)
						return Preference.LastResort;
				}
				else
				{
					// 互相。有人可能真的很讨厌某些东西。
					if (hate)
						return Preference.LastResort;
					if (love)
						return score.settingsScore * mx;
				}

				return avg;
			}

			public static float SettingSexTypeScore(xxx.rjwSextype type)
			{
				switch (type)
				{
					case xxx.rjwSextype.Vaginal:
						return RJWPreferenceSettings.vaginal;
					case xxx.rjwSextype.Anal:
						return RJWPreferenceSettings.anal;
					case xxx.rjwSextype.DoublePenetration:
						return RJWPreferenceSettings.double_penetration;
					case xxx.rjwSextype.Boobjob:
						return RJWPreferenceSettings.breastjob;
					case xxx.rjwSextype.Handjob:
						return RJWPreferenceSettings.handjob;
					case xxx.rjwSextype.Footjob:
						return RJWPreferenceSettings.footjob;
					case xxx.rjwSextype.Fingering:
						return RJWPreferenceSettings.fingering;
					case xxx.rjwSextype.Scissoring:
						return RJWPreferenceSettings.scissoring;
					case xxx.rjwSextype.MutualMasturbation:
						return RJWPreferenceSettings.mutual_masturbation;
					case xxx.rjwSextype.Fisting:
						return RJWPreferenceSettings.fisting;
					case xxx.rjwSextype.Rimming:
						return RJWPreferenceSettings.rimming;
					case xxx.rjwSextype.Fellatio:
						return RJWPreferenceSettings.fellatio;
					case xxx.rjwSextype.Cunnilingus:
						return RJWPreferenceSettings.cunnilingus;
					case xxx.rjwSextype.Sixtynine:
						return RJWPreferenceSettings.sixtynine;
					default:
						return 1;
				}
			}
		}
	}
}
