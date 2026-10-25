#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace rjw
{
	/// <summary>
	/// ChJees 的 Androids 模组的辅助类。
	/// </summary>
	[StaticConstructorOnStartup]
	public static class AndroidsCompatibility
	{
		public static Type androidCompatType;
		public static readonly string typeName = "Androids.SexualizeAndroidRJW";
		private static bool foundType;

		static AndroidsCompatibility()
		{
			try
			{
				androidCompatType = Type.GetType(typeName);
				foundType = true;
				//ModLog.Message("Found Type: Androids.SexualizeAndroidRJW");
			}
			catch
			{
				foundType = false;
				//ModLog.Message("Did NOT find Type: Androids.SexualizeAndroidRJW");
			}
		}

		/*private static bool TestPredicate(DefModExtension extension)
		{
			if (extension == null)
				return false;

			ModLog.Message($"Predicate: {extension} : {extension.GetType()?.FullName}");
			return extension.GetType().FullName == typeName;
		}*/

		public static bool IsAndroid(ThingDef def)
		{
			if (def == null || !foundType)
			{
				return false;
			}

			return def.modExtensions != null && def.modExtensions.Any(extension => extension.GetType().FullName == typeName);
		}

		public static bool IsAndroid(Thing thing)
		{
			return IsAndroid(thing.def);
		}

		public static bool AndroidPenisFertility(Pawn pawn)
		{
			//安卓只有使用超凡科技部位时才有生育力
			BodyPartRecord Part = Genital_Helper.get_genitalsBPR(pawn);
			return (pawn.health.hediffSet.hediffs.Any((Hediff hed) =>
				(hed.Part == Part) &&
				(hed.def == Genital_Helper.archotech_penis)
				));
		}

		public static bool AndroidVaginaFertility(Pawn pawn)
		{
			//安卓只有使用超凡科技部位时才有生育力
			BodyPartRecord Part = Genital_Helper.get_genitalsBPR(pawn);
			return (pawn.health.hediffSet.hediffs.Any((Hediff hed) =>
				(hed.Part == Part) &&
				(hed.def == Genital_Helper.archotech_vagina)
				));
		}

	}
}
