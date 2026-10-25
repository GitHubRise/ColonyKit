#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Multiplayer.API;

namespace rjw
{
	public static class AgeStage
	{
		public const int Baby = 0;
		public const int Toddler = 1;
		public const int Child = 2;
		public const int Teenager = 3;
		public const int Adult = 4;
	}

	public class Hediff_SimpleBaby : HediffWithComps
	{
		// 记录小人已成长到哪个阶段
		private int grown_to = 0;
		private int stage_completed = 0;
		public float lastTick = 0;
		public float dayTick = 0;

		//属性
		public int Grown_To
		{
			get
			{
				return grown_to;
			}
		}

		public override string DebugString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.DebugString());
			stringBuilder.Append("Current CurLifeStageIndex: " + pawn.ageTracker.CurLifeStageIndex + ", grown_to: " + grown_to);
			return stringBuilder.ToString();
		}

		public override void PostMake()
		{
			Severity = Math.Max(0.01f, Severity);
			if (grown_to == 0 && !pawn.health.hediffSet.HasHediff(HediffDef.Named("RJW_NoManipulationFlag")))
			{
				pawn.health.AddHediff(HediffDef.Named("RJW_NoManipulationFlag"), null, null);
			}
			base.PostMake();

			if (!pawn.RaceProps.lifeStageAges.Any(x => x.def.reproductive))
			{
				if (pawn.health.hediffSet.HasHediff(xxx.RJW_NoManipulationFlag))
				{
					pawn.health.hediffSet.hediffs.Remove(pawn.health.hediffSet.GetFirstHediffOfDef(xxx.RJW_NoManipulationFlag));
				}
				pawn.health.RemoveHediff(this);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref grown_to, "grown_to", 0);
			Scribe_Values.Look(ref lastTick, "lastTick", 0);
			Scribe_Values.Look(ref dayTick, "dayTick", 0);
		}

		internal void GrowUpTo(int stage)
		{
			GrowUpTo(stage, true);
		}

		[SyncMethod]
		internal void GrowUpTo(int stage, bool generated)
		{
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			if (grown_to < stage)
				grown_to = stage;

			// 更新殖民者栏
			PortraitsCache.SetDirty(pawn);
			pawn.Drawer.renderer.SetAllGraphicsDirty();

			// 处于幼儿阶段。现在可以移动和说话了。
			if ((stage >= 1 || pawn.ageTracker.CurLifeStage.reproductive) && stage_completed < 1)
			{
				Severity = Math.Max(0.5f, Severity);
				stage_completed++;
			}
			// 重新启用幼儿阶段被禁用的技能
			if ((stage >= 2 || pawn.ageTracker.CurLifeStage.reproductive) && stage_completed < 2)
			{
				// 移除阻止小人操作的隐藏状态
				if (pawn.health.hediffSet.HasHediff(xxx.RJW_NoManipulationFlag))
				{
					pawn.health.hediffSet.hediffs.Remove(pawn.health.hediffSet.GetFirstHediffOfDef(xxx.RJW_NoManipulationFlag));
				}
				Severity = Math.Max(0.75f, Severity);
				stage_completed++;
			}
			// 孩子已成长为青少年，不再需要此效果
			if ((stage >= 3 || pawn.ageTracker.CurLifeStage.reproductive) && stage_completed < 3)
			{
				// 从生活经历中获取特质
				if (pawn.story.traits.allTraits.Count < 3)
				{
					List<Trait> life_traitpool = new List<Trait>();
					// 尝试获得食人者
					if (pawn.needs.mood.thoughts.memories.Memories.Find(x => x.def == VanillaThoughtDefOf.AteHumanlikeMeatAsIngredient) != null)
					{
						life_traitpool.Add(new Trait(VanillaTraitDefOf.Cannibal, 0, false));
					}
					// 尝试获得嗜血
					if (pawn.records.GetValue(RecordDefOf.KillsHumanlikes) > 0 || pawn.records.GetValue(RecordDefOf.AnimalsSlaughtered) >= 2)
					{
						life_traitpool.Add(new Trait(TraitDefOf.Bloodlust, 0, false));
					}
					// 尝试获得射击精准
					if (pawn.records.GetValue(RecordDefOf.ShotsFired) > 100 && (int)pawn.records.GetValue(RecordDefOf.PawnsDowned) > 0)
					{
						life_traitpool.Add(new Trait(TraitDef.Named("ShootingAccuracy"), 1, false));
					}
					else if (pawn.records.GetValue(RecordDefOf.ShotsFired) > 100 && (int)pawn.records.GetValue(RecordDefOf.PawnsDowned) == 0)
					{
						life_traitpool.Add(new Trait(TraitDef.Named("ShootingAccuracy"), -1, false));
					}
					// 尝试获得格斗者
					else if (pawn.records.GetValue(RecordDefOf.ShotsFired) < 15 && pawn.records.GetValue(RecordDefOf.PawnsDowned) > 1)
					{
						life_traitpool.Add(new Trait(TraitDefOf.Brawler, 0, false));
					}
					// 尝试获得恋尸癖
					if (pawn.records.GetValue(RecordDefOf.CorpsesBuried) > 50)
					{
						life_traitpool.Add(new Trait(xxx.necrophiliac, 0, false));
					}
					// 尝试获得花痴
					if (pawn.records.GetValue(RecordDefOf.BodiesStripped) > 50)
					{
						life_traitpool.Add(new Trait(xxx.nymphomaniac, 0, false));
					}
					// 尝试获得强奸犯
					if (pawn.records.GetValue(RecordDefOf.TimeAsPrisoner) > 300)
					{
						life_traitpool.Add(new Trait(xxx.rapist, 0, false));
					}
					// 尝试获得讨厌男性/女性
					int male_rivals = 0;
					int female_rivals = 0;
					foreach (Pawn colinist in Find.AnyPlayerHomeMap.mapPawns.AllPawnsSpawned)
					{
						if (pawn.relations.OpinionOf(colinist) <= -20)
						{
							if (colinist.gender == Gender.Male)
								male_rivals++;
							else
								female_rivals++;
						}
					}
					// 找出我们讨厌哪个性别
					if (male_rivals > 3 || female_rivals > 3)
					{
						if (male_rivals > female_rivals)
							life_traitpool.Add(new Trait(TraitDefOf.DislikesMen, 0, false));
						else if (female_rivals > male_rivals)
							life_traitpool.Add(new Trait(TraitDefOf.DislikesWomen, 0, false));
					}
					// 纵火狂从不灭火。这看起来有点蠢
					/*if ((int)pawn.records.GetValue (RecordDefOf.FiresExtinguished) == 0) {
						life_traitpool.Add (new Trait (TraitDefOf.Pyromaniac, 0, false));
					}*/
					// 神经质
					if (pawn.records.GetValue(RecordDefOf.TimesInMentalState) > 6)
					{
						life_traitpool.Add(new Trait(TraitDef.Named("Neurotic"), 2, false));
					}
					else if (pawn.records.GetValue(RecordDefOf.TimesInMentalState) > 3)
					{
						life_traitpool.Add(new Trait(TraitDef.Named("Neurotic"), 1, false));
					}

					// 人们（无论男女）都可能在青春期变成同性恋
					//Rand.PopState();
					//Rand.PushState(RJW_Multiplayer.PredictableSeed());
					if (Rand.Value <= 0.03f && pawn.story.traits.allTraits.Count <= 3)
					{
						pawn.story.traits.GainTrait(new Trait(TraitDefOf.Gay, 0, true));
					}

					// 现在尝试添加一些生活经历特质
					if (life_traitpool.Count > 0)
					{
						int i = 3;
						while (pawn.story.traits.allTraits.Count < 3 && i > 0)
						{
							Trait newtrait = life_traitpool.RandomElement();
							if (pawn.story.traits.HasTrait(newtrait.def) == false)
								pawn.story.traits.GainTrait(newtrait);
							i--;
						}
					}
				}

				stage_completed++;
				pawn.health.RemoveHediff(this);
			}
		}

		//每天增长 1 岁，直到具备生育能力
		public void GrowFast()
		{
			if (RJWPregnancySettings.phantasy_pregnancy)
			{
				if (!pawn.ageTracker.CurLifeStage.reproductive || pawn.ageTracker.Growth >= 1)
				{
					pawn.ageTracker.AgeBiologicalTicks = (pawn.ageTracker.AgeBiologicalYears + 1) * GenDate.TicksPerYear + 1;
					pawn.ageTracker.DebugForceBirthdayBiological();
					GrowUpTo(pawn.ageTracker.CurLifeStageIndex, false);
				}
			}
		}

		public void TickRare()
		{
			//--ModLog.Message("Hediff_SimpleBaby::TickRare is called");
			if (pawn.ageTracker.CurLifeStageIndex > Grown_To)
			{
				GrowUpTo(pawn.ageTracker.CurLifeStageIndex, false);
			}
		}

		public override void Tick()
		{
			//此 void 每帧都会调用，没有理由打日志
			//--ModLog.Message("Hediff_SimpleBaby::PostTick is called");

			base.Tick();
			if (pawn.Spawned)
			{
				var thisTick = Find.TickManager.TicksGame;

				if ((thisTick - dayTick) >= GenDate.TicksPerDay)
				{
					GrowFast();
					dayTick = thisTick;
				}
				if ((thisTick - lastTick) >= 250)
				{
					TickRare();
					lastTick = thisTick;
				}
			}
		}

		public override bool Visible
		{
			get { return false; }
		}
	}
}
