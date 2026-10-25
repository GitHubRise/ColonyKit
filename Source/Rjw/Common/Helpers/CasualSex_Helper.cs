#nullable disable

using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Multiplayer.API;
using System;

using Seeded = rjw.Modules.Rand.Seeded;
using rjw.Modules.Attraction;
using rjw.Modules.Shared.Extensions;

namespace rjw
{
	/// <summary>
	/// 与（类人）性交的辅助类
	/// </summary>
	public class CasualSex_Helper
	{
		public static readonly HashSet<string> quickieAllowedJobs = new(DefDatabase<StringListDef>.GetNamed("QuickieInterruptibleJobs").strings);
		/// <summary>
		/// 寻找伴侣时用于 RNG 的盐值。
		/// </summary>
		const int PartnerRNGSalt = -787835064;

		/// <summary>
		/// 寻找地点时用于 RNG 的盐值。
		/// </summary>
		const int LocationRNGSalt = -2112521797;

		/// <summary>
		/// 搜索随机性交地点的最大距离。
		/// </summary>
		const int MaxLocationDistance = 40;

		public enum QuickieCloseness { Close, InRange, OutOfRange }

		public static bool CanHaveSex(Pawn target)
		{
			return xxx.can_fuck(target) || xxx.can_be_fucked(target);
		}

		/// <summary>
		/// 判断观察者与目标的粗略距离。
		/// </summary>
		/// <param name="observer">观察者。</param>
		/// <param name="target">目标。</param>
		/// <returns>一个表示二者接近程度的模糊值。</returns>
		public static QuickieCloseness GetCloseness(Pawn observer, Pawn target)
		{
			float distSq = observer.Position.DistanceToSquared(target.Position);
			return distSq switch
			{
				<= 16f => // 4 * 4
					QuickieCloseness.Close,
				var dist when dist > RJWSettings.maxDistanceCellsCasual * RJWSettings.maxDistanceCellsCasual =>
					QuickieCloseness.OutOfRange,
				_ =>
					QuickieCloseness.InRange
			};
		}

		/// <summary>
		/// 找出该小人所在地图上所有可能成为其合适约炮对象
		/// 的小人。
		/// </summary>
		/// <param name="pawn">正在寻找约炮对象的小人。</param>
		/// <param name="bedsex">是否限制为床上的目标。</param>
		/// <returns>潜在约炮对象的枚举。</returns>
		public static IEnumerable<Pawn> PotentialPartnersFor(Pawn pawn, bool bedsex = false)
		{
			foreach (var partner in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				// 动物由兽交工作处理。
				if (partner.IsAnimal()) continue;
				// 可能是机器人或其他模组小人。
				if (partner.relations is null) continue;

				// 基本资格检查。
				if (partner == pawn) continue;
				if (GetCloseness(pawn, partner) is QuickieCloseness.OutOfRange) continue;
				if (partner.IsForbidden(pawn)) continue;
				if (!xxx.IsTargetPawnOkay(partner)) continue;
				if (!CanHaveSex(partner)) continue;
				if (partner.HostileTo(pawn)) continue;

				// 双方必须能够互相预留。
				// 群交：允许最多 max_sex_partners 个发起者同时预约同一个目标。
				int maxPartners = xxx.max_sex_partners_for(pawn, partner);
				if (!pawn.CanReserveAndReach(partner, PathEndMode.OnCell, Danger.Some, maxPartners, 0)) continue;
				if (!partner.CanReserve(pawn, 1, 0)) continue;

				// 验证伴侣是否符合对床的要求。
				switch (bedsex)
				{
					// 群交：目标是床上进行中的自愿性行为，且还有空位时允许加入。
					case true when CanJoinOngoingBedSex(partner, pawn): break;
					case true when Bed_Utility.is_laying_down_alone(partner): break;
					case true when Bed_Utility.in_same_bed(partner, pawn): break;
					case false when !partner.InBed(): break;
					default: continue;
				}

				// 确保他们没在做我们不该打断的事。
				if (!bedsex && partner.jobs?.curJob is Job curJob)
					if (curJob.playerForced || !quickieAllowedJobs.Contains(curJob.def.defName))
						continue;
				
				// 他们现在必须确实被允许约炮。
				if (!CanTargetHookup(pawn, partner)) continue;

				yield return partner;
			}
		}

