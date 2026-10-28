using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>「去蒸桑拿」的 JobGiver（DBH JoyGiver_UseSauna 的移植）：房间必须是桑拿房，找房内的座位坐下。</summary>
	public class JoyGiver_UseSauna : JoyGiver_InteractBuilding
	{
		protected override Job TryGivePlayJob(Pawn pawn, Thing t)
		{
			Room room = t.GetRoom();
			if (room?.Role == null || room.Role.defName != "CK_SaunaRoom")
			{
				return null;
			}
			if (pawn.health.hediffSet.HasHediff(HediffDefOf.Heatstroke))
			{
				return null;
			}
			foreach (Thing item in room.ContainedAndAdjacentThings.InRandomOrder())
			{
				if (item is Building building
					&& building.def.building.isSittable
					&& pawn.CanReserveSittableOrSpot(building.Position))
				{
					return JobMaker.MakeJob(def.jobDef, t, building.Position, building);
				}
			}
			return null;
		}
	}
}
