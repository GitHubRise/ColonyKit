#nullable disable

using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 尝试给符合条件的动物分配配种工作。
	/// </summary>
	public class JobGiver_Breed : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn animal)
		{
			//ModLog.Message(" JobGiver_Breed::TryGiveJob( " + xxx.get_pawnname(animal) + " ) ReadyForLovin " + (SexUtility.ReadyForLovin(animal)));

			if (!SexUtility.ReadyForLovin(animal))
				return null;

			if (!animal.HasParts()) return null;

			//ModLog.Message(" ready to breed::is_healthy " + xxx.is_healthy(animal) + " can_rape " + xxx.can_rape(animal));
			if (xxx.is_healthy(animal) && xxx.can_rape(animal))
			{
				//搜索被指定为性爱目标的对象
				Pawn designated_target = BreederHelper.FindDesignatedBreeder(animal);
				if (designated_target != null)
				{
					return JobMaker.MakeJob(xxx.animalBreed, designated_target);
				}
			}
			return null;
		}
	}
}
