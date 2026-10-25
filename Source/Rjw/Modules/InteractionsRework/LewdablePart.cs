#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using rjw.Modules.Shared.Extensions;
using System.Runtime.InteropServices;

namespace rjw.Modules.Interactions
{
	public enum VanillaPartFamily
	{
		Undefined,
		Hand, Foot, Tail, Mouth, Beak, Tongue
	}
	public enum LewdablePartFamily // GenitalFamily 与 VanillaPartFamily 的并集
	{
		Undefined,
		Vagina, Penis, Breasts, Udders, Anus, FemaleOvipositor, MaleOvipositor,
		Hand, Foot, Tail, Mouth, Beak, Tongue
	}

	public interface ILewdablePart
	{
		public BodyPartRecord BodyPart { get; }
		public LewdablePartFamily Family { get; }

		public Pawn Owner { get; }
		public string Label { get; }
	}

	public class RJWLewdablePart : ILewdablePart
	{
		public ISexPartHediff SexPart;
		public BodyPartRecord BodyPart { get {
				return SexPart.AsHediff.Part;
		}}
		public LewdablePartFamily Family { get; set; }

		public Pawn Owner => SexPart.GetOwner();
		public string Label => SexPart.Def.label.CapitalizeFirst();
		public RJWLewdablePart(ISexPartHediff sexPart, LewdablePartFamily family)
		{
			SexPart = sexPart;
			Family = family;
		}
	}

	public class VanillaLewdablePart : ILewdablePart
	{
		public Pawn Owner { get; }
		public BodyPartRecord BodyPart { get; }
		public LewdablePartFamily Family { get; }
		public string Label => BodyPart.def.label.CapitalizeFirst();
		public VanillaLewdablePart(Pawn owner, BodyPartRecord part, LewdablePartFamily family)
		{
			Owner = owner;
			BodyPart = part;
			Family = family;
		}
	}

	public class LewdParts
	{
		public Pawn Owner { get; protected set; }

		private List<RJWLewdablePart> _penises;
		public List<RJWLewdablePart> Penises
		{
			get
			{
				_penises = _penises ?? LewdPartHelper.GetPenises(Owner);
				return _penises;
			}
		}

		private List<RJWLewdablePart> _vaginas;
		public List<RJWLewdablePart> Vaginas
		{
			get
			{
				_vaginas = _vaginas ?? LewdPartHelper.GetVaginas(Owner);
				return _vaginas;
			}
		}

		private List<RJWLewdablePart> _breasts;
		public List<RJWLewdablePart> Breasts
		{
			get
			{
				_breasts = _breasts ?? LewdPartHelper.GetBreasts(Owner);
				return _breasts;
			}
		}

		private List<RJWLewdablePart> _udders;
		public List<RJWLewdablePart> Udders
		{
			get
			{
				_udders = _udders ?? LewdPartHelper.GetUdders(Owner);
				return _udders;
			}
		}

		private List<RJWLewdablePart> _anuses;
		public List<RJWLewdablePart> Anuses
		{
			get
			{
				_anuses = _anuses ?? LewdPartHelper.GetAnuses(Owner);
				return _anuses;
			}
		}

		private List<RJWLewdablePart> _femaleOvipositors;
		public List<RJWLewdablePart> FemaleOvipositors
		{
			get
			{
				_femaleOvipositors = _femaleOvipositors ?? LewdPartHelper.GetFemaleOvipositors(Owner);
				return _femaleOvipositors;
			}
		}

		private List<RJWLewdablePart> _maleOvipositors;
		public List<RJWLewdablePart> MaleOvipositors
		{
			get
			{
				_maleOvipositors = _maleOvipositors ?? LewdPartHelper.GetMaleOvipositors(Owner);
				return _maleOvipositors;
			}
		}
		
		private List<VanillaLewdablePart> _mouths;
		public List<VanillaLewdablePart> Mouths
		{
			get
			{
				_mouths = _mouths ?? LewdPartHelper.GetMouths(Owner);
				return _mouths;
			}
		}

