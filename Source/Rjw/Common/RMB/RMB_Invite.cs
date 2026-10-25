#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 「邀请加入」：主控正在进行性行为时，把附近的小人拉进这一场。
	/// 同时供右键菜单（<see cref="GetOptions"/>）与指令栏按钮（<see cref="GetMenuOptions"/>）使用。
	/// </summary>
	public static class RMB_Invite
	{
		/// <summary>右键菜单用：针对点击到的具体小人，失败时给出原因文本。</summary>
		public static IEnumerable<FloatMenuOption> GetOptions(Pawn inviter, Pawn invitee)
		{
			if (inviter == null || invitee == null || inviter == invitee) yield break;
			if (!TryGetReceiver(inviter, out Pawn receiver)) yield break;
			if (receiver == invitee) yield break;

			string label = "RJW_RMB_InviteJoin".Translate(invitee);
			string reasonKey = CheckInvite(inviter, invitee, receiver);
			if (reasonKey != null)
			{
				yield return new FloatMenuOption(label + "：" + reasonKey.Translate(), null);
				yield break;
			}

			FloatMenuOption option = new FloatMenuOption(label, delegate { Invite(inviter, invitee, receiver); }, MenuOptionPriority.High);
			yield return FloatMenuUtility.DecoratePrioritizedTask(option, invitee, receiver);
		}

		/// <summary>指令栏按钮用：列出所有当前可以加入的小人。</summary>
		public static IEnumerable<FloatMenuOption> GetMenuOptions(Pawn inviter)
		{
			if (inviter?.Map == null) yield break;
			if (!TryGetReceiver(inviter, out Pawn receiver)) yield break;

			foreach (Pawn candidate in inviter.Map.mapPawns.AllPawnsSpawned)
			{
				if (candidate == null || candidate == inviter || candidate == receiver || candidate.Dead) continue;
				if (!IsAllowedCandidateType(candidate)) continue;
				if (!IsInRange(inviter, receiver, candidate)) continue;
				if (CheckInvite(inviter, candidate, receiver) != null) continue;

				FloatMenuOption option = new FloatMenuOption(
					"RJW_RMB_InviteJoin".Translate(candidate),
					delegate { Invite(inviter, candidate, receiver); },
					MenuOptionPriority.High);
				yield return FloatMenuUtility.DecoratePrioritizedTask(option, candidate, receiver);
			}
		}

		/// <summary>取出主控当前的接受者；不满足“正在自愿性行为中”时返回 false。</summary>
		public static bool TryGetReceiver(Pawn inviter, out Pawn receiver)
		{
			receiver = null;

			// 自己就是接受者（双飞时选中男方）：被邀请的人会以发起者身份加入这一场。
			if (inviter?.jobs?.curDriver is JobDriver_SexBaseReciever and not JobDriver_SexBaseRecieverRaped)
			{
				receiver = inviter;
				return true;
			}

			// RJW 自己的性行为。
			if (inviter?.jobs?.curDriver is JobDriver_SexBaseInitiator initiator)
			{
				receiver = initiator.Partner;
			}
			// 原版恋人亲热：RJW 会在结束时补结算，进行中同样允许邀请（邀请时再转换成 RJW 驱动）。
			else if (inviter?.jobs?.curDriver is JobDriver_Lovin lovin)
			{
				receiver = lovin.Partner;
			}
			else
			{
				return false;
			}

			if (receiver == null)
				return false;

			// 强暴不接受邀请；这里按驱动类型判断，不依赖 Sexprops 是否已经写入。
			if (receiver.jobs?.curDriver is JobDriver_SexBaseRecieverRaped)
				return false;

			// 接受者要么已经在 RJW 的接收者 job 里，要么还在原版 Lovin 里。
			if (receiver.jobs?.curDriver is JobDriver_SexBaseReciever)
				return true;

			return receiver.jobs?.curDriver is JobDriver_Lovin;
		}

		/// <summary>检查某个人能否加入；返回 null 表示可以，否则返回不可用的语言键。</summary>
		private static string CheckInvite(Pawn inviter, Pawn invitee, Pawn receiver)
		{
			if (!IsAllowedCandidateType(invitee))
				return "RJW_RMB_InviteUnavailable";

			// 没有 CompRJW 的小人（未性化化种族/机器人）无法进入 RJW 的性行为流程。
			if (invitee.GetCompRJW() == null)
				return "RJW_RMB_InviteUnavailable";

			if (invitee.Dead || invitee.Downed || invitee.Drafted || invitee.jobs == null)
				return "RJW_RMB_InviteUnavailable";

			// 俘虏与奴隶无条件服从：跳过敌意与性向判定。
			bool mustObey = invitee.IsPrisonerOfColony || xxx.is_slave(invitee);

			if (!mustObey && invitee.HostileTo(receiver))
				return "RJW_RMB_InviteHostile";

			// 注意：这里不用 CasualSex_Helper.CanJoinOngoingBedSex ——
			// 那个判断要求接受者的 Sexprops 已经写入，而邀请按钮可能在 Sexprops 写入前就被点到。
			if (!CanJoinOngoingAct(inviter, receiver, invitee))
				return "RJW_RMB_InviteNoRoom";

			// 双飞：新人是「额外接受者」时，要按发起者的容量再算一次。
			if (WouldJoinAsReceiver(inviter, invitee, receiver))
			{
				int cap = xxx.max_sex_partners_for(invitee, receiver);
				int extras = inviter.jobs?.curDriver is JobDriver_SexBaseInitiator initiatorDriver
					? initiatorDriver.extraReceivers?.Count ?? 0
					: 0;
				if (2 + extras >= cap)
					return "RJW_RMB_InviteNoRoom";
			}

			if (invitee.jobs.curDriver is JobDriver_Sex)
				return "RJW_RMB_InviteUnavailable";

			// 只要对场上任意一方有兴趣就算愿意：只看接受者会把「对发起者有性趣」的人误杀。
			if (!mustObey && !IsAttractedToAny(inviter, invitee, receiver))
				return "RJW_RMB_InviteNotInterested";

			if (!CanReachForInvite(invitee, receiver, mustObey))
				return "CannotReach";

			return null;
		}

		/// <summary>
		/// 只把这几类人类算作候选：殖民者、囚犯、商队、访客、野人、流浪者。
		/// 动物与机械族不参与计算。
		/// </summary>
		public static bool IsAllowedCandidateType(Pawn candidate)
		{
			if (candidate == null || !xxx.is_human(candidate)) return false;
			if (candidate.RaceProps != null && candidate.RaceProps.IsMechanoid) return false;

			// 殖民者（含奴隶）
			if (candidate.IsColonist || candidate.IsSlaveOfColony) return true;

			// 囚犯
			if (candidate.IsPrisonerOfColony || candidate.guest?.GuestStatus == GuestStatus.Prisoner) return true;

			// 访客 / 商队
			if (candidate.guest?.GuestStatus == GuestStatus.Guest) return true;
			if (candidate.trader != null) return true;

			// 野人
			if (WildManUtility.IsWildMan(candidate)) return true;

			// 流浪者（无派系）
			if (candidate.Faction == null) return true;

			return false;
		}

		/// <summary>囚犯/奴隶被邀请时可以忽略禁行限制再试一次（同监狱不同牢房很常见）。</summary>
		private static bool CanReachForInvite(Pawn invitee, Pawn receiver, bool mustObey)
		{
			if (invitee.CanReach(receiver, PathEndMode.OnCell, Danger.Deadly)) return true;
			if (!mustObey) return false;

			return invitee.CanReach(receiver, PathEndMode.OnCell, Danger.Deadly, false, true);
		}

		/// <summary>被邀请者是否适合作为「额外接受者」加入（双飞：男发起、女接受）。</summary>
		public static bool WouldJoinAsReceiver(Pawn inviter, Pawn invitee, Pawn receiver)
		{
			if (inviter == null || invitee == null) return false;
			// 选中的人本身就是接受者时，来者按发起者处理。
			if (inviter == receiver) return false;
			if (inviter.jobs?.curDriver is not JobDriver_SexBaseInitiator) return false;
			if (!xxx.can_fuck(inviter)) return false;
			if (!xxx.can_be_fucked(invitee)) return false;

			return true;
		}

		/// <summary>被邀请者是否对场上任意一方有性趣（发起者或接受者）。</summary>
		private static bool IsAttractedToAny(Pawn inviter, Pawn invitee, Pawn receiver)
		{
			if (OrientationUtility.PreferenceFactor(invitee, receiver) > 0f) return true;
			if (inviter != null && inviter != receiver && OrientationUtility.PreferenceFactor(invitee, inviter) > 0f) return true;
			return false;
		}

		/// <summary>候选范围：同一张地图，且与床同房间或足够近，避免把全地图的人都算进来。</summary>
		public static bool IsInRange(Pawn inviter, Pawn receiver, Pawn candidate)
		{
			if (inviter?.Map == null || candidate?.Map == null) return false;
			if (candidate.Map != inviter.Map) return false;

			// 监狱场景：囚犯之间互相邀请不看距离/房间，只要同在地图上。
			if (inviter.IsPrisonerOfColony && candidate.IsPrisonerOfColony) return true;

			Building_Bed bed = FindBed(inviter, receiver);
			int range = RJWPreferenceSettings.InviteRange;
			if (bed == null)
				return inviter.Position.DistanceTo(candidate.Position) <= range;

			Room bedRoom = bed.GetRoom(RegionType.Set_Passable);
			Room candidateRoom = candidate.GetRoom(RegionType.Set_Passable);
			if (bedRoom != null && bedRoom == candidateRoom) return true;

			return bed.Position.DistanceTo(candidate.Position) <= range;
		}

		/// <summary>找到这场性行为使用的床：RJW 的接收者 job 不在 Pawn.InBed 的判定里，需要从任务目标取。</summary>
		public static Building_Bed FindBed(Pawn inviter, Pawn receiver)
		{
			Building_Bed bed = receiver?.CurrentBed();
			if (bed != null) return bed;

			if (inviter?.jobs?.curDriver is JobDriver_Sex sex && sex.Bed != null)
				return sex.Bed;

			if (inviter?.jobs?.curDriver is JobDriver_Lovin lovin && lovin.Bed != null)
				return lovin.Bed;

			return receiver?.ownership?.OwnedBed;
		}

		/// <summary>接受者是否还能被加入：自愿、在床上、未达人数上限。</summary>
		public static bool CanJoinOngoingAct(Pawn inviter, Pawn receiver, Pawn invitee)
		{
			if (receiver == null || invitee == null) return false;
			if (!xxx.is_human(receiver) || !xxx.is_human(invitee)) return false;
			if (FindBed(inviter, receiver) == null) return false;

			if (receiver.jobs?.curDriver is JobDriver_SexBaseRecieverRaped) return false;

			int cap = xxx.max_sex_partners_for(invitee, receiver);

			if (receiver.jobs?.curDriver is JobDriver_SexBaseReciever recv)
				return recv.parteners.Count < cap;

			// 原版恋人亲热：场上已经有两个人在做。
			if (receiver.jobs?.curDriver is JobDriver_Lovin)
				return cap > 2;

			return false;
		}

		/// <summary>统计没人能加入时的原因，供指令栏按钮给出可读提示。</summary>
		public static void DescribeFailures(Pawn inviter, out int nearby, out int noRoom, out int notInterested, out int other)
		{
			nearby = 0;
			noRoom = 0;
			notInterested = 0;
			other = 0;

			if (inviter?.Map == null) return;
			if (!TryGetReceiver(inviter, out Pawn receiver)) return;

			foreach (Pawn candidate in inviter.Map.mapPawns.AllPawnsSpawned)
			{
				if (candidate == null || candidate == inviter || candidate == receiver || candidate.Dead) continue;
				if (!IsAllowedCandidateType(candidate)) continue;
				if (!IsInRange(inviter, receiver, candidate)) continue;

				string reason = CheckInvite(inviter, candidate, receiver);
				if (reason == null) continue;

				nearby++;
				if (reason == "RJW_RMB_InviteNoRoom") noRoom++;
				else if (reason == "RJW_RMB_InviteNotInterested") notInterested++;
				else other++;
			}
		}

		/// <summary>把当前接受者与候选人的判定细节写进日志，便于定位「没人可邀」。</summary>
		public static void LogDiagnostics(Pawn inviter)
		{
			if (inviter == null) return;

			bool hasReceiver = TryGetReceiver(inviter, out Pawn receiver);
			ModLog.Message($"[InviteJoin] inviter={inviter.LabelShort}, hasReceiver={hasReceiver}");
			if (!hasReceiver) return;

			int cap = xxx.max_sex_partners_for(inviter, receiver);
			int partners = receiver.jobs?.curDriver is JobDriver_SexBaseReciever recv ? recv.parteners.Count : -1;
			ModLog.Message(
				$"[InviteJoin] receiver={receiver.LabelShort}, driver={receiver.jobs?.curDriver?.GetType().Name}, " +
				$"inBed={receiver.InBed()}, bedFound={FindBed(inviter, receiver) != null}, cap={cap}, partners={partners}");

			int logged = 0;
			foreach (Pawn candidate in inviter.Map.mapPawns.AllPawnsSpawned)
			{
				if (candidate == null || candidate == inviter || candidate == receiver || candidate.Dead) continue;
				if (!IsInRange(inviter, receiver, candidate)) continue;
				if (!IsAllowedCandidateType(candidate)) continue;

				string reason = CheckInvite(inviter, candidate, receiver);
				if (reason == null)
				{
					ModLog.Message($"[InviteJoin] accept {candidate.LabelShort}");
					continue;
				}

				if (logged < 10)
				{
					ModLog.Message($"[InviteJoin] reject {candidate.LabelShort}: {reason}");
					logged++;
				}
			}
		}

		/// <summary>邀请下单后的验证记录：确认对方是否真的进入了这场性行为。</summary>
		private sealed class PendingInvite
		{
			public Pawn inviter;
			public Pawn invitee;
			public Pawn receiver;
			public bool asReceiver;
			public int expireTick;
		}

		private static readonly List<PendingInvite> pendingInvites = new();

		/// <summary>登记一次邀请，稍后验证是否真的加入。</summary>
		private static void TrackInvite(Pawn inviter, Pawn invitee, Pawn receiver, bool asReceiver)
		{
			if (invitee == null || receiver == null) return;

			pendingInvites.Add(new PendingInvite
			{
				inviter = inviter,
				invitee = invitee,
				receiver = receiver,
				asReceiver = asReceiver,
				expireTick = Find.TickManager.TicksGame + 1200
			});

			ModLog.Message(
				$"[InviteJoin] order invitee={invitee.LabelShort}, asReceiver={asReceiver}, " +
				$"receiver={receiver.LabelShort}, bed={(FindBed(inviter, receiver) != null)}");
		}

		/// <summary>由地图组件每 tick 调用：确认被邀请者是否已经登记进这一场。</summary>
		public static void TickVerification()
		{
			if (pendingInvites.Count == 0) return;

			int now = Find.TickManager.TicksGame;

			for (int i = pendingInvites.Count - 1; i >= 0; i--)
			{
				PendingInvite pending = pendingInvites[i];

				if (pending.invitee == null || pending.invitee.Dead || pending.receiver == null)
				{
					pendingInvites.RemoveAt(i);
					continue;
				}

				bool joined = pending.asReceiver
					? pending.inviter?.jobs?.curDriver is JobDriver_SexBaseInitiator initiator
						&& initiator.extraReceivers != null
						&& initiator.extraReceivers.Contains(pending.invitee)
					: pending.receiver.jobs?.curDriver is JobDriver_SexBaseReciever receiverDriver
						&& receiverDriver.parteners.Contains(pending.invitee);

				if (joined)
				{
					ModLog.Message($"[InviteJoin] VERIFIED invitee={pending.invitee.LabelShort}, asReceiver={pending.asReceiver}");
					Messages.Message("RJW_InviteSuccess".Translate(pending.invitee), MessageTypeDefOf.NeutralEvent, false);
					pendingInvites.RemoveAt(i);
					continue;
				}

				if (now >= pending.expireTick)
				{
					Pawn invitee = pending.invitee;
					string jobDef = invitee.jobs?.curJob?.def?.defName ?? "none";
					string driver = invitee.jobs?.curDriver?.GetType().Name ?? "none";
					bool reachable = invitee.CanReach(pending.receiver, PathEndMode.OnCell, Danger.Deadly);

					ModLog.Message(
						$"[InviteJoin] FAILED invitee={invitee.LabelShort}, asReceiver={pending.asReceiver}, " +
						$"job={jobDef}, driver={driver}, reachable={reachable}, downed={invitee.Downed}, drafted={invitee.Drafted}");

					string reasonKey = FailureReasonKey(invitee, pending.receiver, reachable, jobDef, driver);
					Messages.Message(
						"RJW_InviteFailed".Translate(invitee) + " " + reasonKey.Translate(),
						MessageTypeDefOf.NegativeEvent,
						false);

					pendingInvites.RemoveAt(i);
				}
			}
		}

		/// <summary>把失败原因归成一句玩家能看懂的话。</summary>
		private static string FailureReasonKey(Pawn invitee, Pawn receiver, bool reachable, string jobDef, string driver)
		{
			if (invitee == null || receiver == null) return "RJW_InviteFailedOther";
			if (!reachable) return "RJW_InviteFailedReach";

			if (jobDef == "Goto" || jobDef == "GotoWander" || driver.Contains("Goto"))
				return "RJW_InviteFailedMoving";

			if (invitee.Downed || invitee.Drafted) return "RJW_InviteFailedOther";

			return "RJW_InviteFailedOther";
		}

		/// <summary>让被邀请者去加入当前这场性行为；必要时先把原版亲热转换成 RJW 驱动。</summary>
		private static void Invite(Pawn inviter, Pawn invitee, Pawn receiver)
		{
			if (invitee?.jobs == null || receiver?.jobs == null) return;

			Building_Bed bed = FindBed(inviter, receiver);
			if (bed == null) return;

			// 原版 JobDriver_Lovin 没有 parteners 列表：先结束它，改用 RJW 的加入流程。
			// 注意只在原版亲热时转换；如果选中的是接受者（双飞），不能结束它自己的接收者 job。
			if (inviter != null && inviter.jobs?.curDriver is JobDriver_Lovin)
			{
				inviter.jobs.EndCurrentJob(JobCondition.InterruptForced);
				inviter.jobs.TryTakeOrderedJob(JobMaker.MakeJob(xxx.casual_sex, receiver, bed), JobTag.Misc);
			}

			// 双飞：男发起、女接受时，把来人做成这一场的「额外接受者」，而不是又一个发起者。
			if (WouldJoinAsReceiver(inviter, invitee, receiver)
				&& inviter.jobs?.curDriver is JobDriver_SexBaseInitiator initiator)
			{
				Job receiverJob = JobMaker.MakeJob(xxx.gettin_loved, inviter, bed);
				invitee.jobs.TryTakeOrderedJob(receiverJob, JobTag.Misc);
				initiator.AddExtraReceiver(invitee);
				TrackInvite(inviter, invitee, receiver, asReceiver: true);
				return;
			}

			Job job = JobMaker.MakeJob(xxx.casual_sex, receiver, bed);
			invitee.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			TrackInvite(inviter, invitee, receiver, asReceiver: false);
		}
	}
}
