#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;
using rjw.Modules.Attraction;

namespace rjw
{
	internal class JobDef_RapeEnemy : JobDef
	{
		public List<JobDef> interruptJobs;
		public List<string> TargetDefNames = new List<string>();
		public int priority = 0;

		protected JobDriver_RapeEnemy instance
		{
			get
			{
				if (_tmpInstance == null)
				{
					_tmpInstance = (JobDriver_RapeEnemy)Activator.CreateInstance(driverClass);
				}
				return _tmpInstance;
			}
		}

		private JobDriver_RapeEnemy _tmpInstance;

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			interruptJobs = new List<JobDef> { null, JobDefOf.LayDown, JobDefOf.Wait_Wander, JobDefOf.GotoWander, JobDefOf.AttackMelee };
		}

		public virtual bool CanUseThisJobForPawn(Pawn rapist)
		{
			bool busy = !interruptJobs.Contains(rapist.CurJob?.def);
			if (RJWSettings.DebugRape) ModLog.Message(" JobDef_RapeEnemy::CanUseThisJobForPawn( " + xxx.get_pawnname(rapist) + " ) - busy:" + busy + " with current job: " + rapist.CurJob?.def?.ToString());
			if (busy) return false;

			return instance.CanUseThisJobForPawn(rapist);// || TargetDefNames.Contains(rapist.def.defName);
		}