		private List<VanillaLewdablePart> _beaks;
		public List<VanillaLewdablePart> Beaks
		{
			get
			{
				_beaks = _beaks ?? LewdPartHelper.GetBeaks(Owner);
				return _beaks;
			}
		}

		private List<VanillaLewdablePart> _tongues;
		public List<VanillaLewdablePart> Tongues
		{
			get
			{
				_tongues = _tongues ?? LewdPartHelper.GetTongues(Owner);
				return _tongues;
			}
		}

		private List<VanillaLewdablePart> _feet;
		public List<VanillaLewdablePart> Feet
		{
			get
			{
				_feet = _feet ?? LewdPartHelper.GetFeet(Owner);
				return _feet;
			}
		}

		private List<VanillaLewdablePart> _hands;
		public List<VanillaLewdablePart> Hands
		{
			get
			{
				_hands = _hands ?? LewdPartHelper.GetHands(Owner);
				return _hands;
			}
		}

		private List<VanillaLewdablePart> _tails;
		public List<VanillaLewdablePart> Tails
		{
			get
			{
				_tails = _tails ?? LewdPartHelper.GetTails(Owner);
				return _tails;
			}
		}

		public List<ILewdablePart> AllParts
		{
			get
			{
				return AllRJWParts.Cast<ILewdablePart>()
					.Concat(AllVanillaParts)
					.ToList();
			}
		}

		public List<RJWLewdablePart> AllRJWParts
		{
			get
			{
				if (_penises == null)
					PopulateAll();

				return Penises.Concat(Vaginas).Concat(Breasts).Concat(Udders).Concat(Anuses)
					.Concat(FemaleOvipositors).Concat(MaleOvipositors)
					.ToList();
			}
		}

		public List<VanillaLewdablePart> AllVanillaParts
		{
			get
			{
				return Mouths
					.Concat(Beaks).Concat(Tongues).Concat(Feet).Concat(Hands).Concat(Tails)
					.ToList();
			}
		}

		public LewdParts(Pawn pawn, bool populateAll = false)
		{
			Owner = pawn;
			if (populateAll)
			{
				PopulateAll();
			}
		}

		public void PopulateAll()
		{
			_mouths = LewdPartHelper.GetMouths(Owner);
			_beaks = LewdPartHelper.GetBeaks(Owner);
			_tongues = LewdPartHelper.GetTongues(Owner);
			_hands = LewdPartHelper.GetHands(Owner);
			_feet = LewdPartHelper.GetFeet(Owner);
			_tails = LewdPartHelper.GetTails(Owner);

			IList<ISexPartHediff> cache = Owner.health.hediffSet.hediffs
				.OfType<ISexPartHediff>()
				.ToList();

			_penises = LewdPartHelper.GetPenises(Owner, cache);
			_vaginas = LewdPartHelper.GetVaginas(Owner, cache);
			_breasts = LewdPartHelper.GetBreasts(Owner, cache);
			_udders = LewdPartHelper.GetUdders(Owner, cache);
			_anuses = LewdPartHelper.GetAnuses(Owner, cache);
		}
	}

	public static class LewdPartHelper
	{
		public static LewdParts GetLewdParts(this Pawn self)
		{
			if (self == null)
			{
				return null;
			}

			var lewdParts = new LewdParts(self);

			return lewdParts;
		}

