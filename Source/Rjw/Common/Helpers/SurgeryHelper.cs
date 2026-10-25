#nullable disable

using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace rjw
{
	public static class SurgeryHelper
	{
		// 快速粗略的方法，用于判断玩家是在摘取生殖器还是在切除它们
		// （例如因感染）。核心代码无法正确处理，因为它把私密部位的
		// 状态视为“不干净”。
		public static bool IsHarvest(Pawn p, BodyPartRecord part)
		{
			foreach (Hediff hed in p.health.hediffSet.hediffs)
			{
				if ((hed.Part?.def == part.def) && hed.def.isBad && (hed.Severity >= 0.70f))
					return false;
			}
			return true;
		}

		/// <summary>
		/// 从部位的状态生成该部位，并对所有子部位做同样处理（除非我们在主身体部位上）。 
		/// 同时移除部位状态，使 BodyPartRecord 在核心部位生成逻辑中可视为“干净”。
		/// </summary>
		/// <remarks>
		/// 目前这有些杀鸡用牛刀，因为部位被强制只出现在没有子部位的特定 BodyPartRecord 上， 
		/// 但希望有一天有人会做一个子模组，加入长着阴茎舌嘴巴阴道的种族之类的，
		/// 那时你们就会看到。
		/// <para>那时你们都会看到。</para>
		/// </remarks>
		public static void RemoveAndSpawnSexParts(Pawn surgeon, Pawn patient, BodyPartRecord part, bool isReplacement)
		{
			List<Hediff> parts = patient.health.hediffSet.hediffs.Where(h => h.Part == part && h is ISexPartHediff).ToList();
			foreach (var partHediff in parts)
			{
				Thing removedPart = SexPartAdder.recipePartRemover(partHediff);
				patient.health.RemoveHediff(partHediff);

				if (removedPart != null)
				{
					if (surgeon?.Map != null)
					{
						GenSpawn.Spawn(removedPart, surgeon.Position, surgeon.Map);
					}
					else if (patient.Map != null)
					{
						GenSpawn.Spawn(removedPart, patient.Position, patient.Map);
					}
				}

				if (isReplacement)
				{
					partHediff.Notify_SurgicallyReplaced(surgeon);
				}
				else
				{
					partHediff.Notify_SurgicallyRemoved(surgeon);
				}
			}

			if (part.IsCorePart)
			{
				return;
			}

			foreach (var subPart in part.parts)
			{
				RemoveAndSpawnSexParts(surgeon, patient, subPart, isReplacement);
			}
		}
	
	}
}
