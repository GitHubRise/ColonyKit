#nullable disable

using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 小人尝试寻找动物来做爱/强暴。
	/// </summary>
	public class JobGiver_Bestiality : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.Drafted) return null;

			// 大多数检查现在在 ThinkNode_ConditionalBestiality 中完成

			if (!pawn.HasParts()) return null;

			if (!SexUtility.ReadyForLovin(pawn) || !xxx.is_hornyorfrustrated(pawn))
				return null;

			Pawn target = BreederHelper.FindAnimalToBreed(pawn);

			if (target == null) return null;

			Building_Bed petbed = target.ownership.OwnedBed;
			Building_Bed bed = pawn.ownership.OwnedBed;

			float animal_raped_chance = 0.5f;

			if (AfterSexUtility.AnimalParaphilia(target))
			{
				animal_raped_chance -= AfterSexUtility.AnimalParaphiliaStage(target) * 0.125f;
				//ModLog.Message("Animal raped chance: " + animal_raped_chance + " Animal Name:" + target.Name);
			}

			if (xxx.can_rape(pawn) && Rand.Chance(animal_raped_chance))
			{
				SexUtility.IncreaseTicksToNextHookup(pawn);
				return JobMaker.MakeJob(xxx.bestiality, target);
			}
			else if (xxx.can_fuck(pawn) && !target.Downed)
			{
				if (bed != null && petbed != null && target.CanReach(bed, PathEndMode.OnCell, Danger.Some) && pawn.CanReach(petbed, PathEndMode.OnCell, Danger.Some)) //两种床都可用，尝试选择最近的
				{
					if (pawn.Position.DistanceToSquared(petbed.Position) >= pawn.Position.DistanceToSquared(bed.Position))
					{
						SexUtility.IncreaseTicksToNextHookup(pawn);
						return JobMaker.MakeJob(xxx.bestialityForFemale, target, bed); //自己的床更近，去那里
					}
					else if (pawn.Position.DistanceToSquared(petbed.Position) < pawn.Position.DistanceToSquared(bed.Position))
					{
						SexUtility.IncreaseTicksToNextHookup(pawn);
						return JobMaker.MakeJob(xxx.bestialityForFemale, target, petbed); //宠物床更近，改去那里
					}
				}
				else if (bed != null && target.CanReach(bed, PathEndMode.OnCell, Danger.Some)) //只有小人床可用
				{
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.bestialityForFemale, target, bed); //前往小人床
				}
				else if (petbed != null && pawn.CanReach(petbed, PathEndMode.OnCell, Danger.Some)) //只有动物床可用
				{
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.bestialityForFemale, target, petbed); //前往动物床
				}
				else
				{
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.bestiality, target); //没有床时做爱的回退方案
				}
			}
			else if (xxx.can_be_fucked(pawn) && !target.Downed)
			{
				if (bed != null && petbed != null && target.CanReach(bed, PathEndMode.OnCell, Danger.Some) && pawn.CanReach(petbed, PathEndMode.OnCell, Danger.Some)) //两种床都可用，尝试选择最近的
				{
					if (pawn.Position.DistanceToSquared(petbed.Position) >= pawn.Position.DistanceToSquared(bed.Position))
					{
						SexUtility.IncreaseTicksToNextHookup(pawn);
						return JobMaker.MakeJob(xxx.bestialityForFemale, target, bed); //自己的床更近，去那里
					}
					else if (pawn.Position.DistanceToSquared(petbed.Position) < pawn.Position.DistanceToSquared(bed.Position))
					{
						SexUtility.IncreaseTicksToNextHookup(pawn);
						return JobMaker.MakeJob(xxx.bestialityForFemale, target, petbed); //宠物床更近，改去那里
					}
				}
				else if (bed != null && target.CanReach(bed, PathEndMode.OnCell, Danger.Some)) //只有小人床可用
				{
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.bestialityForFemale, target, bed); //前往小人床
				}
				else if (petbed != null && pawn.CanReach(petbed, PathEndMode.OnCell, Danger.Some)) //只有动物床可用
				{
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.bestialityForFemale, target, petbed); //前往动物床
				}
			}
			return null;
		}
	}
}
