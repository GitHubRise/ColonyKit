#nullable disable

using Verse;
using UnityEngine;
using rjw.Modules.Shared.Extensions;

namespace rjw
{
	// 直接叫 'ISexPart' 会更好，但 ILewdablePart 已存在，且代表相似但不同的东西。
	public interface ISexPartHediff
	{
		HediffDef_SexPart Def { get; }

		T GetComp<T>() where T : HediffComp;

		HediffWithComps AsHediff { get; }
	}

	public static class ISexPartHediffExtensions
	{
		public static HediffComp_SexPart GetPartComp(this ISexPartHediff partHediff) => partHediff.GetComp<HediffComp_SexPart>();

		public static Pawn GetOwner(this ISexPartHediff part) => part.AsHediff.pawn;

		public static void DetectExternalSeverityChange(HediffWithComps hediff, float prevValue, float newValue)
		{
			if(Find.TickManager.Paused && prevValue != newValue)
			{
				if(hediff.TryGetComp<HediffComp_SexPart>(out var comp))
				{
					if (newValue != comp.GetSeverity())
					{
						ModLog.Message($"DetectExternalSeverityChange: {hediff.LabelBaseCap} ({hediff.pawn.GetName()}), {prevValue} -> {newValue}.");
						comp.SetSeverity(newValue, sync: false);
					}
				}
			}
		}
	}
}
