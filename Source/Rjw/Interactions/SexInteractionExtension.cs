#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

using rjw.Modules.Interactions;
using rjw.Modules.Shared.Enums;
using RimWorld;

namespace rjw
{

	public enum SexInteractionType
	{
		Undefined, Masturbation, Consensual, Whoring, Rape, Bestiality, Animal, Necrophilia, Mechanoid
	}
	public enum SexInteractionTag // 扩展 InteractionType
	{
		Undefined, Masturbation, Consensual, Whoring, Rape, Bestiality, Animal, Necrophilia, Mechanoid,
		Reverse, Fertilization, EggFertilization, EggLaying, MechImplant, NonPenetrative
	}
	public class SexInteractionRequirement
	{
		public int? minimumCount = 1;
		public float? minimumSeverity;
		public List<string> partTags = new List<string>();
		public List<VanillaPartFamily> partFamilies = new List<VanillaPartFamily>();
		public List<GenitalTag> genitalTags = new List<GenitalTag>();
		public List<GenitalFamily> genitalFamilies = new List<GenitalFamily>();
		public List<PawnState> pawnStates = new List<PawnState>();

		public bool IsOrifice()
		{
			return genitalTags.Any(t => t == GenitalTag.CanBePenetrated)
				|| genitalFamilies.Any(f => f.AsLewdPartFamily().SatisfiesTag(GenitalTag.CanBePenetrated))
				|| partFamilies.Any(f => f.AsLewdPartFamily().SatisfiesTag(GenitalTag.CanBePenetrated));
		}

		public bool IsPenetrator()
		{
			return genitalTags.Any(t => t == GenitalTag.CanPenetrate)
				|| genitalFamilies.Any(f => f.AsLewdPartFamily().SatisfiesTag(GenitalTag.CanPenetrate))
				|| partFamilies.Any(f => f.AsLewdPartFamily().SatisfiesTag(GenitalTag.CanPenetrate));
		}

		public bool UsesPart(LewdablePartFamily part)
		{
			return genitalFamilies.Any(f => f.AsLewdPartFamily() == part)
				|| partFamilies.Any(f => f.AsLewdPartFamily() == part)
				|| genitalTags.Any(t => part.SatisfiesTag(t));
		}
	}
	
	public class SexInteractionExtension : DefModExtension
	{
		public string RMBLabel = ""; // 右键菜单
		public string rjwSextype = ""; // xxx.rjwSextype
		public List<RulePackDef> extraSentencePacks;
		public List<SexInteractionTag> tags = new List<SexInteractionTag>();
		public SexInteractionRequirement initiatorRequirement;
		public SexInteractionRequirement recipientRequirement;
	}
}
