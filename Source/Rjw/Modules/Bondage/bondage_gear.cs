#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	//public static class bondage_gear_tradeability
	//{
	//	public static void init()
	//	{
	//		// 允许商人出售束缚装备
	//		if (xxx.config.bondage_gear_enabled)
	//		{
	//			foreach (var def in DefDatabase<bondage_gear_def>.AllDefs)
	//				def.tradeability = Tradeability.Sellable;
	//		}
	//		// 禁止商人出售束缚装备
	//		else
	//		{
	//			foreach (var def in DefDatabase<bondage_gear_def>.AllDefs)
	//				def.tradeability = Tradeability.None;
	//		}
	//	}
	//}

	public static class bondage_gear_extensions
	{
		public static bool has_lock(this Apparel app)
		{
			return (app.TryGetComp<CompHoloCryptoStamped>() != null);
		}

		public static bool is_wearing_locked_apparel(this Pawn p)
		{
			if (p.apparel != null)
				foreach (var app in p.apparel.WornApparel)
					if (app.has_lock())
						return true;
			return false;
		}

		// 尝试让 p 开始执行对另一个小人或对自己（当 "other" 为 null 时）使用物品的工作。
		// 当然，此方法要生效，物品的 useJob 必须能够处理对其他小人使用的情况。这对
		// RJW 中的 holokey 和束缚装备成立，但对核心游戏中的物品不成立。
		public static void start_job(this CompUsable usa, Pawn p, LocalTargetInfo tar)
		{
			if (p.CanReserveAndReach(usa.parent, PathEndMode.Touch, Danger.Some) &&
				((tar == null) || p.CanReserveAndReach(tar, PathEndMode.Touch, Danger.Some)))
			{
				var comfor = usa.parent.GetComp<CompForbiddable>();
				if (comfor != null)
					comfor.Forbidden = false;
				var job = JobMaker.MakeJob(((CompProperties_Usable)usa.props).useJob, usa.parent, tar);
				p.jobs.TryTakeOrderedJob(job);
			}
		}

		// 创建一个使用物品的菜单选项。"tar" 应为小人、尸体，若不存在则为 null（此时
		// 小人大概会对自己使用该物品）。"required_work" 也可以为 null。
		public static FloatMenuOption make_option(this CompUsable usa, string label, Pawn p, LocalTargetInfo tar, WorkTypeDef required_work)
		{
			if ((tar != null) && (!p.CanReserve(tar)))
			{
				string key = "Reserved";
				string text = TranslatorFormattedStringExtensions.Translate(key);
				return new FloatMenuOption(label + " (" + text + ")", null, MenuOptionPriority.DisabledOption);
			}
			else if ((tar != null) && (!p.CanReach(tar, PathEndMode.Touch, Danger.Some)))
			{
				string key = "NoPath";
				string text = TranslatorFormattedStringExtensions.Translate(key);
				return new FloatMenuOption(label + " (" + text + ")", null, MenuOptionPriority.DisabledOption);
			}
			else if ((required_work != null) && p.WorkTagIsDisabled(required_work.workTags))
			{
				string key = "CannotPrioritizeWorkTypeDisabled";
				string text = TranslatorFormattedStringExtensions.Translate(key, required_work.gerundLabel);
				return new FloatMenuOption(label + " (" + text + ")", null, MenuOptionPriority.DisabledOption);
			}
			else
				return new FloatMenuOption(
					label,
					delegate
					{
						usa.start_job(p, tar);
					},
					MenuOptionPriority.Default);
		}
	}

	public class bondage_gear_def : ThingDef
	{
		public Type soul_type;
		public HediffDef equipped_hediff = null;
		public bool gives_bound_moodlet = false;
		public bool gives_gagged_moodlet = false;
		public bool blocks_hands = false;
		public bool blocks_oral = false;
		public bool blocks_penis = false;
		public bool blocks_vagina = false;
		public bool blocks_anus = false;
		public bool blocks_breasts = false;
		private bondage_gear_soul soul_ins = null;

		public List<BodyPartDef> HediffTargetBodyPartDefs;      //用于可选的目标部位列表的字段，供施加状态使用
		public List<BodyPartGroupDef> BoundBodyPartGroupDefs;   //用于可选分组列表的字段，这些分组被禁止进行动词施放

		public bondage_gear_soul soul
		{
			get
			{
				if (soul_ins == null)
					soul_ins = (bondage_gear_soul)Activator.CreateInstance(soul_type);
				return soul_ins;
			}
		}
	}

	public class bondage_gear_soul
	{
		// 添加束缚装备关联的 HediffDef，并生成与之匹配的 holokey
		public virtual void on_wear(Pawn wearer, Apparel gear)
		{
			var def = (bondage_gear_def)gear.def;

			if (def.equipped_hediff != null && def.HediffTargetBodyPartDefs != null)
			{
				foreach (BodyPartDef partDef in def.HediffTargetBodyPartDefs)                           //获取 BodyPartDef，例如 "Arm"
				{
					foreach (BodyPartRecord partRec in wearer.RaceProps.body.GetPartsWithDef(partDef))  //将状态施加到小人身上找到的每一条手臂
					{
						wearer.health.AddHediff(def.equipped_hediff, partRec);
					}
				}
			}
			else if (def.equipped_hediff != null && def.HediffTargetBodyPartDefs == null)               //向后兼容：不含 HediffTargetBodyPartDefs 的简化装备定义
			{                                                                                           //状态施加于整个身体
				wearer.health.AddHediff(def.equipped_hediff);
			}

			var gear_stamp = gear.TryGetComp<CompHoloCryptoStamped>();
			if (gear_stamp != null)
			{
				var key = ThingMaker.MakeThing(ThingDef.Named("Holokey"));
				var key_stamp = key.TryGetComp<CompHoloCryptoStamped>();
				key_stamp.copy_stamp_from(gear_stamp);
				if (wearer.Map != null)
					GenSpawn.Spawn(key, wearer.Position, wearer.Map);
				else
					wearer.inventory.TryAddItemNotForSale(key);
			}
		}

		// 移除该装备的 HediffDef
		public virtual void on_remove(Apparel gear, Pawn former_wearer)
		{
			var def = (bondage_gear_def)gear.def;
			if (def.equipped_hediff != null && def.HediffTargetBodyPartDefs != null)
			{                                                                                   //根据 equipped_hediff 定义获取所有状态
				List<Hediff> hediffs = former_wearer.health.hediffSet.hediffs.Where(x => x.def == def.equipped_hediff).ToList();
				foreach (Hediff hedToRemove in hediffs)
				{
					if (def.HediffTargetBodyPartDefs.Contains(hedToRemove.Part.def))            //若由该 bondage_gear 施加则移除
						former_wearer.health.RemoveHediff(hedToRemove);                         //假设可能存在多种不同的束缚装备
				}                                                                               //使用相同的 equipped_hediff 定义
			}
			else if (def.equipped_hediff != null && def.HediffTargetBodyPartDefs == null)       //向后兼容：不含 HediffTargetBodyPartDefs 的简化装备定义
			{
				var hed = former_wearer.health.hediffSet.GetFirstHediffOfDef(def.equipped_hediff);
				if (hed != null)
					former_wearer.health.RemoveHediff(hed);
			}
		}
	}

	// 当束缚装备未被穿戴时给其极低的评分，使小人永不会自行装备它，并在
	// 其被穿戴时给其极高的评分，使小人永不会试图脱下它去装备“更好”的东西。
	public class bondage_gear : Apparel
	{
		// 使此方法对任何 bondage_gear 通用；若装备的 BoundBodyPartGroupDefs 为空或 null，则不会有任何影响
		public override bool AllowVerbCast(Verb verb)
		{
			if ((this.def as bondage_gear_def).BoundBodyPartGroupDefs != null &&
				verb.tool != null &&
				(this.def as bondage_gear_def).BoundBodyPartGroupDefs.Contains(verb.tool.linkedBodyPartsGroup))
			{
				return false;
			}
			return true;
		}

		//仅为存档兼容性所需
		public override void ExposeData()
		{
			base.ExposeData();
			//if (Scribe.mode == LoadSaveMode.PostLoadInit)
			//	CheckHediffs();
		}

		//存档兼容性保险措施，可防止 Armbinder 状态在全身出现部位效率为 0 的情况
		private void CheckHediffs()
		{
			var def = (bondage_gear_def)this.def;
			if (this.Wearer == null || def.equipped_hediff == null) return;

			bool changedHediff = false;
			void ApplyHediffDirect(HediffDef hedDef, BodyPartRecord partRec)
			{
				Hediff hediff = (Hediff)HediffMaker.MakeHediff(hedDef, Wearer);
				if (partRec != null)
					hediff.Part = partRec;
				Wearer.health.hediffSet.AddDirect(hediff);
				changedHediff = true;
			}

			void RemoveHediffDirect(Hediff hed)
			{
				Wearer.health.hediffSet.hediffs.Remove(hed);
				changedHediff = true;
			}

			List<bondage_gear> wornBondageGear = Wearer.apparel.WornApparel.Where(x => x is bondage_gear).Cast<bondage_gear>().ToList();
			List<Hediff> hediffs = new List<Hediff>();
			foreach (Hediff h in this.Wearer.health.hediffSet.hediffs) hediffs.Add(h);

			//检查由 bondage_gear 定义的当前状态是否位于规定位置，清理错位者
			bool equippedHediff;
			bool onPlace;
			foreach (Hediff hed in hediffs)
			{
				equippedHediff = false;
				onPlace = false;
				foreach (bondage_gear gear in wornBondageGear)
				{
					if (hed.def == (gear.def as bondage_gear_def).equipped_hediff)
					{
						//若状态来自 bondage_gear 且位于其规定位置，则不要动它，否则移除
						//假设可能存在多种不同的束缚装备，它们具有相同的 equipped_hediff 定义但不同的状态目标部位，不知为何
						equippedHediff = true;

						if ((hed.Part != null && (gear.def as bondage_gear_def).HediffTargetBodyPartDefs == null))
						{
							//略过
						}
						else if ((hed.Part == null && (gear.def as bondage_gear_def).HediffTargetBodyPartDefs == null))
						{
							onPlace = true;
							break;
						}
						else if (hed.Part != null && (gear.def as bondage_gear_def).HediffTargetBodyPartDefs.Contains(hed.Part.def))
						{
							onPlace = true;
							break;
						}
					}
				}
				if (equippedHediff && !onPlace)
				{
					ModLog.Message("Removing Hediff " + hed.Label + " from " + Wearer + (hed.Part == null ? "'s body" : "'s " + hed.Part));
					RemoveHediffDirect(hed);
				}
			}

			// 现在遍历每件装备，使所有状态都就位，并补上缺失的
			foreach (bondage_gear gear in wornBondageGear)
			{
				if ((gear.def as bondage_gear_def).equipped_hediff == null) continue;
				if ((gear.def as bondage_gear_def).HediffTargetBodyPartDefs == null)                        //处理不含 HediffTargetBodyPartDefs 的装备
				{

					Hediff hed = Wearer.health.hediffSet.hediffs.Find(x => (x.def == (gear.def as bondage_gear_def).equipped_hediff && x.Part == null));//检查由装备定义的、作用于全身的状态
					if (hed == null)                                                                        //如果没有合法状态，则添加
					{
						ModLog.Message("Adding missing Hediff " + (gear.def as bondage_gear_def).equipped_hediff.label + " to " + Wearer + "'s body");
						ApplyHediffDirect((gear.def as bondage_gear_def).equipped_hediff, null);
					}
				}
				else                                                                                            //处理定义了 HediffTargetBodyPartDefs 的装备
				{
					foreach (BodyPartDef partDef in (gear.def as bondage_gear_def).HediffTargetBodyPartDefs)    //获取每个 partDef
					{
						foreach (BodyPartRecord partRec in Wearer.RaceProps.body.GetPartsWithDef(partDef))      //检查该定义的所有部位是否有已施加的状态
						{
							Hediff hed = Wearer.health.hediffSet.hediffs.Find(x => (x.def == (gear.def as bondage_gear_def).equipped_hediff && x.Part == partRec));//检查由装备定义的、位于规定位置的状态
							if (hed == null)                                                                    //如果状态缺失，则添加
							{
								ModLog.Message("Adding missing Hediff " + (gear.def as bondage_gear_def).equipped_hediff.label + " to " + Wearer + "'s " + partRec.Label);
								ApplyHediffDirect((gear.def as bondage_gear_def).equipped_hediff, partRec);
							}
						}
					}
				}
				//不考虑多件不同物品具有相同的 equipped_hediff 定义且作用于同一部位的可能，那太离谱了
			}
			if (changedHediff)
			{
				//若涉及 capacity.Manipulation，当前 toil 在 Notify_HediffChanged() 中可能出错，所以放到最后执行。
				//大概无害，修正后的状态被保存后应该不会再发生
				Wearer.health.Notify_HediffChanged(null);
			}
		}
	}

	public class armbinder : bondage_gear
	{
		// 防止被 Armbinder 束缚的小人进行近战攻击
		//public override bool AllowVerbCast (IntVec3 root, TargetInfo targ)
		//{
		//	return false;
		//}
	}

	public class yoke : bondage_gear
	{
		// 防止被 Armbinder 束缚的小人进行近战攻击
		//public override bool AllowVerbCast (IntVec3 root, TargetInfo targ)
		//{
		//	return false;
		//}
	}

	public class Restraints : bondage_gear
	{
		// 防止被 Armbinder 束缚的小人进行近战攻击
		//public override bool AllowVerbCast (IntVec3 root, TargetInfo targ)
		//{
		//	return false;
		//}
	}

	public class force_off_gear_def : RecipeDef
	{
		public ThingDef removes_apparel;
		public BodyPartDef failure_affects;
		public List<BodyPartDef> destroys_one_of = null;
		public List<BodyPartDef> major_burns_on = null;
		public List<BodyPartDef> minor_burns_on = null;
	}
}
