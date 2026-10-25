using Verse;
using Verse.AI;

namespace rjw
{
	public static class Toils_Notify
	{
		public static Toil Notify_MemberRaped(Pawn rapist, Pawn victim)
		{
			Toil toil = ToilMaker.MakeToil();
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			toil.initAction = () => SexUtility.Notify_MemberRaped(rapist, victim);
			return toil;
		}
	}
}
