#nullable disable

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using rjw.Modules.Interactions;

namespace rjw.Modules.Interactions.Preferences
{
	public static class Preference
	{
		public const float LastResort = 0.001f;
		public const float Despise = 0.1f;
		public const float Hate = 0.2f;
		public const float Dislike = 0.5f;
		public const float Neutral = 1f;
		public const float Like = 2f;
		public const float Love = 5f;
		public const float Need = 10f;
		public const float Always = Mathf.Infinity;
	}
	public class SexPreference
	{
		public float score;
		public List<SexPreference> whenSatisfied;
		public List<SexPreference> whenDissatisfied;

		public SexPreference IfSatisfied(SexPreference child)
		{
			whenSatisfied.Add(child);
			return this;
		}

		public SexPreference IfDissatisfied(SexPreference child)
		{
			whenDissatisfied.Add(child);
			return this;
		}

		public virtual bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			return true;
		}

		public virtual bool SatisfiedBy(bool initiator, SexInteractionResolved interaction)
		{
			return SatisfiedBy(initiator, interaction.Interaction);
		}


		// NOTE：如果 WhenSatisfied/WhenDissatisfied 返回新分数，它会覆盖现有分数，而不是乘入其中。
		private float GetScoreInternal(bool initiator, object interaction, float defaultValue = 1.0f)
		{
			var value = defaultValue;

			var satisfied = false;
			if (interaction is SexInteraction sexInteraction)
			{
				satisfied = SatisfiedBy(initiator, sexInteraction);
			}
			else if (interaction is SexInteractionResolved resolved)
			{
				satisfied = SatisfiedBy(initiator, resolved);
			}

			if (satisfied)
			{
				value = score;
				foreach (var preference in whenSatisfied)
				{
					var newValue = preference.GetScoreInternal(initiator, interaction, -1.0f);
					if (newValue != -1.0f)
					{
						value = newValue;
					}
				}
			}
			else
			{
				foreach (var preference in whenDissatisfied)
				{
					var newValue = preference.GetScoreInternal(initiator, interaction, -1.0f);
					if (newValue != -1.0f)
					{
						value = newValue;
					}
				}
			}

			//SexInteractionHelper.LogInteraction($"GetScore: {this.GetType().Name}, Initiator {initiator}, Satisfied {satisfied}, Value {value}");

			return value;
		}

		// 可以用 SexInteraction 或 SexInteractionResolved 来评分。
		// - SexInteraction 会针对部位候选池评分（本质上是最差/最好情况）。
		// - SexInteractionResolved 会针对所选的具体部位评分。
		public float GetScore(bool initiator, SexInteraction interaction, float defaultValue = 1.0f)
		{
			return GetScoreInternal(initiator, interaction, defaultValue);
		}
		public float GetScore(bool initiator, SexInteractionResolved interaction, float defaultValue = 1.0f)
		{
			return GetScoreInternal(initiator, interaction, defaultValue);
		}
		public SexPreference(float score)
		{
			this.score = score;
			whenSatisfied = new List<SexPreference>();
			whenDissatisfied = new List<SexPreference>();
		}
		// RimWorld 的 xml 反序列化器要求有默认构造函数
		public SexPreference() : this(Preference.Neutral)
		{ }


		public virtual string ToNameString()
		{
			return $"{GetType().Name} (Score: {score})";
		}

