#nullable disable

using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 用于判断该动物是否有资格从事繁殖工作
	/// </summary>
	public class ThinkNode_ConditionalCanBreed : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			//ModLog.Message("ThinkNode_ConditionalCanBreed " + p);

			//Rimworld of Magic 中变形的人形生物也会获得动物思考节点
			//if (p.Faction != null && p.Faction.IsPlayer)
			//	ModLog.Message("ThinkNode_ConditionalCanBreed " + xxx.get_pawnname(p) + " is animal: " + xxx.is_animal(p));

			// 人形生物没有繁殖工作，那由兽交处理。
			if (!xxx.is_animal(p))
				return false;

			// 动物相关内容已禁用。
			if (!RJWSettings.bestiality_enabled && !RJWSettings.animal_on_animal_enabled)
				return false;

			//return p.IsDesignatedBreedingAnimal() || RJWSettings.WildMode;
			return p.IsDesignatedBreedingAnimal();
		}
	}
}
