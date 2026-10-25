#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using System;

namespace rjw
{
	public class JobDriver_SexBaseRecieverLoved : JobDriver_SexBaseReciever
	{
		protected override void DoSetup()
		{
			base.DoSetup();

			// 依据看法显示更多/更少的爱心。
			try
			{
				if (pawn.relations.OpinionOf(Partner) < 0)
					ticks_between_hearts += 50;
				else if (pawn.relations.OpinionOf(Partner) > 60)
					ticks_between_hearts -= 25;
			}
			catch
			{
				ModLog.Warning("Failed to resolve pawn relations, if on save load, this shouldn't matter too much.");
			}

			// 对于自愿性爱，征召接受者会打断该工作。
			this.FailOn(() => pawn.Drafted);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			DoSetup();
			// ModLog.Message("JobDriver_GettinLoved::MakeNewToils is called");
			// ModLog.Message("" + Partner.CurJob.def);

			if (Partner.CurJob.def == xxx.casual_sex) // 在床上做爱
			{
				this.KeepLyingDown(iBed);
				yield return Toils_Reserve.Reserve(iTarget, 1, 0);
				yield return Toils_Reserve.Reserve(iBed, Bed.SleepingSlotsCount, 0);

				var get_loved = MakeSexToil();
				// 不再绑定单一发起者的 job：群交时第一个发起者离开后，
				// 只要 parteners 里还有人，接受者就应当继续。
				yield return get_loved;
			}
			else if (Partner.CurJob.def == xxx.quick_sex)
			{
				yield return Toils_Reserve.Reserve(iTarget, 1, 0);

				var get_loved = MakeSexToil();
				get_loved.handlingFacing = false;
				yield return get_loved;
			}
			else if (Partner.CurJob.def == xxx.whore_is_serving_visitors)
			{
				this.FailOn(() => Partner.CurJob == null);
				yield return Toils_Reserve.Reserve(iTarget, 1, 0);

				var get_loved = MakeSexToil();
				get_loved.FailOn(() => Partner.CurJob?.def != xxx.whore_is_serving_visitors);
				yield return get_loved;
			}
			else if (Partner.CurJob.def == xxx.bestialityForFemale)
			{
				this.FailOn(() => Partner.CurJob == null);
				yield return Toils_Reserve.Reserve(iTarget, 1, 0);

				var get_loved = MakeSexToil();
				get_loved.FailOn(() => Partner.CurJob?.def != xxx.bestialityForFemale);
				yield return get_loved;
			}
			else if (Partner.CurJob.def == xxx.animalMate)
			{
				this.FailOn(() => Partner.CurJob == null);
				yield return Toils_Reserve.Reserve(iTarget, 1, 0);

				var get_loved = MakeSexToil();
				get_loved.FailOn(() => Partner.CurJob?.def != xxx.animalMate);
				yield return get_loved;
			}
		}

		private Toil MakeSexToil()
		{
			// 除随性性爱外，其他自愿性爱都在床上进行。
			var get_loved
				= Partner.CurJob.def != xxx.casual_sex ? new Toil()
				: Toils_LayDown.LayDown(iBed, true, false, false, false);

			get_loved.defaultCompleteMode = ToilCompleteMode.Never;
			get_loved.socialMode = RandomSocialMode.Off;
			get_loved.handlingFacing = true;
			get_loved.tickAction = () =>
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
			};
			get_loved.AddFinishAction(() =>
			{
				if (pawn?.GetCompRJW() is CompRJW comp)
				{
					comp.drawNude = false;
					pawn.Drawer.renderer.SetAllGraphicsDirty();
				}

				GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
			});

			return get_loved;
		}
	}

	// 出于某种原因被快速性爱工作使用。将来扩展？
	public class JobDriver_SexBaseRecieverQuickie : JobDriver_SexBaseRecieverLoved { }
}