		public string ToString(int indent = 0)
		{
			var i = new string('\t', indent);
			var s = $"{i}{ToNameString()}";
			if (whenSatisfied.Count > 0)
			{
				s += $"\n{i}IfSatisfied:\n" + string.Join("\n", whenSatisfied.Select(p => p.ToString(indent + 1)));
			}
			if (whenDissatisfied.Count > 0)
			{
				s += $"\n{i}IfDissatisfied:\n" + string.Join("\n", whenDissatisfied.Select(p => p.ToString(indent + 1)));
			}
			return s;
		}

	}

	public class SelfPartPreference : SexPreference
	{
		public LewdablePartFamily part;
		public override bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			var req = initiator ? interaction.Extension.initiatorRequirement : interaction.Extension.recipientRequirement;
			return req.UsesPart(part);
		}

		// 如果已有解析后的互动，就可以直接检查部位。
		public override bool SatisfiedBy(bool initiator, SexInteractionResolved interaction)
		{
			if (initiator)
				return interaction.InitiatorParts.Any(p => p.Family == part);
			else
				return interaction.RecipientParts.Any(p => p.Family == part);
		}

		public SelfPartPreference(LewdablePartFamily part, float score) : base(score)
		{
			this.part = part;
		}

		public SelfPartPreference() : this(LewdablePartFamily.Undefined, Preference.Neutral)
		{ }

		public override string ToNameString()
		{
			return $"{GetType().Name} (Part: {part}, Score: {score})";
		}
	}

	public class PartnerPartPreference : SexPreference
	{
		public LewdablePartFamily part;
		public override bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			var req = initiator ? interaction.Extension.recipientRequirement : interaction.Extension.initiatorRequirement;
			return req.UsesPart(part);
		}

		// 如果已有解析后的互动，就可以直接检查部位。
		public override bool SatisfiedBy(bool initiator, SexInteractionResolved interaction)
		{
			if (initiator)
				return interaction.RecipientParts.Any(p => p.Family == part);
			else
				return interaction.InitiatorParts.Any(p => p.Family == part);
		}
		public PartnerPartPreference(LewdablePartFamily part, float score) : base(score)
		{
			this.part = part;
		}
		public PartnerPartPreference() : this(LewdablePartFamily.Undefined, Preference.Neutral)
		{ }

		public override string ToNameString()
		{
			return $"{GetType().Name} (Part: {part}, Score: {score})";
		}
	}

	public class TagPreference : SexPreference
	{
		public SexInteractionTag tag;
		public override bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			return interaction.HasInteractionTag(tag);
		}
		public TagPreference(SexInteractionTag tag, float score) : base(score)
		{
			this.tag = tag;
		}
		public TagPreference() : this(SexInteractionTag.Undefined, Preference.Neutral)
		{ }

		public override string ToNameString()
		{
			return $"{GetType().Name} (Tag: {tag}, Score: {score})";
		}
	}

	public class TypePreference : SexPreference
	{
		public xxx.rjwSextype type;
		public override bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			return interaction.Sextype == type;
		}
		public TypePreference(xxx.rjwSextype typ, float score) : base(score)
		{
			type = typ;
		}
		public TypePreference() : this(xxx.rjwSextype.None, Preference.Neutral)
		{ }

		public override string ToNameString()
		{
			return $"{GetType().Name} (Type: {type}, Score: {score})";
		}
	}

	public class PenetratedPreference : SexPreference
	{
		public override bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			var req = initiator ? interaction.Extension.initiatorRequirement : interaction.Extension.recipientRequirement;
			return interaction.IsPenetrative() && req.IsOrifice();
		}

		public override bool SatisfiedBy(bool initiator, SexInteractionResolved interaction)
		{
			foreach (var (_, orifice) in interaction.Penetrations)
			{
				if (orifice.Owner == (initiator ? interaction.Initiator : interaction.Recipient))
				{
					return true;
				}
			}
			return false;
		}
		public PenetratedPreference(float score) : base(score)
		{ }
		public PenetratedPreference() : this(Preference.Neutral)
		{ }
	}

	public class PenetratingPreference : SexPreference
	{
		public override bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			var req = initiator ? interaction.Extension.initiatorRequirement : interaction.Extension.recipientRequirement;
			return interaction.IsPenetrative() && req.IsPenetrator();
		}
		public override bool SatisfiedBy(bool initiator, SexInteractionResolved interaction)
		{
			foreach (var (penetrator, _) in interaction.Penetrations)
			{
				if (penetrator.Owner == (initiator ? interaction.Initiator : interaction.Recipient))
				{
					return true;
				}
			}
			return false;
		}
		public PenetratingPreference(float score) : base(score)
		{ }
		public PenetratingPreference() : this(Preference.Neutral)
		{ }
	}

	public class PenetrativePreference : SexPreference
	{
		public override bool SatisfiedBy(bool initiator, SexInteraction interaction)
		{
			return interaction.IsPenetrative();
		}
		public PenetrativePreference(float score) : base(score)
		{ }
		public PenetrativePreference() : this(Preference.Neutral)
		{ }
	}

	public class InteractionPreferencesResolved
	{
		public List<SexPreference> initiatorPreferences;
		public List<SexPreference> recieverPreferences;

		public InteractionPreferencesResolved()
		{
			initiatorPreferences = new List<SexPreference>();
			recieverPreferences = new List<SexPreference>();
		}
	}
}
