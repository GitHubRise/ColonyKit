using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 清理一块污渍。逻辑与原版 JobDriver_CleanFilth 相同，但**不限制家区域**，
	/// 这样「清洁」才能处理基地外战后的血迹；范围由 JobGiver 选择。
	/// </summary>
	public class JobDriver_NannyCleanFilth : JobDriver
	{
		private const TargetIndex FilthInd = TargetIndex.A;

		private float cleaningWorkDone;
		private float totalCleaningWorkDone;
		private float totalCleaningWorkRequired;

		private Filth Filth => job.GetTarget(FilthInd).Thing as Filth;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return Filth != null
				&& pawn.Reserve(job.GetTarget(FilthInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(FilthInd);

			yield return Toils_Goto.GotoThing(FilthInd, PathEndMode.Touch);

			Toil clean = ToilMaker.MakeToil("CK_NannyCleanFilth");
			clean.defaultCompleteMode = ToilCompleteMode.Never;
			clean.FailOnDespawnedNullOrForbidden(FilthInd);
			clean.initAction = () =>
			{
				cleaningWorkDone = 0f;
				totalCleaningWorkDone = 0f;
				Filth filth = Filth;
				totalCleaningWorkRequired = filth == null
					? 0f
					: filth.def.filth.cleaningWorkToReduceThickness * filth.thickness;
			};
			clean.tickIntervalAction = delta =>
			{
				Filth filth = Filth;
				if (filth == null || filth.Destroyed)
				{
					ReadyForNextToil();
					return;
				}

				float terrainFactor = filth.Position
					.GetTerrain(filth.Map)
					.GetStatValueAbstract(StatDefOf.CleaningTimeFactor);
				float speed = pawn.GetStatValue(StatDefOf.CleaningSpeed) * delta;
				if (terrainFactor != 0f)
				{
					speed /= terrainFactor;
				}

				cleaningWorkDone += speed;
				totalCleaningWorkDone += speed;
				if (cleaningWorkDone > filth.def.filth.cleaningWorkToReduceThickness)
				{
					filth.ThinFilth();
					cleaningWorkDone = 0f;
					if (filth.Destroyed)
					{
						pawn.records.Increment(RecordDefOf.MessesCleaned);
						ReadyForNextToil();
					}
				}
			};
			clean.WithEffect(EffecterDefOf.Clean, FilthInd);
			clean.WithProgressBar(
				FilthInd,
				() => totalCleaningWorkRequired <= 0f
					? 0f
					: totalCleaningWorkDone / totalCleaningWorkRequired,
				interpolateBetweenActorAndTarget: true);
			clean.PlaySustainerOrSound(() =>
			{
				SoundDef sound = Filth?.def?.filth?.cleaningSound;
				return sound.NullOrUndefined() ? SoundDefOf.Interact_CleanFilth : sound;
			});
			yield return clean;
		}
	}
}
