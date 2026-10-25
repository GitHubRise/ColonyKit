#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	public class JobDriver_Knotted : JobDriver_Goto
	{
		/// <summary>
		/// 用于平滑开始与结束过渡的 tick 数量。
		/// </summary>
		const int TweenTicks = 10;

		/// <summary>
		/// 小人的伙伴。
		/// </summary>
		public Pawn Partner => job.GetTarget(TargetIndex.A).Pawn;

		/// <summary>
		/// 设置此项可对该小人施加位置偏移。
		/// </summary>
		private Vector3 forcedBodyOffset = Vector3.zero;
		public override Vector3 ForcedBodyOffset => forcedBodyOffset;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (job.def.waitAfterArriving <= 0)
				yield break;

			var waiter = Toils_General.Wait(job.def.waitAfterArriving);

			// TODO：更好地处理衣物。如果小人做爱时没有
			// 穿衣服，绘制时仍应保持裸体。由于结住是单独的
			// 工作，我们不知道这是否属于动物"配种"
			// 穿着衣服的对象这类情况，因此很难判断何时
			// 应保持裸体。这也许说明它本不该是
			// 单独的工作，而应是性爱工作中的一个额外 toil。

			// 以防万一。在结住期间，动物可能只会喘息、哀鸣或拉扯……
			if (xxx.is_animal(pawn))
				waiter.socialMode = RandomSocialMode.Off;

			// 我们应当已设置了伙伴。若没有，则使用回退行为
			// 即单纯等待（结住以往就是这样运作），不包含任何
			// 与伙伴相关的增强效果。
			if (Partner is not Pawn partner)
			{
				ModLog.Warning($"Knotting job for {xxx.get_pawnname(pawn)} did not receive a partner.");
				yield return waiter;
				yield break;
			}

			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOnDowned(TargetIndex.A);

			// 检测结住提前解除的情况。这可能由多种
			// 原因引起，但我常见到的是动物在结住期间
			// 开始接受训练。
			this.FailOn(() => !IsCouplingValid());

			// 如果其他模组补丁了此方法，可以用它来为等待方
			// 添加更复杂的动画或位置调整。
			AddAnimation(waiter);

			yield return waiter;
		}

		/// <summary>
		/// <para>在结住期间应用一些特殊的动画处理。</para>
		/// <para>动画系统可以补丁此方法，以挂接自己的动作，
		/// 从而实现更复杂的动画。</para>
		/// </summary>
		/// <param name="waiter">等待用的 toil。</param>
		void AddAnimation(Toil waiter)
		{
			if (Partner is not Pawn partner) return;

			// 只有当结住涉及动物时，才会把结住摆成臀对臀。
			// 有结的人形生物只会紧紧抱住伙伴。<3
			if (!InvolvesAnimal()) return;

			var initTicks = waiter.defaultDuration;
			var remTicks = initTicks;

			waiter.handlingFacing = true;
			waiter.AddPreTickAction(delegate ()
			{
				pawn.rotationTracker.Face(partner.DrawPos);
				pawn.Rotation = pawn.Rotation.Opposite;

				var newOffset = GetOffset();
				if (newOffset == Vector3.zero) return;

				// 在开始与结束时对偏移做插值过渡。
				// 不过，对于被打断的结住，确实无法做平滑处理。
				var tweenTicks = Math.Min(TweenTicks, Math.Min(initTicks - remTicks, remTicks));
				var curTween = (float)tweenTicks / TweenTicks;
				forcedBodyOffset = newOffset * curTween;

				remTicks -= 1;
			});
		}

		/// <summary>
		/// 获取当前小人的偏移量以改善视觉效果。
		/// </summary>
		/// <returns>要应用的偏移量。</returns>
		public Vector3 GetOffset()
		{
			// 必须是动物。
			if (!xxx.is_animal(pawn))
				return Vector3.zero;

			// 伙伴必须存在且为人类。
			if (Partner is not Pawn partner || xxx.is_animal(partner))
				return Vector3.zero;

			// 仅适用于朝东或朝西。
			if (pawn.Rotation != Rot4.East && pawn.Rotation != Rot4.West)
				return Vector3.zero;

			// 微调动物位置以更好地对齐。
			var offset = partner.BodySize * 0.15f;
			var sign = pawn.Rotation == Rot4.West ? -1f : 1f;
			return new Vector3(sign * offset, 0f, -0.2f);
		}

		/// <summary>
		/// 此结住是否至少涉及一只动物。
		/// </summary>
		/// <returns>伙伴和/或小人是否为动物。</returns>
		public bool InvolvesAnimal() => xxx.is_animal(pawn) || xxx.is_animal(Partner);

		/// <summary>
		/// <para>确保指定的伙伴仍与此小人处于结合状态。</para>
		/// <para>为了稳妥，这里实际会检查伙伴小人是否正在
		/// 使用 `JobDriver_Sex` 或 `JobDriver_Knotted`，并且其伙伴仍是
		/// 此小人。</para>
		/// </summary>
		/// <returns>结合是否有效。</returns>
		public bool IsCouplingValid()
		{
			if (Partner is not Pawn partner) return false;

			var result = partner.jobs.curDriver switch
			{
				JobDriver_Knotted knottedDriver => knottedDriver.Partner == pawn,
				JobDriver_Sex sexDriver => sexDriver.Partner == pawn,
				_ => false
			};
			if (result) return true;

			// 伙伴可能已排队准备结住，但正处于两个工作之间。
			if (partner.jobs.jobQueue.FirstOrDefault()?.job is not { } job) return false;
			if (job.def != xxx.knotted) return false;
			return job.AnyTargetIs(pawn);
		}
	}
}
