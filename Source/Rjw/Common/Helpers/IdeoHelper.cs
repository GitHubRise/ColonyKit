#nullable disable

using System.Collections.Generic;
using Verse;
using RimWorld;

using static rjw.PreceptDefOf;

namespace rjw
{
	public static class IdeoHelper
	{
		public static readonly List<PreceptDef> freeLovePrecepts;

		public static readonly List<PreceptDef> nonPolyPrecepts;

		public static readonly List<PreceptDef> prudePrecepts;

		public static bool ClassicMode => !ModsConfig.IdeologyActive || Find.IdeoManager.classicMode;

		static IdeoHelper()
		{
			freeLovePrecepts = new();
			if (ModsConfig.IdeologyActive)
			{	
				freeLovePrecepts.Add(Lovin_FreeApproved);
				nonPolyPrecepts = new() {
					Lovin_SpouseOnly_Mild,
					Lovin_SpouseOnly_Moderate,
					Lovin_SpouseOnly_Strict
				};
				if (RJWHookupSettings.FreeLovinMeansPoly)
				{
					freeLovePrecepts.Add(Lovin_Free);
				}
				else
				{
					nonPolyPrecepts.Add(Lovin_Free);
				}
				prudePrecepts = new() { Lovin_Prohibited, Lovin_Horrible };

			}
			else
			{
				if (RJWHookupSettings.FreeLovinMeansPoly)
				{
					freeLovePrecepts.Add(Lovin_Free);
				}
			}
		}

		/// <summary>
		/// 检查小人是否有带自由爱恋教条的意识形态。
		/// </summary>
		/// <param name="pawn">要检查其意识形态的小人</param>
		/// <returns>符合则为 true 等等，否则为 false</returns>
		/// <remarks>
		/// 	是否应尊重经典及非 DLC 意识形态中包含的隐藏“Lovin: Free”教条。<br />
		/// 	换言之，我们是否足够坚强去接受官方设定，还是恐惧地抱紧我们那套愚蠢的 21 世纪道德。
		/// </remarks>
		public static bool IsIdeologicallyPoly(this Pawn pawn)
		{
			if (pawn.Ideo == null)
			{
				return false;
			}
			if (ClassicMode)
			{
				return RJWHookupSettings.FreeLovinMeansPoly;
			}

			var lovinPrecept = GetLovinPreceptDef(pawn.Ideo);
			if (lovinPrecept == null)
			{
				return false;
			}
			return freeLovePrecepts.Contains(lovinPrecept);
		}

		public static bool IsIdeologicallyPrudish(this Pawn pawn)
		{
			if (pawn.Ideo == null || ClassicMode)
			{
				return false;
			}

			var lovinPrecept = GetLovinPreceptDef(pawn.Ideo);
			if (lovinPrecept == null)
			{
				return false;
			}

			return prudePrecepts.Contains(lovinPrecept);
		}

		private static PreceptDef GetLovinPreceptDef(Ideo ideo)
		{
			return ideo.PreceptsListForReading.FirstOrDefault(p => p.def.issue == IssueDefOf.Lovin)?.def;
		}
	}
}
