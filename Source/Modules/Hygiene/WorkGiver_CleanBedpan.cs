using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 找用过的便盆并派「清洗便盆」的活（Cleaning，和清旱厕同一套工作类型）。
	/// 水源用 <see cref="WaterSourceUtility.FindFillSource"/>：水井/水龙头/同网水箱，最后才退回地表水。
	/// </summary>
	public class WorkGiver_CleanBedpan : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_BedPan"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is ThingWithComps pan) || pan.GetComp<CompBedPan>()?.used != true)
			{
				return false;
			}
			if (pawn.Faction != Faction.OfPlayer
				|| t.IsForbidden(pawn)
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			if (t.IsBurning())
			{
				return false;
			}
			if (!WaterSourceUtility.FindFillSource(pawn, t.Position).IsValid)
			{
				JobFailReason.Is("CK.Hygiene.FloatMenu.NoWaterSource".Translate());
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			LocalTargetInfo source = WaterSourceUtility.FindFillSource(pawn, t.Position);
			if (!source.IsValid)
			{
				JobFailReason.Is("CK.Hygiene.FloatMenu.NoWaterSource".Translate());
				return null;
			}
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_CleanBedpan");
			if (def == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(def, t, source);
			job.count = 1;
			return job;
		}
	}
}