		public static List<RJWLewdablePart> GetPenises(this Pawn pawn, IEnumerable<ISexPartHediff> cache = null)
		{
			return (cache ?? pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				.ByGenitalFamily(GenitalFamily.Penis, LewdablePartFamily.Penis)
				.ToList();
		}

		public static List<RJWLewdablePart> GetVaginas(this Pawn pawn, IEnumerable<ISexPartHediff> cache = null)
		{
			return (cache ?? pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				.ByGenitalFamily(GenitalFamily.Vagina, LewdablePartFamily.Vagina)
				.ToList();
		}

		public static List<RJWLewdablePart> GetBreasts(this Pawn pawn, IEnumerable<ISexPartHediff> cache = null)
		{
			return (cache ?? pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				.ByGenitalFamily(GenitalFamily.Breasts, LewdablePartFamily.Breasts)
				.ToList();
		}

		public static List<RJWLewdablePart> GetUdders(this Pawn pawn, IEnumerable<ISexPartHediff> cache = null)
		{
			return (cache ?? pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				.ByGenitalFamily(GenitalFamily.Udders, LewdablePartFamily.Udders)
				.ToList();
		}

		public static List<RJWLewdablePart> GetAnuses(this Pawn pawn, IEnumerable<ISexPartHediff> cache = null)
		{
			return (cache ?? pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				.ByGenitalFamily(GenitalFamily.Anus, LewdablePartFamily.Anus)
				.ToList();
		}

		public static List<RJWLewdablePart> GetFemaleOvipositors(this Pawn pawn, IEnumerable<ISexPartHediff> cache = null)
		{
			return (cache ?? pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				.ByGenitalFamily(GenitalFamily.FemaleOvipositor, LewdablePartFamily.FemaleOvipositor)
				.ToList();
		}

		public static List<RJWLewdablePart> GetMaleOvipositors(this Pawn pawn, IEnumerable<ISexPartHediff> cache = null)
		{
			return (cache ?? pawn.health.hediffSet.hediffs.OfType<ISexPartHediff>())
				.ByGenitalFamily(GenitalFamily.MaleOvipositor, LewdablePartFamily.MaleOvipositor)
				.ToList();
		}

		public static List<VanillaLewdablePart> GetMouths(this Pawn pawn)
		{
			//EatingSource = mouth
			return pawn.RaceProps.body.GetPartsWithTag(RimWorld.BodyPartTagDefOf.EatingSource)
				.Where(part => part.def.defName?.ToLower().Contains("beak") == false)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Mouth).ToList();
		}
		public static List<VanillaLewdablePart> GetBeaks(this Pawn pawn)
		{
			//EatingSource = mouth
			return pawn.RaceProps.body.GetPartsWithTag(RimWorld.BodyPartTagDefOf.EatingSource)
				.Where(part => part.def.defName?.ToLower().Contains("beak") == true)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Beak).ToList();
		}
		public static List<VanillaLewdablePart> GetTongues(this Pawn pawn)
		{
			BodyDef body = pawn.RaceProps.body;
			
			var possibleParts = body.GetPartsWithTag(RimWorld.BodyPartTagDefOf.Tongue)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Tongue).ToList();
			
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 原版动物没有舌头
			possibleParts = body.GetPartsWithDef(RJWBodyPartDefOf.AnimalJaw)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Tongue).ToList();

			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 为拥有自定义舌头定义的种族提供的兜底
			possibleParts = GetPartsWhereDefNameContains(body, "tongue")
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Tongue).ToList();

			return possibleParts;
		}

		public static List<VanillaLewdablePart> GetTails(this Pawn pawn)
		{
			BodyDef body = pawn.RaceProps.body;
			var possibleParts = body.GetPartsWithDef(RJWBodyPartDefOf.Tail)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Tail).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 至少发现一个种族拥有自定义尾巴定义
			return GetPartsWhereDefNameContains(body, "tail")
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Tail).ToList();
		}

