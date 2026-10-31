using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 双持的装备管理：主手 = <see cref="Pawn_EquipmentTracker.Primary"/>，副手 = 装备列表里
	/// 除主手以外的另一件武器。两把武器都放在原版 <see cref="Pawn_EquipmentTracker"/> 里，
	/// 死亡掉落、征召、读档、近战动词挑选全部沿用原版，只有「装备第三把时怎么换」和
	/// 「副手怎么参与战斗」需要补丁。
	/// </summary>
	internal static class MkVLoadout
	{
		/// <summary>Mk-V 最多同时拿两把武器。</summary>
		public const int MaxWeapons = 2;

		/// <summary>主手优先远程武器的评分权重：没有远程武器就没法开火。</summary>
		private const float RangedPrimaryBonus = 100000f;

		public static bool Enabled => NannyBotUtility.Settings.mkvDualWield;

		/// <summary>带电弧发射器专属扩展的武器：换装时永远保留，不会被第三把武器顶掉。</summary>
		public static bool IsExclusive(ThingDef def)
		{
			return def != null && def.GetModExtension<DefModExtension_MkvExclusiveWeapon>() != null;
		}

		public static List<ThingWithComps> HeldWeapons(Pawn pawn)
		{
			List<ThingWithComps> result = new List<ThingWithComps>();
			if (pawn?.equipment == null)
			{
				return result;
			}
			List<ThingWithComps> all = pawn.equipment.AllEquipmentListForReading;
			for (int i = 0; i < all.Count; i++)
			{
				ThingWithComps thing = all[i];
				if (thing?.def != null && thing.def.IsWeapon)
				{
					result.Add(thing);
				}
			}
			return result;
		}

		public static ThingWithComps Primary(Pawn pawn)
		{
			return pawn?.equipment?.Primary;
		}

		/// <summary>副手：装备列表里除主手以外的第一件武器；没有就是空手。</summary>
		public static ThingWithComps Offhand(Pawn pawn)
		{
			if (pawn?.equipment == null)
			{
				return null;
			}
			ThingWithComps primary = pawn.equipment.Primary;
			List<ThingWithComps> all = pawn.equipment.AllEquipmentListForReading;
			for (int i = 0; i < all.Count; i++)
			{
				ThingWithComps thing = all[i];
				if (thing != primary && thing?.def != null && thing.def.IsWeapon)
				{
					return thing;
				}
			}
			return null;
		}

		/// <summary>
		/// 武器评分：远程按射程 + 单发伤害 × 连发数，近战按最锋利工具的 DPS，再叠市场价。
		/// 两类武器的分数只用于「哪把更值得留/换」，不参与实际战斗结算。
		/// </summary>
		public static float WeaponScore(ThingDef def)
		{
			if (def == null)
			{
				return -1f;
			}
			float score = def.BaseMarketValue;
			if (def.IsRangedWeapon)
			{
				float range = 0f;
				int burst = 1;
				if (def.Verbs != null)
				{
					for (int i = 0; i < def.Verbs.Count; i++)
					{
						range = Mathf.Max(range, def.Verbs[i].range);
						burst = Mathf.Max(burst, def.Verbs[i].burstShotCount);
					}
				}
				// 专属加成只在 Mk-V 手上生效，评分时也要算进去，否则会把电弧发射器当成弱枪换掉。
				DefModExtension_MkvExclusiveWeapon exclusive =
					def.GetModExtension<DefModExtension_MkvExclusiveWeapon>();
				if (exclusive != null)
				{
					range += Mathf.Max(0f, exclusive.rangeBonus);
				}
				float damage = def.projectile?.GetDamageAmount(def, null) ?? 0f;
				return score + range * 2f + damage * burst * 12f;
			}
			if (def.IsMeleeWeapon)
			{
				float bestDps = 0f;
				if (def.tools != null)
				{
					for (int i = 0; i < def.tools.Count; i++)
					{
						Tool tool = def.tools[i];
						if (tool == null)
						{
							continue;
						}
						float cooldown = Mathf.Max(0.1f, tool.cooldownTime);
						bestDps = Mathf.Max(bestDps, tool.power / cooldown);
					}
				}
				return score + bestDps * 120f;
			}
			return score;
		}

		/// <summary>
		/// 双持已经拿满两把时要丢掉一把：专属武器永远保留；否则优先丢掉和新武器同类的那把
		/// （装枪换枪、装刀换刀），尽量保住「远程 + 近战」组合；同类再比评分。
		/// 返回要保留下来当副手的那把。
		/// </summary>
		public static ThingWithComps ChooseOffhandToKeep(
			ThingWithComps first, ThingWithComps second, ThingWithComps incoming)
		{
			bool firstExclusive = IsExclusive(first?.def);
			bool secondExclusive = IsExclusive(second?.def);
			if (firstExclusive && !secondExclusive)
			{
				return first;
			}
			if (secondExclusive && !firstExclusive)
			{
				return second;
			}

			bool incomingRanged = incoming != null && incoming.def.IsRangedWeapon;
			bool firstRanged = first.def.IsRangedWeapon;
			bool secondRanged = second.def.IsRangedWeapon;
			if (firstRanged == incomingRanged && secondRanged != incomingRanged)
			{
				return second;
			}
			if (secondRanged == incomingRanged && firstRanged != incomingRanged)
			{
				return first;
			}
			return WeaponScore(first.def) >= WeaponScore(second.def) ? first : second;
		}

		/// <summary>
		/// 装备新武器前的安排：空着副手位就把主手挪到副手；两把都满就决定丢哪把。
		/// 返回 true 表示补丁接管（调用方需要自己把 <paramref name="keep"/> 放进副手位）。
		/// </summary>
		public static bool TryPrepareEquip(
			Pawn pawn, ThingWithComps incoming, out ThingWithComps keep, out ThingWithComps discard)
		{
			keep = null;
			discard = null;
			if (pawn?.equipment == null || incoming?.def == null)
			{
				return false;
			}
			if (!NannyBotUtility.IsMkV(pawn) || !Enabled)
			{
				return false;
			}
			if (incoming.def.equipmentType != EquipmentType.Primary)
			{
				return false;
			}
			// 商队/运输舱里没有地图，掉落不可靠，交给原版处理。
			if (pawn.MapHeld == null)
			{
				return false;
			}
			ThingWithComps primary = pawn.equipment.Primary;
			if (primary == null)
			{
				return false;
			}

			ThingWithComps offhand = Offhand(pawn);
			if (offhand == null)
			{
				keep = primary;
				return true;
			}
			keep = ChooseOffhandToKeep(primary, offhand, incoming);
			discard = keep == primary ? offhand : primary;
			return true;
		}

		/// <summary>把武器直接放进装备列表的副手位（绕开 AddEquipment 的「已有主手」检查）。</summary>
		public static bool AddAsOffhand(Pawn pawn, ThingWithComps weapon)
		{
			if (pawn?.equipment == null || weapon == null)
			{
				return false;
			}
			ThingOwner owner = pawn.equipment.GetDirectlyHeldThings();
			return owner != null && owner.TryAdd(weapon);
		}

		/// <summary>
		/// 双持关闭时把副手武器放回地上，只留主手。原版 MakeRoomFor 只会丢主手，
		/// 若装备栏里还留着第二把，换装后 AddEquipment 会因「已有主手」报错并拒绝。
		/// </summary>
		public static void DisarmOffhand(Pawn pawn)
		{
			if (pawn?.equipment == null || !pawn.Spawned || pawn.Map == null)
			{
				return;
			}
			ThingWithComps primary = pawn.equipment.Primary;
			List<ThingWithComps> held = HeldWeapons(pawn);
			for (int i = 0; i < held.Count; i++)
			{
				if (held[i] == primary)
				{
					continue;
				}
				if (pawn.equipment.TryDropEquipment(held[i], out ThingWithComps dropped, pawn.Position))
				{
					dropped?.SetForbidden(false, false);
				}
			}
		}

		/// <summary>
		/// 出厂装备：主手电弧发射器，副手电弧刃（双持开启时）。组装舱、工厂和开局赠送都走这里，
		/// 保证三处产出的 Mk-V 装备一致。
		/// </summary>
		public static void GiveStartingWeapons(Pawn pawn)
		{
			if (pawn?.equipment == null)
			{
				return;
			}
			if (pawn.equipment.Primary == null)
			{
				Equip(pawn, NannyBotDefOf.CK_ArcEmitter);
			}
			if (!Enabled || pawn.equipment.Primary == null || Offhand(pawn) != null)
			{
				return;
			}
			Equip(pawn, NannyBotDefOf.CK_ArcBlade);
		}

		private static void Equip(Pawn pawn, ThingDef def)
		{
			if (def == null || def.equipmentType != EquipmentType.Primary)
			{
				return;
			}
			if (!(ThingMaker.MakeThing(def) is ThingWithComps weapon))
			{
				return;
			}
			if (pawn.MapHeld != null)
			{
				pawn.equipment.AddEquipment(weapon);
				return;
			}
			// 还没上地图（生成中途）时走原版会因「已有主手」被拒，这里直接放进对应槽位。
			if (pawn.equipment.Primary == null)
			{
				pawn.equipment.AddEquipment(weapon);
			}
			else
			{
				AddAsOffhand(pawn, weapon);
			}
		}

		/// <summary>
		/// 保证列表第一把（也就是原版 Primary）是应该拿在主手的武器：有远程就拿远程，
		/// 否则拿评分更高的。顺序不对时重排一次；顺序正确时什么都不做。
		/// </summary>
		public static void NormalizeOrder(Pawn pawn)
		{
			if (pawn?.equipment == null)
			{
				return;
			}
			List<ThingWithComps> held = HeldWeapons(pawn);
			if (held.Count < 2)
			{
				return;
			}
			ThingWithComps main = ChooseMain(held);
			if (main == null || held[0] == main)
			{
				return;
			}
			ThingOwner owner = pawn.equipment.GetDirectlyHeldThings();
			if (owner == null)
			{
				return;
			}

			List<ThingWithComps> ordered = new List<ThingWithComps>();
			ordered.Add(main);
			for (int i = 0; i < held.Count; i++)
			{
				if (held[i] != main)
				{
					ordered.Add(held[i]);
				}
			}

			for (int i = 0; i < held.Count; i++)
			{
				owner.Remove(held[i]);
			}
			for (int i = 0; i < ordered.Count; i++)
			{
				owner.TryAdd(ordered[i]);
			}
			string mainDef = main.def?.defName ?? "none";
			string offhandDef = ordered.Count > 1 ? ordered[1].def?.defName ?? "none" : "none";
			ModLog.Info("Robotics",
				$"MkV loadout reordered: main={mainDef} offhand={offhandDef}");
		}

		private static ThingWithComps ChooseMain(List<ThingWithComps> held)
		{
			ThingWithComps best = null;
			for (int i = 0; i < held.Count; i++)
			{
				best = BetterMain(best, held[i]);
			}
			return best;
		}

		/// <summary>两把武器里谁该拿在主手：有远程优先远程，同类比评分。</summary>
		public static ThingWithComps BetterMain(ThingWithComps first, ThingWithComps second)
		{
			if (first == null)
			{
				return second;
			}
			if (second == null)
			{
				return first;
			}
			return MainScore(first) >= MainScore(second) ? first : second;
		}

		private static float MainScore(ThingWithComps weapon)
		{
			float score = WeaponScore(weapon.def);
			if (weapon.def.IsRangedWeapon)
			{
				score += RangedPrimaryBonus;
			}
			return score;
		}

		/// <summary>能被第三把武器顶掉的那把：专属武器不动；两把都非专属时留评分高的。</summary>
		public static ThingWithComps ReplaceableWeapon(Pawn pawn)
		{
			ThingWithComps primary = Primary(pawn);
			ThingWithComps offhand = Offhand(pawn);
			if (primary == null || offhand == null)
			{
				return null;
			}
			bool primaryExclusive = IsExclusive(primary.def);
			bool offhandExclusive = IsExclusive(offhand.def);
			if (primaryExclusive && !offhandExclusive)
			{
				return offhand;
			}
			if (offhandExclusive && !primaryExclusive)
			{
				return primary;
			}
			if (primaryExclusive && offhandExclusive)
			{
				return null;
			}
			return WeaponScore(primary.def) <= WeaponScore(offhand.def) ? primary : offhand;
		}

		/// <summary>自动拾取的门槛：评分低于这个值的武器不值得占用副手位。</summary>
		public static float PickupBaseline(Pawn pawn, List<ThingWithComps> held)
		{
			if (held == null || held.Count == 0)
			{
				return 0f;
			}
			if (held.Count == 1)
			{
				return 0f;
			}
			ThingWithComps replaceable = ReplaceableWeapon(pawn);
			return replaceable != null ? WeaponScore(replaceable.def) : float.MaxValue;
		}

		/// <summary>背包里的备用武器，按放进背包的先后顺序排列。</summary>
		public static List<ThingWithComps> SpareWeapons(Pawn pawn)
		{
			List<ThingWithComps> result = new List<ThingWithComps>();
			ThingOwner container = pawn?.inventory?.innerContainer;
			if (container == null)
			{
				return result;
			}
			for (int i = 0; i < container.Count; i++)
			{
				if (container[i] is ThingWithComps thing && thing.def != null && thing.def.IsWeapon)
				{
					result.Add(thing);
				}
			}
			return result;
		}

		/// <summary>
		/// 背包还能不能装下这把武器。负重是「装备 + 衣物 + 背包」的总质量，容量由
		/// <see cref="MassUtility.Capacity"/> 给出（Mk-V 被 Patch_MkVCaravan 固定为 200 kg），
		/// 所以双手武器已经在扣，背包里的备用武器按真实质量继续扣。
		/// </summary>
		public static bool CanStoreAsSpare(Pawn pawn, Thing weapon, out float missingMass)
		{
			missingMass = 0f;
			if (pawn?.inventory == null || weapon?.def == null || !weapon.def.IsWeapon)
			{
				return false;
			}
			if (!NannyBotUtility.IsMkV(pawn))
			{
				return false;
			}
			float mass = weapon.GetStatValue(StatDefOf.Mass);
			missingMass = mass - MassUtility.FreeSpace(pawn);
			return missingMass <= 0f;
		}

		/// <summary>
		/// 把背包里的备用武器换到手上：手上本来只有一把就直接补第二把，已经双持时按
		/// 「保留专属武器、优先换掉和新武器同类的那把」丢一把进背包。只由玩家点选触发。
		/// 换完统一排一次序，远程武器始终回主手，避免出现「主手近战、副手枪打不响」。
		/// </summary>
		public static bool TryEquipSpare(Pawn pawn, ThingWithComps spare)
		{
			if (pawn?.equipment == null || pawn.inventory?.innerContainer == null || spare == null)
			{
				return false;
			}
			ThingOwner bag = pawn.inventory.innerContainer;
			if (!bag.Contains(spare) || spare.def == null || !spare.def.IsWeapon
				|| spare.def.equipmentType != EquipmentType.Primary)
			{
				return false;
			}
			ThingOwner held = pawn.equipment.GetDirectlyHeldThings();
			if (held == null)
			{
				return false;
			}

			ThingWithComps primary = Primary(pawn);
			ThingWithComps offhand = Offhand(pawn);
			ThingWithComps discard = null;
			if (primary != null && offhand != null)
			{
				ThingWithComps keep = ChooseOffhandToKeep(primary, offhand, spare);
				discard = keep == primary ? offhand : primary;
			}

			if (discard != null)
			{
				pawn.equipment.Remove(discard);
			}
			if (!bag.TryTransferToContainer(spare, held))
			{
				if (discard != null)
				{
					held.TryAdd(discard);
				}
				return false;
			}
			if (discard != null && !bag.TryAddOrTransfer(discard))
			{
				// 背包塞不回去就原样退回，别把武器弄没。
				held.Remove(spare);
				held.TryAdd(discard);
				bag.TryAddOrTransfer(spare);
				ModLog.Warn("Robotics",
					$"MkV loadout could not stash {discard.def?.defName} back into the backpack.");
				return false;
			}
			NormalizeOrder(pawn);
			ModLog.Info("Robotics",
				$"MkV loadout equipped spare {spare.def?.defName}; bag={SpareWeapons(pawn).Count}.");
			return true;
		}

		/// <summary>把手上的武器放进背包（玩家主动操作）。装备栏里剩下的那把自动成为主手。</summary>
		public static bool TryStashToBag(Pawn pawn, ThingWithComps weapon)
		{
			if (pawn?.equipment == null || pawn.inventory?.innerContainer == null || weapon == null)
			{
				return false;
			}
			if (!weapon.def.IsWeapon || !pawn.equipment.Contains(weapon))
			{
				return false;
			}
			ThingOwner bag = pawn.inventory.innerContainer;
			pawn.equipment.Remove(weapon);
			if (bag.TryAddOrTransfer(weapon))
			{
				return true;
			}
			pawn.equipment.AddEquipment(weapon);
			ModLog.Warn("Robotics",
				$"MkV loadout could not move {weapon.def?.defName} into the backpack.");
			return false;
		}
	}
}
