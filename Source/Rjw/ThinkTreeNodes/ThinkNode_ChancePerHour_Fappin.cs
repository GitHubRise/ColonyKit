#nullable disable

using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace rjw
{
	public class ThinkNode_ChancePerHour_Fappin : ThinkNode_ChancePerHour
	{
		public static float get_fappin_mtb_hours(Pawn pawn)
		{
			float result = 1f;
			result /= 1f - pawn.health.hediffSet.PainTotal;
			
			float efficiency = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
			if (efficiency < 0.5f)
			{
				result /= efficiency * 2f;
			}

			if (!pawn.RaceProps.Humanlike)
			{
				return result * 4f;
			}
			if (RimWorld.LovePartnerRelationUtility.ExistingLovePartner(pawn) != null)
			{
				result *= 2f; //这个系数会让有爱人的小人更不容易自慰/随机强暴/强暴 CP/兽交/恋尸。
			}
			else if (pawn.gender == Gender.Male)
			{
				result /= 1.25f; //这里考虑了单身男性
			}

			result /= GenMath.FlatHill(0.0001f, 8f, 13f, 28f, 50f, 0.15f, pawn.ageTracker.AgeBiologicalYearsFloat);//这里需要修改

			if (xxx.is_nympho(pawn))
			{
				result /= 2f;
			}

			return result;
		}

		private float GetAloneFactor(Pawn p)
		{
			bool isAlone = !p.Map.mapPawns.AllPawnsSpawned.Any(x => p.CanSee(x) && xxx.is_human(x));
			// 独自一人时更可能自慰。
			return isAlone ? 0.6f : 1.2f;
		}

		protected override float MtbHours(Pawn p)
		{
			//没有性需求——就不自慰
			var sex_need = p?.needs?.TryGetNeed<Need_Sex>();
			if (sex_need == null)
				return -1.0f;

			// 动物不会自慰……至少目前如此。
			// 将来也许会对怪物娘之类的启用，但那需要改动代码以避免报错。
			if (xxx.is_animal(p))
				return -1.0f;

			bool is_horny = xxx.is_hornyorfrustrated(p);
			if (is_horny)
			{
				// 裸体时更可能自慰。
				float clothingFactor = p.apparel.PsychologicallyNude ? 0.8f : 1.0f;

				float SexNeedFactor = (4 - xxx.need_some_sex(p)) / 2f;
				return get_fappin_mtb_hours(p) * SexNeedFactor * GetAloneFactor(p) * clothingFactor;
			}

			return -1.0f;
		}
	}
}