		public virtual Pawn FindVictim(Pawn rapist, Map m)
		{
			return instance.FindVictim(rapist, m);
		}
	}



	public class JobDriver_RapeEnemy : JobDriver_Rape
	{
		//覆盖 can_rape 机制
		protected bool requireCanRape = true;

		public virtual bool CanUseThisJobForPawn(Pawn rapist)
		{
			return xxx.is_human(rapist);
		}

		// 这大概没什么用，也许将来会派上用场
		public virtual bool considerStillAliveEnemies => true;

		[SyncMethod]
		public virtual Pawn FindVictim(Pawn rapist, Map m)
		{
			if (RJWSettings.DebugRape) ModLog.Message($"{this.GetType().ToString()}::TryGiveJob({xxx.get_pawnname(rapist)}) map {m?.ToString()}");
			if (rapist == null || m == null) return null;

			if (RJWSettings.DebugRape) ModLog.Message($" can rape = {xxx.can_rape(rapist)}");
			if (requireCanRape && !xxx.can_rape(rapist)) return null;

			List<Pawn> validTargets = new List<Pawn>();
			float min_fuckability = 0.10f;                          // 不要强暴可性度低于 10% 的小人
			float avg_fuckability = 0f;                             // 目标的平均可性度，选择高于该值的目标
			var valid_targets = new Dictionary<Pawn, float>();      // 有效小人及其可性度
			Pawn chosentarget = null;                               // 最终目标小人

			IEnumerable<Pawn> targets = m.mapPawns.AllPawnsSpawned.Where(x
				=> !x.IsForbidden(rapist) && x != rapist && x.HostileTo(rapist)
				&& IsValidTarget(rapist, x))
				.ToList();

			if (RJWSettings.DebugRape) ModLog.Message($" targets {targets.Count()}");

			if (targets.Any(x => IsBlocking(rapist, x)))	//如果有任一目标未倒地且可见——就不要继续强暴（你有更紧要的事要做）。
			{												//考虑到目标选择，这有点古怪。例如脆弱的小人会阻挡，而不脆弱的不会
				return null;
			}

			foreach (var target in targets)
			{
				if (!Pather_Utility.cells_to_target_rape(rapist, target.Position))
				{
					//if (RJWSettings.DebugRape) ModLog.Message($" {xxx.get_pawnname(target)} too far (cells) = {rapist.Position.DistanceToSquared(target.Position)}, skipping");
					if (RJWSettings.DebugRape) ModLog.Message($" {xxx.get_pawnname(target)} too far (cells) = {rapist.Position.DistanceTo(target.Position)}, skipping");
					continue;// 太远
				}

				float fuc = GetFuckability(rapist, target);

				if (fuc > min_fuckability)
				{
					if (Pather_Utility.can_path_to_target(rapist, target.Position))
						valid_targets.Add(target, fuc);
					else
						if (RJWSettings.DebugRape) ModLog.Message($" {xxx.get_pawnname(target)} too far (path), skipping");
				}
				else
					if (RJWSettings.DebugRape) ModLog.Message($" {xxx.get_pawnname(target)} fuckability too low = {fuc}, skipping");

			}
			if (RJWSettings.DebugRape) ModLog.Message($" fuckable targets {valid_targets.Count()}");

			if (valid_targets.Any())
			{
				avg_fuckability = valid_targets.Average(x => x.Value);
				if (RJWSettings.DebugRape) ModLog.Message($" avg_fuckability {avg_fuckability}");

				// 选择可性度高于平均的小人
				var valid_targetsFiltered = valid_targets.Where(x => x.Value >= avg_fuckability);
				if (RJWSettings.DebugRape) ModLog.Message($" targets above avg_fuckability {valid_targetsFiltered.Count()}");

				if (valid_targetsFiltered.Any())
					chosentarget = valid_targetsFiltered.RandomElement().Key;
			}

			return chosentarget;
		}

		bool IsBlocking(Pawn rapist, Pawn target)
		{
			return considerStillAliveEnemies && !target.Downed && rapist.CanSee(target);
		}

		bool IsValidTarget(Pawn rapist, Pawn target)
		{

			if (!RJWSettings.bestiality_enabled)
			{
				if (xxx.is_animal(target) && xxx.is_human(rapist))
				{
					//兽交已禁用，跳过。
					return false;
				}
				if (xxx.is_animal(rapist) && xxx.is_human(target))
				{
					//兽交已禁用，跳过。
					return false;
				}
			}

			if (!RJWSettings.animal_on_animal_enabled)
				if ((xxx.is_animal(target) && xxx.is_animal(rapist)))
				{
					//动物对动物已禁用，跳过。
					return false;
				}

			if (!RJWSettings.mechanophilia_enabled)
			{
				if (xxx.is_mechanoid(rapist) || xxx.is_mechanoid(target))
				{
					//机械恋已禁用，跳过。
					return false;
				}
			}

			if ((xxx.is_mechanoid(rapist) && xxx.is_animal(target)) || (xxx.is_animal(rapist) && xxx.is_mechanoid(target)))
				return false; //没有机械体对动物的行为

			if (target.CurJob?.def == xxx.gettin_raped || target.CurJob?.def == xxx.gettin_loved)
			{
				//已经在和某人做爱，跳过，把机会留给其他受害者。
				return false;
			}

			return Can_rape_Easily(target) &&
				(xxx.is_human(target) || xxx.is_animal(target)) &&
				rapist.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, xxx.max_rapists_per_prisoner, 0);
		}

		public virtual float GetFuckability(Pawn rapist, Pawn target)
		{
			float fuckability = 0;
			if (target.health.hediffSet.HasHediff(xxx.submitting)) // 现在无关吸引力，而是要显示谁是谁的狗
			{
				fuckability = 2 * AttractionUtility.Evaluate(rapist, target, AttractionPurpose.ForRape);
			}
			else if (SexAppraiser.InMoodForRape(rapist))
			{
				fuckability = AttractionUtility.Evaluate(rapist, target, AttractionPurpose.ForRape);
			}

			if (RJWSettings.DebugRape) ModLog.Message($"JobDriver_RapeEnemy::GetFuckability({xxx.get_pawnname(rapist)}, {xxx.get_pawnname(target)})");

			return fuckability;
		}

		protected bool Can_rape_Easily(Pawn pawn)
		{
			return xxx.can_get_raped(pawn) && !pawn.IsBurning();
		}
	}
}
