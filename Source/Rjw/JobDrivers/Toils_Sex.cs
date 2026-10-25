#nullable disable

using Verse;
using Verse.AI;

namespace rjw
{
	public static class Toils_Sex
	{
		/// <summary>
		/// 用于提醒玩家 <paramref name="rapist"/> 正试图强暴 <paramref name="target"/> 的 toil
		/// </summary>
		public static Toil RapeTargetAlert(Pawn rapist, Pawn target)
		{
			Toil toil = ToilMaker.MakeToil();
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			toil.initAction = () => SexUtility.RapeTargetAlert(rapist, target);
			return toil;
		}
	}
}
