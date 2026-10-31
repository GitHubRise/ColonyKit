using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 选中殖民者后右键「小人」时的照护菜单：送水、喂水、清洁。
	/// 送水优先给瓶装水（凉白开/纯净水/矿泉水），没有瓶装水时退回拿水盆水桶去水源打水喂；
	/// 清洁复用医生给卧床病人擦洗的 <c>CK_WashPatient</c>。
	/// </summary>
	public class FloatMenuOptionProvider_PawnCare : FloatMenuOptionProvider
	{
		protected override bool Drafted => false;

		protected override bool Undrafted => true;

		protected override bool Multiselect => false;

		protected override bool RequiresManipulation => true;

		public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
		{
			Pawn carer = context.FirstSelectedPawn;
			if (carer == null
				|| clickedPawn == null
				|| clickedPawn == carer
				|| clickedPawn.Dead
				|| !clickedPawn.Spawned
				|| clickedPawn.Map != carer.Map
				|| !carer.IsColonistPlayerControlled
				|| !BringWaterUtility.IsColonyCaredFor(clickedPawn))
			{
				yield break;
			}

			yield return JobOption(
				"CK.Hygiene.FloatMenu.BringWater",
				carer,
				clickedPawn,
				() => BringWaterUtility.TryMakeJob(carer, clickedPawn),
				"CK.Hygiene.FloatMenu.NoWaterToDeliver");

			yield return JobOption(
				"CK.Hygiene.FloatMenu.FeedWater",
				carer,
				clickedPawn,
				() => WorkGiver_WaterIncapacitated.TryMakeFeedJob(carer, clickedPawn),
				"CK.Hygiene.FloatMenu.NeedContainer");

			yield return JobOption(
				"CK.Hygiene.FloatMenu.CleanPawn",
				carer,
				clickedPawn,
				() => WorkGiver_WashPatient.ShouldBeWashedBySomeone(clickedPawn)
					? WorkGiver_WashPatient.TryGiveWashJob(carer, clickedPawn)
					: null,
				"CK.Hygiene.FloatMenu.NotBedridden");
		}

		private static FloatMenuOption JobOption(
			string labelKey, Pawn carer, Pawn target, Func<Job> makeJob, string reasonKey)
		{
			string label = labelKey.Translate();
			if (makeJob() == null)
			{
				string reason = reasonKey.Translate();
				return new FloatMenuOption(
					label + " " + "CK.Hygiene.FloatMenu.Disabled".Translate(reason),
					null);
			}
			return new FloatMenuOption(
				label,
				() =>
				{
					// 点下去时重新造一次：这段时间里水可能被喝掉、水源可能被拆掉。
					Job job = makeJob();
					if (job != null)
					{
						carer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}
				},
				MenuOptionPriority.Default,
				null,
				target);
		}
	}
}
