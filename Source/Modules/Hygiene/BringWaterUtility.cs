using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「送水」的公共判断：给走不到水源的病人 / 囚徒把水送到手上。
	/// 优先送 1 L 瓶装水（凉白开 &gt; 纯净水 &gt; 矿泉水），收到水的人渴了自己喝；
	/// 没有瓶装水时退回「拿水盆/水桶去水源打水、当场喂」的 <c>CK_WaterPatient</c>。
	/// </summary>
	internal static class BringWaterUtility
	{
		/// <summary>口渴低于这个值才值得派一趟送水（0.55 大约剩一半多一点）。</summary>
		public const float ThirstToDeliver = 0.55f;

		/// <summary>一次最多送几瓶，避免为了一口水把整垛瓶装水搬空。</summary>
		public const int MaxBottlesPerTrip = 5;

		/// <summary>能不能自己端着水喝：倒地、卧床需要别人喂的不算。</summary>
		public static bool CanDrinkOnOwn(Pawn pawn)
		{
			return pawn != null && !pawn.Downed && !FoodUtility.ShouldBeFedBySomeone(pawn);
		}

		/// <summary>
		/// 这个小人是不是殖民地该照顾的对象：殖民者、奴隶、囚徒、访客。
		/// 敌对派系和野生动物不算，动物喝水由水槽负责。
		/// </summary>
		public static bool IsColonyCaredFor(Pawn pawn)
		{
			if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.RaceProps.Animal)
			{
				return false;
			}
			if (pawn.IsPrisonerOfColony)
			{
				return pawn.guest == null || pawn.guest.CanBeBroughtFood;
			}
			if (pawn.HostFaction == Faction.OfPlayer)
			{
				return true;
			}
			return pawn.Faction == Faction.OfPlayer;
		}

		/// <summary>身上已经带着能喝的水（瓶装水或装了水的容器），不用再送。</summary>
		public static bool HasDrinkOnHand(Pawn pawn)
		{
			if (pawn?.needs == null)
			{
				return false;
			}
			if (MineralWaterUtility.Carried(pawn) != null)
			{
				return true;
			}
			return WaterContainerUtility.CarriedContainer(pawn, requireWater: true) != null;
		}

		/// <summary>小人自己走得到水吗：能走到洗手池/取水点/马桶水箱/地表水就算能。</summary>
		public static bool CanReachDrinkSource(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return false;
			}
			return ClosestSanitation.FindBestDrink(pawn, urgent: false, 40f).IsValid;
		}

		/// <summary>便宜的筛选：口渴、殖民地要管、身上没水。不含找水分析，供扫描列表用。</summary>
		public static bool IsCandidate(Pawn pawn)
		{
			if (!IsColonyCaredFor(pawn))
			{
				return false;
			}
			Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null || thirst.CurLevel >= ThirstToDeliver)
			{
				return false;
			}
			if (HasDrinkOnHand(pawn))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// 需不需要派人送水：口渴、身上没水，而且要么自己动不了（倒地/卧床），
		/// 要么压根够不着任何水源（关在没有水管的牢房里就是这种）。
		/// </summary>
		public static bool ShouldDeliverWater(Pawn pawn)
		{
			if (!IsCandidate(pawn))
			{
				return false;
			}
			if (!CanDrinkOnOwn(pawn))
			{
				return true;
			}
			return !CanReachDrinkSource(pawn);
		}

		/// <summary>给 target 造一个送水 Job：瓶装水优先，没有瓶装水就退回拿容器喂水。</summary>
		public static Job TryMakeJob(Pawn caretaker, Pawn target)
		{
			if (caretaker?.Map == null || target?.Map == null || caretaker == target)
			{
				return null;
			}
			if (caretaker.Map != target.Map)
			{
				return null;
			}
			Job bottleJob = TryMakeBottleJob(caretaker, target);
			if (bottleJob != null)
			{
				return bottleJob;
			}
			return WorkGiver_WaterIncapacitated.TryMakeFeedJob(caretaker, target);
		}

		private static Job TryMakeBottleJob(Pawn caretaker, Pawn target)
		{
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_BringWater");
			if (def == null)
			{
				return null;
			}
			Thing stack = FindBottleForDelivery(caretaker);
			if (stack == null || stack.stackCount <= 0)
			{
				return null;
			}
			Need_Thirst thirst = target.needs?.TryGetNeed<Need_Thirst>();
			int count = DesiredBottleCount(target, stack, thirst);
			if (count <= 0)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(def, target, stack);
			job.count = Mathf.Min(count, stack.stackCount);
			return job;
		}

		/// <summary>
		/// 送几瓶够喝：按口渴差除以每瓶解渴量算，最多 <see cref="MaxBottlesPerTrip"/> 瓶；
		/// 自己动不了的（只能当场喂一瓶）就只拿一瓶。
		/// </summary>
		private static int DesiredBottleCount(Pawn target, Thing stack, Need_Thirst thirst)
		{
			float perBottle = Mathf.Max(0.05f, MineralWaterUtility.ThirstFor(stack));
			float missing = 1f - (thirst?.CurLevel ?? 0f);
			int byNeed = Mathf.CeilToInt(missing / perBottle);
			int cap = CanDrinkOnOwn(target) ? MaxBottlesPerTrip : 1;
			return Mathf.Clamp(byNeed, 1, cap);
		}

		/// <summary>找一叠能送的瓶装水：先看自己身上，再按 凉白开 → 纯净水 → 矿泉水 找地上最近的。</summary>
		public static Thing FindBottleForDelivery(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return null;
			}
			Thing carried = FindCarriedBottle(pawn);
			if (carried != null)
			{
				return carried;
			}
			return FindNearestSpawnedBottle(pawn, MineralWaterUtility.BoiledDef)
				?? FindNearestSpawnedBottle(pawn, MineralWaterUtility.PurifiedDef)
				?? FindNearestSpawnedBottle(pawn, MineralWaterUtility.Def);
		}

		/// <summary>物品栏里最好的一瓶（凉白开 &gt; 纯净水 &gt; 矿泉水）。</summary>
		public static Thing FindCarriedBottle(Pawn pawn)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			Thing best = FindInInventory(pawn, MineralWaterUtility.BoiledDef)
				?? FindInInventory(pawn, MineralWaterUtility.PurifiedDef)
				?? FindInInventory(pawn, MineralWaterUtility.Def);
			return best;
		}

		private static Thing FindInInventory(Pawn pawn, ThingDef def)
		{
			if (def == null || pawn.inventory?.innerContainer == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				if (thing.def == def && thing.stackCount > 0)
				{
					return thing;
				}
			}
			return null;
		}

		private static Thing FindNearestSpawnedBottle(Pawn pawn, ThingDef def)
		{
			if (def == null)
			{
				return null;
			}
			Thing best = null;
			float bestDistance = float.MaxValue;
			foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(def))
			{
				if (thing.stackCount <= 0 || thing.IsForbidden(pawn) || thing.IsBurning())
				{
					continue;
				}
				float distance = thing.Position.DistanceTo(pawn.Position);
				if (distance >= bestDistance)
				{
					continue;
				}
				if (!pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
				{
					continue;
				}
				bestDistance = distance;
				best = thing;
			}
			return best;
		}

		/// <summary>把送水人身上最多 count 瓶转给 target；target 拿不下就放在脚边。</summary>
		public static void HandOverBottles(Pawn carrier, Pawn target, int count)
		{
			if (carrier?.inventory?.innerContainer == null || target == null)
			{
				return;
			}
			int remaining = Mathf.Max(1, count);
			for (int guard = 0; guard < 32 && remaining > 0; guard++)
			{
				Thing stack = MineralWaterUtility.Carried(carrier);
				if (stack == null)
				{
					return;
				}
				int take = Mathf.Min(remaining, stack.stackCount);
				Thing part = TakeOutOfInventory(carrier, stack, take);
				if (part == null)
				{
					return;
				}
				GiveTo(target, part);
				remaining -= take;
			}
		}

		/// <summary>病人/囚徒自己端不起来时，医生开一瓶直接喂掉。</summary>
		public static bool FeedBottle(Pawn carrier, Pawn target)
		{
			if (carrier?.inventory?.innerContainer == null || target == null || target.Dead)
			{
				return false;
			}
			Thing stack = MineralWaterUtility.Carried(carrier);
			if (stack == null)
			{
				return false;
			}
			Thing bottle = TakeOutOfInventory(carrier, stack, 1);
			if (bottle == null)
			{
				return false;
			}
			// Ingested 会走原版消化结算 + 本项目的水质/饱腹/心情后缀（Patch_Thing_Ingested_MineralWater）。
			bottle.Ingested(target, 0f);
			if (!bottle.Destroyed)
			{
				bottle.Destroy(DestroyMode.Vanish);
			}
			return true;
		}

		/// <summary>从容器里拿出最多 count 件（整叠拿走时从容器里摘掉），返回脱离容器的部分。</summary>
		private static Thing TakeOutOfInventory(Pawn owner, Thing stack, int count)
		{
			if (stack == null || stack.stackCount <= 0 || count <= 0)
			{
				return null;
			}
			int take = Mathf.Min(count, stack.stackCount);
			bool whole = take >= stack.stackCount;
			Thing part = stack.SplitOff(take);
			if (part == null)
			{
				return null;
			}
			if (whole)
			{
				owner.inventory.innerContainer.Remove(part);
			}
			if (part.Spawned)
			{
				part.DeSpawn();
			}
			return part;
		}

		/// <summary>把一件东西塞进 target 物品栏；塞不下就放在它脚边。</summary>
		public static bool GiveTo(Pawn target, Thing thing)
		{
			if (target == null || thing == null)
			{
				return false;
			}
			if (target.inventory?.innerContainer != null && target.inventory.innerContainer.TryAdd(thing))
			{
				return true;
			}
			Map map = target.MapHeld;
			if (map == null)
			{
				thing.Destroy(DestroyMode.Vanish);
				return false;
			}
			return GenPlace.TryPlaceThing(thing, target.PositionHeld, map, ThingPlaceMode.Near);
		}
	}
}