		[SyncMethod]
		public static Pawn FindPartner(Pawn pawn, bool bedsex = false)
		{
			string pawnName = xxx.get_pawnname(pawn);
			if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): starting.");

			bool pawnIsNympho = xxx.is_nympho(pawn);
			bool pawnCanPickAnyone = RJWSettings.WildMode;// || (pawnIsNympho && RJWHookupSettings.NymphosCanPickAnyone);

			if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): nympho:{pawnIsNympho}, ignores rules:{pawnCanPickAnyone}");

			if (!RJWHookupSettings.ColonistsCanHookup && pawn.IsFreeColonist && !pawnCanPickAnyone)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): is a colonist and colonist hookups are disabled in mod settings");
				return null;
			}

			var targets = PotentialPartnersFor(pawn, bedsex).ToList();

			if (targets.Count <= 0)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): no eligible targets");
				return null;
			}

			if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): considering {targets.Count} targets");

			// 这会为整个函数设置随机种子。
			using var _rng = Seeded.ForHour(pawn, PartnerRNGSalt);

			// HippieMode 意味着：允许所有人彼此发生性关系（“自由恋爱”）
			//	- 不区分伴侣优先级
			//	- 不必处于性奋状态
			//	- 从不视为出轨
			// -> 直接选一个伴侣

			if (!RJWSettings.HippieMode)
			{
				// 优先选择已有的恋人（如果存在）。
				var splitPartners = targets.ToLookup(target => pawn.GetRelations(target).Any(LovePartnerRelationUtility.IsLovePartnerRelation));
				var lovers = splitPartners[true].ToList();
				targets = splitPartners[false].ToList();

				if (lovers.Count == 0)
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): no eligible love partners");
					// 可能当前根本没有符合条件的恋人。我们仍需要检查
					// 他们能否约炮，以及这是否算出轨。
					goto checkHookup;
				}

				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): considering {lovers.Count} partners first");

				// 对观察者和候选者使用默认的 `ForFucking`。
				var appraisalParams = new AppraisalParams(pawn, lovers);

				// 使用 `FindPartners` 会排除任何低于平均水平的恋人。
				// 我们将运行自定义搜索，仍然偏向更有吸引力的
				// 选项，但也可以改为选择任何可能的伴侣。
				var results = SexAppraiser.RNG.FindResults(SexAppraiser.CandidatesAsTargets(in appraisalParams));

				// 如果这里不为空，将使用第一个选项。
				foreach (var result in results)
				{
					// 伴侣必须是实际可达的。
					// if (!Pather_Utility.can_path_to_target(pawn, result.Target)) continue;

					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): banging lover {xxx.get_pawnname(result.Target)}");
					return result.Target;
				}

			checkHookup:
				// 没有可用的恋人……看看小人是否愿意约炮。花痴和欲求不满的小人总是愿意！
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): no lovers in targets; checking CanPawnHookup");
				if (!RNG.CanPawnHookup(pawn, pawnIsNympho))
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): no hookup today");
					return null;
				}

				// 随性约炮不算出轨……否则殖民地关系管理大概会太烦人
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): checking CanFoolAround");
				
				// 检查任何约炮是否构成出轨。
				if (!AttractionUtility.CanFoolAround(pawn, true))
				{
					if (RJWHookupSettings.NymphosCanCheat && pawnIsNympho && xxx.is_frustrated(pawn))
					{
						if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): I'm a nympho and I'm so frustrated that I'm going to cheat");
						// 这里不返回，以便他们继续寻找约炮对象
					}
					else if (pawn.HasBeerGoggles())
					{
						if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): I want to bang and im too drunk to care if its cheating");
					}
					else
					{
						if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): I want to bang but that's cheating");
						return null;
					}
				}
			}

			// 使用专门的评估函数来寻找伴侣。
			foreach (AppraisalResult result in RNG.FindQuickiePartner(pawn, targets))
			{
				// 确保强制执行配置的最小值。
				if (!CasualHookupAllowedViaSettings(result)) continue;
				// 我们尽可能推迟，但最后还是要做寻路检查。
				if (!Pather_Utility.can_path_to_target(pawn, result.Target)) continue;

				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): found rando {xxx.get_pawnname(result.Target)} with score {result.CombinedAttraction} ({result.ObserverAttraction} + {result.TargetAttraction})");
				return result.Target;
			}

			if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindPartner({pawnName}): couldn't find anyone to bang");
			return null;
		}

		public static bool CasualHookupAllowedViaSettings(AppraisalResult result)
		{
			GetCasualHookupSettingsForPawn(result.Observer, out float observerMinFuckability, out int observerMinRelationship, out float observerMinAttractiveness);
			GetCasualHookupSettingsForPawn(result.Target, out float targetMinFuckability, out int targetMinRelationship, out float targetMinAttractiveness);

			if (result.ObserverAttraction < observerMinFuckability)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CasualHookupAllowedViaSettings({result.Observer}): hookup {xxx.get_pawnname(result.Target)}, ObserverAttraction too low:({result.ObserverAttraction} < {observerMinFuckability})");
				return false;
			}

			if (result.TargetAttraction < targetMinFuckability)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CasualHookupAllowedViaSettings({result.Observer}): hookup {xxx.get_pawnname(result.Target)}, TargetAttraction too low:({result.TargetAttraction} < {targetMinFuckability})");
				return false;
			}

			if (xxx.is_animal(result.Target))
				return true;

			// 类人检查
			int relations = result.Observer.relations.OpinionOf(result.Target);
			if (relations < observerMinRelationship)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CasualHookupAllowedViaSettings({result.Observer}): hookup {xxx.get_pawnname(result.Target)}, i don't like them:({relations})");
				return false;
			}

			relations = result.Target.relations.OpinionOf(result.Observer);
			if (relations < targetMinRelationship)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CasualHookupAllowedViaSettings({result.Observer}): hookup {xxx.get_pawnname(result.Target)}, don't like me:({relations})");
				return false;
			}

			float attraction = result.Observer.relations.SecondaryRomanceChanceFactor(result.Target);
			if (attraction < observerMinAttractiveness)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CasualHookupAllowedViaSettings({result.Observer}): hookup {xxx.get_pawnname(result.Target)}, i don't find them attractive:({attraction})");
				return false;
			}
			attraction = result.Target.relations.SecondaryRomanceChanceFactor(result.Observer);
			if (attraction < targetMinAttractiveness)
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CasualHookupAllowedViaSettings({result.Observer}): hookup {xxx.get_pawnname(result.Target)}, doesn't find me attractive:({attraction})");
				return false;
			}

			return true;
		}

		// 月经模块对此方法打了补丁
		public static void GetCasualHookupSettingsForPawn(Pawn pawn, out float minimumFuckability, out int minimumRelationship, out float minimumAttractiveness)
		{
			if (xxx.is_nympho(pawn))
			{
				minimumFuckability = 0f;
				minimumRelationship = 0;
				minimumAttractiveness = 0f;
				return;
			}

			minimumFuckability = RJWHookupSettings.MinimumFuckabilityToHookup;
			minimumRelationship = RJWHookupSettings.MinimumRelationshipToHookup;
			minimumAttractiveness = RJWHookupSettings.MinimumAttractivenessToHookup;
		}

		/// <summary>
		/// 判断 <paramref name="target"/> 是否正在床上进行自愿性行为，
		/// 并且按当前设置还能再接纳一名发起者。
		/// </summary>
		public static bool CanJoinOngoingBedSex(Pawn target, Pawn joiner)
		{
			if (target?.jobs?.curDriver is not JobDriver_SexBaseReciever receiver) return false;
			if (receiver.Sexprops == null || receiver.Sexprops.isRape) return false;
			if (!target.InBed()) return false;
			if (!xxx.is_human(target) || !xxx.is_human(joiner)) return false;
			return receiver.parteners.Count < xxx.max_sex_partners_for(joiner, target);
		}

		public static bool CanTargetHookup(Pawn pawn, Pawn target)
		{
			var pawnName = xxx.get_pawnname(pawn);
			var targetName = xxx.get_pawnname(target);

			// 已在 `PotentialPartnersFor` 中检查过，但这是公开函数。
			if (target.relations is not { } tRelations) return false;

			if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CanTargetHookup({pawnName}): checking hookup {targetName}");

			// 检查约炮的模组设置是否允许这种配对
			if (!RJWSettings.WildMode && !HookupAllowedViaSettings(pawn, target)) return false;

			// 群交：对方已经在做，就不必再检查"最近做过爱/是否性奋"。
			if (CanJoinOngoingBedSex(target, pawn)) return true;

			// 如果小人最近做过爱且现在不性奋，就跳过。
			if (!SexUtility.ReadyForLovin(target) && !xxx.is_hornyorfrustrated(target))
			{
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CanTargetHookup({pawnName}): hookup {targetName} isn't ready for lovin'");
				return false;
			}

			if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" CanTargetHookup({pawnName}): hookup {targetName} is sufficiently available");
			return true;
		}

		/// <summary> 检查模组设置是否允许这两个小人约炮。 </summary>
		public static bool HookupAllowedViaSettings(Pawn pawn, Pawn targetPawn)
		{
			// 囚犯能约炮吗？
			if (pawn.IsPrisonerOfColony || pawn.IsPrisoner || xxx.is_slave(pawn))
			{
				if (!RJWHookupSettings.PrisonersCanHookupWithNonPrisoner && !(targetPawn.IsPrisonerOfColony || targetPawn.IsPrisoner || xxx.is_slave(targetPawn)))
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" find_partner({xxx.get_pawnname(pawn)}): not hooking up with {xxx.get_pawnname(targetPawn)} due to mod setting PrisonersCanHookupWithNonPrisoner");
					return false;
				}

				if (!RJWHookupSettings.PrisonersCanHookupWithPrisoner && (targetPawn.IsPrisonerOfColony || targetPawn.IsPrisoner || xxx.is_slave(targetPawn)))
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" find_partner({xxx.get_pawnname(pawn)}): not hooking up with {xxx.get_pawnname(targetPawn)} due to mod setting PrisonersCanHookupWithPrisoner");
					return false;
				}
			}
			else
			{
				// 非囚犯能和囚犯约炮吗？
				if (!RJWHookupSettings.CanHookupWithPrisoner && (targetPawn.IsPrisonerOfColony || targetPawn.IsPrisoner || xxx.is_slave(targetPawn)))
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" find_partner({xxx.get_pawnname(pawn)}): not hooking up with {xxx.get_pawnname(targetPawn)} due to mod setting CanHookupWithPrisoner");
					return false;
				}
			}

			// 奴隶能约炮吗？
			//if (xxx.is_slave(pawn))
			//{
			//	if (!RJWHookupSettings.SlaveCanHookup)
			//	{
			//		if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" find_partner({xxx.get_pawnname(pawn)}): not hooking up with {xxx.get_pawnname(targetPawn)} due to mod setting SlaveCanHookup");
			//		return false;
			//	}
			//}

			// 殖民者能和访客约炮吗？
			if (pawn.IsFreeColonist && !xxx.is_slave(pawn))
			{
				if (!RJWHookupSettings.ColonistsCanHookupWithVisitor && targetPawn.Faction != Faction.OfPlayer && !targetPawn.IsPrisonerOfColony)
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" find_partner({xxx.get_pawnname(pawn)}): not hooking up with {xxx.get_pawnname(targetPawn)} due to mod setting ColonistsCanHookupWithVisitor");
					return false;
				}
			}

			// 访客能约炮吗？
			if (pawn.Faction != Faction.OfPlayer && !pawn.IsPrisonerOfColony)
			{
				// 访客对殖民者
				if (!RJWHookupSettings.VisitorsCanHookupWithColonists && targetPawn.IsColonist)
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" find_partner({xxx.get_pawnname(pawn)}): not hooking up with {xxx.get_pawnname(targetPawn)} due to mod setting VisitorsCanHookupWithColonists");
					return false;
				}

				// 访客对访客
				if (!RJWHookupSettings.VisitorsCanHookupWithVisitors && targetPawn.Faction != Faction.OfPlayer)
				{
					if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" find_partner({xxx.get_pawnname(pawn)}): not hooking up with {xxx.get_pawnname(targetPawn)} due to mod setting VisitorsCanHookupWithVisitors");
					return false;
				}
			}

			// TODO：不确定这是否处理了所有小人对动物的情况。

			return true;
		}