		public static List<VanillaLewdablePart> GetHands(this Pawn pawn)
		{
			BodyDef body = pawn.RaceProps.body;
			var possibleParts = body.GetPartsWithDef(BodyPartDefOf.Hand)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Hand).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 有些种族根本没有对手部建模
			possibleParts = body.GetPartsWithDef(BodyPartDefOf.Arm)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Hand).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 至少有一个种族只对肩部建模
			possibleParts = body.GetPartsWithDef(BodyPartDefOf.Shoulder)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Hand).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 为拥有自定义手部定义的种族提供的兜底
			possibleParts = GetPartsWhereDefNameContains(body, "hand")
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Hand).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 查找身体中带有此特定标签的任何部位
			var manipulationTags = new[]
			{
				RimWorld.BodyPartTagDefOf.ManipulationLimbCore,
				RimWorld.BodyPartTagDefOf.ManipulationLimbSegment,
				RimWorld.BodyPartTagDefOf.ManipulationLimbDigit
			};

			foreach (var tag in manipulationTags)
			{
				possibleParts = [.. body.GetPartsWithTag(tag).AsVanillaLewdableParts(pawn, LewdablePartFamily.Hand)];

				if (possibleParts.Count > 0)
				{
					return possibleParts;
				}
			}
			return [];
		}

		public static List<VanillaLewdablePart> GetFeet(this Pawn pawn)
		{
			BodyDef body = pawn.RaceProps.body;
			var possibleParts = body.GetPartsWithDef(RJWBodyPartDefOf.Foot)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Foot).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 动物有爪子而不是脚
			possibleParts = body.GetPartsWithDef(RJWBodyPartDefOf.Paw)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Foot).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 不知道是否存在未对脚部建模的种族，但我还是加上这个
			possibleParts = body.GetPartsWithDef(RJWBodyPartDefOf.Leg)
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Foot).ToList();
			if (possibleParts.Count > 0)
			{
				return possibleParts;
			}

			// 为拥有自定义脚部定义的种族提供的兜底
			return GetPartsWhereDefNameContains(body, "foot", "paw")
				.AsVanillaLewdableParts(pawn, LewdablePartFamily.Foot).ToList();
		}

		public static bool HasGenitalTag(this ILewdablePart part, GenitalTag tag)
		{
			if(part is RJWLewdablePart rjwPart)
			{
				return rjwPart.SexPart.Def.genitalTags.Contains(tag);
			}
			return false;
		}

		public static bool HasPartTag(this ILewdablePart part, string tag)
		{
			if(part is RJWLewdablePart rjwPart)
			{
				return rjwPart.SexPart.Def.partTags.Contains(tag);
			}
			return false;
		}

		public static bool CouldBeOrifice(this ILewdablePart part)
		{
			return SatisfiesTag(part.Family, GenitalTag.CanBePenetrated);
		}

		public static bool CouldBePenetrator(this ILewdablePart part)
		{
			return SatisfiesTag(part.Family, GenitalTag.CanPenetrate);
		}

		public static bool IsMissing(Pawn pawn, ILewdablePart part)
		{
			if(pawn.health.hediffSet.PartIsMissing(part.BodyPart))
			{
				// 该部位可能因被人工部位替代而缺失。
				// 在原版中，一个人工部位可以替代多个自然部位。
				// 例如，仿生臂实际上是肩部，而手臂、手和手指都缺失了
				if(!pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part.BodyPart))
				{
					return true;
				}
			}
			return false;
		}
		public static bool IsBlocked(Pawn pawn, ILewdablePart part)
		{
			switch (part.Family)
			{
				case LewdablePartFamily.Penis:
					if (Genital_Helper.penis_blocked(pawn) || Genital_Helper.genitals_blocked(pawn))
						return true;
					break;
				case LewdablePartFamily.Vagina:
					if (Genital_Helper.vagina_blocked(pawn) || Genital_Helper.genitals_blocked(pawn))
						return true;
					break;
				case LewdablePartFamily.MaleOvipositor:
				case LewdablePartFamily.FemaleOvipositor:
					if (Genital_Helper.genitals_blocked(pawn))
						return true;
					break;
				case LewdablePartFamily.Anus:
					if (Genital_Helper.anus_blocked(pawn))
						return true;
					break;
				case LewdablePartFamily.Breasts:
					if (Genital_Helper.breasts_blocked(pawn))
						return true;
					break;
				case LewdablePartFamily.Mouth:
				case LewdablePartFamily.Beak:
				case LewdablePartFamily.Tongue:
					if (Genital_Helper.oral_blocked(pawn))
						return true;
					break;
				case LewdablePartFamily.Hand:
					if (Genital_Helper.hands_blocked(pawn))
						return true;
					break;
			}

			return false;
		}

		public static bool IsAvailable(Pawn pawn, ILewdablePart part)
		{
			return !IsMissing(pawn, part) && !IsBlocked(pawn, part);
		}

		// 查找定义名称包含这些字符串之一的全部 BodyPartRecord。
		private static IEnumerable<BodyPartRecord> GetPartsWhereDefNameContains(BodyDef body, params string[] name)
		{
			for (int i = 0; i < body.AllParts.Count; i++)
			{
				if (body.AllParts[i].def.defName?.ToLower().ContainsAny(name) == true)
				{
					yield return body.AllParts[i];
				}
			}
		}

		private static IEnumerable<RJWLewdablePart> ByGenitalFamily(this IEnumerable<ISexPartHediff> self, GenitalFamily family, LewdablePartFamily kind)
		{
			return self.Where(e => e.Def.genitalFamily == family)
				.AsRJWLewdableParts(kind);
		}

		private static IEnumerable<VanillaLewdablePart> AsVanillaLewdableParts(this IEnumerable<BodyPartRecord> self, Pawn owner, LewdablePartFamily kind)
		{
			return self.Select(part => new VanillaLewdablePart(owner, part, kind));
		}

		private static IEnumerable<RJWLewdablePart> AsRJWLewdableParts(this IEnumerable<ISexPartHediff> self, LewdablePartFamily kind)
		{
			return self.Select(part => new RJWLewdablePart(part, kind));
		}

		public static List<ISexPartHediff> AsSexPartHediffs(this List<RJWLewdablePart> self)
		{
			return self.Select(e => e.SexPart).ToList();
		}

		public static List<BodyPartRecord> AsBodyPartRecords(this List<ILewdablePart> self)
		{
			return self.Select(e => e.BodyPart).ToList();
		}
		
		public static bool SatisfiesTag(this LewdablePartFamily part, GenitalTag tag)
		{
			switch(tag)
			{
				case GenitalTag.CanBeFertilized:
					return part == LewdablePartFamily.Vagina;
				case GenitalTag.CanFertilize:
					return part == LewdablePartFamily.Penis;
				case GenitalTag.CanEgg:
					return part == LewdablePartFamily.FemaleOvipositor;
				case GenitalTag.CanFertilizeEgg:
					return part == LewdablePartFamily.MaleOvipositor;
				case GenitalTag.CanLactate:
					return part == LewdablePartFamily.Breasts || part == LewdablePartFamily.Udders;
				case GenitalTag.CanPenetrate:
					return part == LewdablePartFamily.Penis
						|| part == LewdablePartFamily.MaleOvipositor
						|| part == LewdablePartFamily.FemaleOvipositor
						|| part == LewdablePartFamily.Hand;
				case GenitalTag.CanBePenetrated:
					return part == LewdablePartFamily.Vagina
						|| part == LewdablePartFamily.Anus
						|| part == LewdablePartFamily.FemaleOvipositor
						|| part == LewdablePartFamily.Mouth
						|| part == LewdablePartFamily.Beak;
				default:
					return false;
			}
		}

		// GenitalFamily/VanillaPartFamily -> LewdablePartFamily 的转换
		public static LewdablePartFamily AsLewdPartFamily(this GenitalFamily genitalFamily)
		{
			return (LewdablePartFamily)Enum.Parse(typeof(LewdablePartFamily), genitalFamily.ToString());
		}

		public static LewdablePartFamily AsLewdPartFamily(this VanillaPartFamily partFamily)
		{
			return (LewdablePartFamily)Enum.Parse(typeof(LewdablePartFamily), partFamily.ToString());
		}

	}
}
