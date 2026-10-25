#nullable disable

using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace rjw
{
	public class JobDriver_SexBaseReciever : JobDriver_Sex
	{
		// 给这个可怜的驱动一点爱，而不只是 (Partner.jobs?.curDriver is JobDriver_SexBaseReciever)
		public List<Pawn> parteners = new();

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref parteners, "parteners", LookMode.Reference);
		}

		/// <summary>
		/// 添加一个伙伴必须满足才被视为有效的谓词。
		/// </summary>
		/// <param name="predicate">该谓词。</param>
		public void AddPartnerCondition(Predicate<Pawn> predicate) =>
			this.FailOn(() => parteners.Count > 0 && !parteners.Any(predicate));

		protected virtual void DoSetup()
		{
			setup_ticks();

			// 添加性爱发起者，以免在其开始工作前就失败。
			parteners.AddDistinct(Partner);

			// 处理在无剩余伙伴时结束工作。
			AddEndCondition(() => parteners.Count <= 0 ? JobCondition.Succeeded : JobCondition.Ongoing);

			// 这些检查确保至少还有一个伙伴有效。在完美的世界里，
			// 伙伴本该在不再
			// 试图干这个小人的时候自行移除……但这不是一个完美的世界。
			AddPartnerCondition(MustBeSpawned);
			AddPartnerCondition(MustBeAwake);
			AddPartnerCondition(MustNotBeDrafted);
			AddPartnerCondition(MustBeMySexInitiator);
		}

		/// <summary>
		/// 检查伙伴确实已生成在此地图上。
		/// </summary>
		protected bool MustBeSpawned(Pawn partner) =>
			partner.Spawned && partner.Map == pawn.Map;

		/// <summary>
		/// 检查伙伴是否能够保持清醒。
		/// </summary>
		protected bool MustBeAwake(Pawn partner) =>
			partner.health.capacities.CanBeAwake;

		/// <summary>
		/// 检查伙伴未被征召。
		/// </summary>
		protected bool MustNotBeDrafted(Pawn partner) =>
			!partner.Drafted;
		
		/// <summary>
		/// 检查伙伴仍在试图干此驱动的小人。
		/// </summary>
		protected bool MustBeMySexInitiator(Pawn partner) =>
			(partner.jobs.curDriver as JobDriver_SexBaseInitiator)?.Partner == pawn;
	}
}