#nullable enable

		[SyncMethod]
		public static IntVec3 FindSexLocation(Pawn pawn, Pawn? partner = null)
		{
			IntVec3 position = pawn.Position;
			int bestPosition = -100;
			var pawnMap = pawn.Map;
			var pawnRegion = pawn.GetRegion();
			IntVec3 cell = pawn.Position;

			// List<Pawn> all_pawns = pawnMap.mapPawns.AllPawnsSpawned.Where(x
			// 	=> xxx.is_human(x)
			// 	&& x.Position.DistanceToSquared(position) < 10000
			// 	&& x != pawn
			// 	&& x != partner
			// 	).ToList();
			int distSquared = 10000;
			HashSet<int> tmpInvIds = [pawn.thingIDNumber];
			List<Pawn> all_pawns = new();
			if (pawnRegion != null)
			{
				RegionTraverser.BreadthFirstTraverse(pawnRegion, (Region from, Region to) => (to.extentsClose.ClosestDistSquaredTo(pawn.Position) <= distSquared), delegate (Region reg)
				{
						List<Thing> list = reg.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
						for (int i = 0; i < list.Count; i++)
						{
								if (!tmpInvIds.Contains(list[i].thingIDNumber)
										&& list[i] is Pawn otherPawn
										&& xxx.is_human(otherPawn)
										&& otherPawn.Position.DistanceToSquared(pawn.Position) < distSquared
								)
								{
										all_pawns.Add(otherPawn);
								}
								tmpInvIds.Add(list[i].thingIDNumber);
						}
						return false;
				}, 99999);
			}

			//ModLog.Message(" Pawn is " + xxx.get_pawnname(pawn) + ", current cell is " + cell);

			HashSet<IntVec3> randomCells = new();
			int iterationCounter = 0;
			using (Seeded.ForHour(pawn, LocationRNGSalt))
			{
				foreach (var c in RNG.RandomCellsAround(position))
				{
					iterationCounter++;
					if (iterationCounter < 50)
					{
						if (!IsAcceptableSexSpot(c, pawn, partner))
							continue;
						randomCells.Add(c);
						if (randomCells.Count >= 25)
							break;
					}
					else break;
				}
			}

			//ModLog.Message(" Found " + random_cells.Count + " valid cells.");
			var randomCellsRoomsDoors = new Dictionary<int, int>();
			foreach (var c in randomCells)
			{
					var room = c.GetRoom(pawnMap);
					if (room == null || room.PsychologicallyOutdoors || randomCellsRoomsDoors.ContainsKey(room.ID))
							continue;
					int count = 0;
					var things = room.ContainedAndAdjacentThings;
					for (int i = 0; i < things.Count; i++)
					{
							var def = things[i].def;
							if (def.IsDoor && def.holdsRoof && def.blockLight)
									count++;
					}
					randomCellsRoomsDoors[room.ID] = count;
			}

			foreach (IntVec3 randomCell in randomCells)
			{
				if (!Pather_Utility.cells_to_target_casual(pawn, randomCell))
					continue;// 太远了

				int doors = randomCellsRoomsDoors.TryGetValue(randomCell.GetRoom(pawnMap).ID);
				int score = ScoreSexSpot(all_pawns, doors, randomCell, pawn, partner);

				if (score <= bestPosition) continue;

				bestPosition = score;
				cell = randomCell;
			}

			return cell;

			//ModLog.Message(" Best cell is " + cell);
		}

		/// <summary>
		/// 快速检查某个位置能否用于性交。
		/// </summary>
		/// <returns>如果参与者在任何情况下都不应使用该位置，则为 false</returns>
		public static bool IsAcceptableSexSpot(IntVec3 spot, Pawn pawn, Pawn? partner)
		{
			if (spot.Fogged(pawn.Map))
			{
				return false;
			}

			if (!StandableForLewd(spot, pawn.Map))
			{
				return false;
			}

			if (!spot.InAllowedArea(pawn))
			{
				return false;
			}

			if (spot.ContainsTrap(pawn.Map))
			{
				return false;
			}

			if (spot.ContainsStaticFire(pawn.Map))
			{
				return false;
			}

			if (spot.VacuumConcernTo(pawn))
			{
				return false;
			}

			if (!pawn.CanReach(spot, PathEndMode.OnCell, Danger.Some))
			{
				return false;
			}

			// 伴侣检查
			if (partner != null && partner != pawn)
			{
				if (spot.VacuumConcernTo(partner))
				{
					return false;
				}
			}

			return true;
		}

		public static bool StandableForLewd(IntVec3 c, Map map)
		{
			if (!c.Walkable(map))
			{
				return false;
			}

			List<Thing> list = map.thingGrid.ThingsListAt(c);
			for (int i = 0; i < list.Count; i++)
			{
				var thing = list[i];
				var thingDef = thing.def;
				if (thing is Building_Bed || thingDef.IsBed)
				{
					continue;
				}

				if (thingDef.passability != 0)
				{
					return false;
				}
			}

			return true;
		}

