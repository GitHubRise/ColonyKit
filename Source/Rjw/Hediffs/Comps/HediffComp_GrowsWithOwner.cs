#nullable disable

using System.Collections.Generic;
using Verse;
using RimWorld;
using LudeonTK;

namespace rjw
{

	
	public class HediffComp_GrowsWithOwner : HediffComp
	{
		public float lastOwnerSize = -1;

		private const int UpdateInterval = GenTicks.TickRareInterval;

		public static bool ForceUpdate = false;
		public void UpdatePartSize(HediffComp_SexPart partComp)
		{
			//从 calculation() 获取（~）相对大小变量
			var relativeSize = partComp.baseSize;

			//移除旧的 lifestage 体型修改
			relativeSize /= partComp.originalOwnerSize;

			//添加新的 lifestage 体型修改
			relativeSize *= Pawn.BodySize;

			//保存新的大小
			partComp.baseSize = relativeSize;

			//更新拥有者的大小
			partComp.originalOwnerSize = Pawn.BodySize;

			// 更新/重置部位大小
			partComp.UpdateSeverity();
		}
		public HediffCompProperties_GrowsWithOwner Props => (HediffCompProperties_GrowsWithOwner)props;

		public override void CompPostTickInterval(ref float severityAdjustment, int delta)
		{
			// 天然部位相对于拥有者生长是合理的。
			// 当小人体型变大时，移植或仿生部位（相对）看起来会缩小。

			// 该组件在 bodySize 变化时更新 baseSize，使部位严重度（相对大小）保持固定。			
			// 部位随 lifeStage（青春期）产生的额外生长/缩小由 lifeStageFactor 处理，与本组件无关。

			// 仅在间隔时更新以提升性能
			if (!ForceUpdate && !Pawn.IsHashIntervalTick(UpdateInterval, delta))
				return;
			ForceUpdate = false;

			// 若自上次检查以来体型未变化则跳过
			if (Pawn.BodySize == lastOwnerSize) return;

			// 尝试获取性部位组件
			if (!parent.TryGetComp<HediffComp_SexPart>(out var partComp))
			{
				ModLog.WarningOnce($"HediffComp_SexPart missing on {parent.def.defName}", parent.def.GetHashCode() + 450720);
				return;
			}

			//移植部位不做改动
			if (partComp.isTransplant && !Props.evenIfTransplanted) return;
			UpdatePartSize(partComp);
			lastOwnerSize = Pawn.BodySize;
		}

		public override void CompExposeData()
		{
			base.CompExposeData();
			Scribe_Values.Look(ref lastOwnerSize, "lastOwnerSize");
		}
	}
}
