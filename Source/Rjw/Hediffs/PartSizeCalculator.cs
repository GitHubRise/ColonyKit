#nullable disable

using Verse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rjw
{
	public static class PartSizeCalculator
	{
		public static bool TryGetLength(Hediff hediff, out float length)
		{
			if (!hediff.TryGetComp<HediffComp_SexPart>(out var comp))
			{
				length = 0;
				return false;
			}

			return Internal.TryGetLength(hediff.def, Internal.GetPartSeverity(hediff), Internal.GetBodySize(hediff), out length);
		}

		public static bool TryGetGirth(Hediff hediff, out float girth)
		{
			if (!hediff.TryGetComp<HediffComp_SexPart>(out var comp))
			{
				girth = 0;
				return false;
			}

			return Internal.TryGetGirth(hediff.def, Internal.GetPartSeverity(hediff), Internal.GetBodySize(hediff), out girth);
		}

		public static bool TryGetPenisWeight(Hediff hediff, out float weight)
		{
			if (!TryGetLength(hediff, out float length) ||
				!TryGetGirth(hediff, out float girth))
			{
				weight = 0f;
				return false;
			}

			return Internal.TryGetPenisWeight(hediff.def, length, girth, out weight);
		}

		public static bool TryGetBreastSize(Hediff hediff, out BreastSize breastSize)
		{
			if (!hediff.TryGetComp<HediffComp_SexPart>(out var comp))
			{
				breastSize = new BreastSize(0, 0, 1);
				return false;
			}

			return Internal.TryGetBreastSize(hediff.def, Internal.GetPartSeverity(hediff), Internal.GetBodySize(hediff), out breastSize);
		}

		public class Internal
		{
			public static float GetBodySize(Hediff hediff)
			{
				if (hediff.TryGetComp<HediffComp_SexPart>(out var comp))
				{
					return comp.GetBodySize();
				}
				return hediff.pawn.BodySize;
			}

			public static float GetPartSeverity(Hediff hediff)
			{
				if (hediff.TryGetComp<HediffComp_SexPart>(out var comp))
				{
					return comp.GetSeverity();
				}
				return hediff.Severity;
			}

			public static bool TryGetLength(HediffDef hediffDef, float severity, float bodySize, out float length)
			{
				var curve = Curves.LengthCurve(hediffDef);

				if(!Curves.EvaluateCurve(curve, severity, bodySize, out length)) {
					length = 0;
					return false;
				}

				return true;
			}

			public static bool TryGetGirth(HediffDef hediffDef, float severity, float bodySize, out float girth)
			{
				var curve = Curves.GirthCurve(hediffDef);

				if(!Curves.EvaluateCurve(curve, severity, bodySize, out girth)) {
					girth = 0;
					return false;
				}

				return true;
			}


			public static bool TryGetCupSize(HediffDef hediffDef, float severity, out float cupSize)
			{
				var curve = Curves.CupSizeCurve(hediffDef);

				if(!Curves.EvaluateCurve(curve, severity, 1.0f, out cupSize)) {
					cupSize = 0;
					return false;
				}

				return true;
			}

			public static bool TryGetBandSize(HediffDef hediffDef, float bodySize, out float bandSize)
			{
				var baseBandSize = BraSizeConfigDef.Instance.bandSizeBase;

				bandSize = (float)Math.Round(baseBandSize * bodySize / 2) * 2;

				return true;
			}

			public static bool TryGetPenisWeight(HediffDef hediffDef, float length, float girth, out float weight)
			{
				var density = Curves.GetSizeConfig(hediffDef).density;
				if (density == null)
				{
					weight = 0f;
					return false;
				}

				var r = girth / (2.0 * Math.PI);
				var volume = r * r * Math.PI * length;

				weight = (float)(volume * density.Value / 1000f);
				return true;
			}

			public static bool TryGetBreastSize(HediffDef hediffDef, float severity, float bodySize, out BreastSize breastSize)
			{
				var density = Curves.GetSizeConfig(hediffDef).density;
				if (density == null)
				{
					density = 1.0f;
				}

				breastSize = new BreastSize(0, 0, 1);

				if(!TryGetCupSize(hediffDef, severity, out var cupSize))
				{
					return false;
				}

				if(!TryGetBandSize(hediffDef, bodySize, out var bandSize))
				{
					return false;
				}

				breastSize = new BreastSize(cupSize, bandSize, density.Value);
				return true;
			}			
		}

		public static class Curves 
		{
			public static SimpleCurve LengthCurve(HediffDef def)
			{
				return BuildCurve(def.stages, GetSizeConfig(def)?.lengths);
			}

			public static SimpleCurve GirthCurve(HediffDef def)
			{
				return BuildCurve(def.stages, GetSizeConfig(def)?.girths);
			}

			public static SimpleCurve CupSizeCurve(HediffDef def)
			{
				return BuildCurve(def.stages, GetSizeConfig(def)?.cupSizes);
			}

			public static SimpleCurve BuildCurve(List<HediffStage> stages, List<float> stageSizes)
			{
				if(stageSizes == null) return null;
				return new SimpleCurve(stages.Zip(stageSizes, (stage, stageSize) => new CurvePoint(stage.minSeverity, stageSize)));
			}

			public static bool EvaluateCurve(SimpleCurve curve, float input, float scale, out float measurement)
			{
				if (curve == null) {
					measurement = 0;
					return false;
				}
				measurement = curve.Evaluate(input) * scale;
				return true;
			}

			public static bool EvaluateInvertedCurve(SimpleCurve curve, float measurement, float scale, out float input)
			{
				if (curve == null) {
					input = 0;
					return false;
				}
				input = curve.EvaluateInverted(measurement/scale);
				return true;
			}

			public static PartSizeConfigDef GetSizeConfig(Hediff hediff)
			{
				return GetSizeConfig(hediff.def);
			}

			public static PartSizeConfigDef GetSizeConfig(HediffDef def)
			{
				return (def as HediffDef_SexPart)?.sizeProfile;
			}
		}

		public class Inversion
		{
			public static bool TryInvertLength(HediffDef hediffDef, float length, float bodySize, out float severity)
			{
				var curve = Curves.LengthCurve(hediffDef);

				if(!Curves.EvaluateInvertedCurve(curve, length, bodySize, out severity)) {
					severity = 0;
					return false;
				}

				return true;
			}

			public static bool TryInvertGirth(HediffDef hediffDef, float girth, float bodySize, out float severity)
			{
				var curve = Curves.GirthCurve(hediffDef);

				if(!Curves.EvaluateInvertedCurve(curve, girth, bodySize, out severity)) {
					severity = 0;
					return false;
				}

				return true;
			}

			public static bool TryInvertCupSize(HediffDef hediffDef, float cupSize, out float severity)
			{
				var curve = Curves.CupSizeCurve(hediffDef);

				if(!Curves.EvaluateInvertedCurve(curve, cupSize, 1.0f, out severity)) {
					severity = 0;
					return false;
				}

				return true;
			}

			public static bool TryInvertBreastWeight(HediffDef hediffDef, float weight, float bodySize, out float severity, out BreastSize breastSize)
			{
				var density = Curves.GetSizeConfig(hediffDef).density;
				if (density == null)
				{
					severity = 0f;
					breastSize = new BreastSize(0, 0, 1);
					return false;
				}

				var volume = weight / density.Value;
				return TryInvertBreastVolume(hediffDef, volume, bodySize, out severity, out breastSize);
			}

			public static bool TryInvertBreastVolume(HediffDef hediffDef, float volume, float bodySize, out float severity, out BreastSize breastSize)
			{
				severity = 0f;
				breastSize = new BreastSize(0, 0, 1);

				if(!Internal.TryGetBandSize(hediffDef, bodySize, out var bandSize))
				{
					return false;
				}

				var cupSize = BreastSize.CalculateCupSize(volume, bandSize);

				if(!TryInvertCupSize(hediffDef, cupSize, out severity))
				{
					return false;
				}

				var density = Curves.GetSizeConfig(hediffDef).density ?? 1.0f;

				breastSize = new BreastSize(cupSize, bandSize, density);

				return true;
			}
		}

		public class Modify
		{
			public static bool TryModifyLength(Hediff hediff, Func<float, float> lengthModifier, out float newSeverity)
			{
				if(!TryGetLength(hediff, out var length))
				{
					newSeverity = 0;
					return false;
				}

				return Inversion.TryInvertLength(hediff.def, lengthModifier(length), Internal.GetBodySize(hediff), out newSeverity);
			}

			public static bool TryModifyGirth(Hediff hediff, Func<float, float> girthModifier, out float newSeverity)
			{
				if(!TryGetGirth(hediff, out var girth))
				{
					newSeverity = 0;
					return false;
				}

				return Inversion.TryInvertGirth(hediff.def, girthModifier(girth), Internal.GetBodySize(hediff), out newSeverity);
			}

			public static bool TryModifyCupSize(Hediff hediff, Func<float, float> cupSizeModifier, out float newSeverity)
			{
				if(!Internal.TryGetCupSize(hediff.def, Internal.GetPartSeverity(hediff), out var cupSize))
				{
					newSeverity = 0;
					return false;
				}

				return Inversion.TryInvertCupSize(hediff.def, cupSizeModifier(cupSize), out newSeverity);
			}

			public static bool TryModifyBreastVolume(Hediff hediff, Func<float, float> volumeModifier, out float newSeverity)
			{
				if(!TryGetBreastSize(hediff, out var breastSize))
				{
					newSeverity = 0;
					return false;
				}

				return Inversion.TryInvertBreastVolume(hediff.def, volumeModifier(breastSize.volume), Internal.GetBodySize(hediff), out newSeverity, out _);
			}

			public static bool TryModifyBreastWeight(Hediff hediff, Func<float, float> weightModifier, out float newSeverity)
			{
				if(!TryGetBreastSize(hediff, out var breastSize))
				{
					newSeverity = 0;
					return false;
				}

				return Inversion.TryInvertBreastWeight(hediff.def, weightModifier(breastSize.weight), Internal.GetBodySize(hediff), out newSeverity, out _);
			}
		}
	}
}