#nullable disable

		/// <summary>
		/// 获取小人对在特定单元格内性交这一想法的喜欢程度。
		/// </summary>
		/// <param name="otherPawns">需要考虑的附近其他小人</param>
		/// <param name="doorCount">如果单元格在房间内，该房间有多少扇门</param>
		/// <param name="cell">地点坐标</param>
		/// <param name="pawn">选择地点的小人</param>
		/// <param name="partner">伴侣，自慰时可能为 null</param>
		/// <param name="isExhibitionist">小人是否为暴露狂。目前由子模组设置</param>
		/// <returns>相对评分。其本身没有意义</returns>
		public static int ScoreSexSpot(List<Pawn> otherPawns, int doorCount, IntVec3 cell, Pawn pawn, Pawn partner = null, bool isExhibitionist = false)
		{
			int score = 0;
			var pawnMap = pawn.Map;
			Room room = cell.GetRoom(pawnMap);

			bool might_be_seen = MightBeSeen(otherPawns, cell, pawn, partner);

			if (isExhibitionist)
			{
				if (might_be_seen)
					score += 5;
				else
					score -= 10;
			}
			else
			{
				if (might_be_seen)
					score -= 30;
			}

			FloatRange temperature = pawn.ComfortableTemperatureRange();

			if (temperature.Includes(cell.GetTemperature(pawnMap)))
				score += 20;
			else
				score -= 20;

			if (cell.Roofed(pawnMap))
				score += 5;

			if (cell.HasEatSurface(pawnMap))
				score += 5; // 躲在植被中。

			Danger danger = cell.GetDangerFor(pawn, pawnMap);

			if (danger == Danger.Some)
				score -= 25;
			else if (danger == Danger.None)
				score += 5;

			TerrainDef terrain = cell.GetTerrain(pawnMap);

			// TODO：检查是否改用 terrain.IsWater
			if (terrain == TerrainDefOf.WaterShallow ||
				terrain == TerrainDefOf.WaterMovingShallow ||
				terrain == TerrainDefOf.WaterOceanShallow)
				score -= 20;
			var cellThings = cell.GetThingList(pawnMap);
			bool corpseFound = false;
			bool foodFound = false;
			bool bedFound = false;
			for (int i = 0; i < cellThings.Count; i++)
			{
				var cellThing = cellThings[i];
				if (!bedFound && cellThing is Building_Bed)
				{
					bedFound = true;
					score += 5;
				}

				else if (!corpseFound && cellThing.def.IsWithinCategory(ThingCategoryDefOf.Corpses))
				{
					corpseFound = true;
					if (xxx.is_necrophiliac(pawn))
						score += 20;
					else
						score -= 20;
				}

				else if (!foodFound && cellThing.def.IsWithinCategory(ThingCategoryDefOf.Foods))
				{
					foodFound = true;
					score -= 10;
				}
			}

			if (room == pawn.Position.GetRoom(pawn.MapHeld))
				score -= 10;

			if (room.PsychologicallyOutdoors)
				score += 5;

			if (room.IsHuge)
				score -= 5;

			if (!room.IsHuge)
			{
				List<Region> regions = room.Regions;
				bool shouldBreak = false;
				for (int i = 0; i < regions.Count; i++)
				{
					List<Thing> allThings = regions[i].ListerThings.AllThings;
					if (allThings == null)
					{
						continue;
					}
					for (int j = 0; j < allThings.Count; j++)
					{
						Thing atThing = allThings[j];
						if (atThing is Building_Bed building_Bed)
						{
							score += 5;
							shouldBreak = true;
							break;
						}
					}
					if (shouldBreak) break;
				}
				if (!room.Owners.Any())
					score += 10;
				else if (room.Owners.Contains(pawn))
					score += 20;
			}

			if (room.IsDoorway && !isExhibitionist)
				score -= 100;

			if (room.Role == RoomRoleDefOf.Bedroom)
				score += 10;

			else if (room.Role == RoomRoleDefOf.Barracks || room.Role == RoomRoleDefOf.PrisonBarracks || room.Role == RoomRoleDefOf.PrisonCell
				|| room.Role == VanillaRoomRoleDefOf.Laboratory || room.Role == VanillaRoomRoleDefOf.RecRoom
				|| room.Role == VanillaRoomRoleDefOf.DiningRoom || room.Role == RoomRoleDefOf.Hospital
				)
				if (isExhibitionist)
					score += 10;
				else
					score -= 5;

			if (room.GetStat(RoomStatDefOf.Cleanliness) < 0.01f)
				score -= 5;

			if (room.GetStat(RoomStatDefOf.GraveVisitingJoyGainFactor) > 0.1f)
				if (xxx.is_necrophiliac(pawn))
				{
					score += 5;
				}
				else
				{
					score -= 5;
				}

			if (doorCount > 1)
				if (isExhibitionist)
				{
					score += 2 * doorCount;
				}
				else
				{
					score -= 5 * doorCount;
				}
			
			return score;
		}

		public static bool MightBeSeen(List<Pawn> otherPawns, IntVec3 cell, Pawn pawn, Pawn partner = null)
		{
			return otherPawns.Any(x
					=> x != partner
					&& x.Awake()
					&& x.Position.DistanceToSquared(cell) < 2500
					&& GenSight.LineOfSight(x.Position, cell, pawn.Map)
					);
		}

		/// <summary>
		/// 这些方法使用 RNG。你应当使用确定性的种子
		/// （使用 <see cref="Seeded" />），或对响应玩家操作的
		/// 方法使用多人 API 的 `SyncMethod` 特性。
		/// </summary>
		public static class RNG
		{
			public static bool CanPawnHookup(Pawn pawn, bool pawnIsNympho)
			{
				if (pawnIsNympho) return true;
				if (RJWSettings.WildMode) return true;
				if (xxx.is_frustrated(pawn)) return true;
				if (!xxx.is_horny(pawn)) return false;

				return Rand.Value < RJWHookupSettings.HookupChanceForNonNymphos;
			}

			public static IEnumerable<AppraisalResult> FindQuickiePartner(
				Pawn observer,
				IEnumerable<Pawn> candidates)
			{
				// 分为近处和远处目标。
				var splitTargets = candidates.ToLookup((t) => GetCloseness(observer, t));

				// 快速路径：先尝试近处目标，直接采用最好的那个。
				var nearParams = new AppraisalParams(observer, splitTargets[QuickieCloseness.Close]);
				var nearResults = SexAppraiser.CandidatesAsTargets(in nearParams);
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindQuickiePartner({xxx.get_pawnname(observer)}): checking {nearResults.Count} close results");
				foreach (var result in SexAppraiser.FindBestResults(nearResults))
					yield return result;

				// 然后再处理更远的目标。
				var farParams = new AppraisalParams(observer, splitTargets[QuickieCloseness.InRange]);
				var farResults = SexAppraiser.CandidatesAsTargets(in farParams);
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindQuickiePartner({xxx.get_pawnname(observer)}): checking {farResults.Count} far results");
				foreach (var result in SexAppraiser.RNG.FindBiasedResults(farResults))
					yield return result;
			}

			/// <summary>
			/// <para>无限产生给定位置周围的随机地点。</para>
			/// <para>此可枚举对象不会终止；请配合 `Take` 或其他
			/// 方法来限制它生成的结果数量。</para>
			/// </summary>
			/// <param name="position">位置。</param>
			/// <returns>无限的位置可枚举对象。</returns>
			public static IEnumerable<IntVec3> RandomCellsAround(IntVec3 position)
			{
				while (true)
				{
					float angle = Rand.Range(0f, 360f);
					int dist = Rand.RangeInclusive(1, MaxLocationDistance);
					var offset = IntVec3.FromPolar(angle, dist);

					yield return position + offset;
				}
			}
		}
	}
}
