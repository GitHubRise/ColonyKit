#nullable disable

using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Text;
using Verse.AI.Group;
using Multiplayer.API;

namespace rjw
{
	public class Hediff_InsectEgg : HediffWithComps
	{
		/*
		 * 开发备注：要方便地测试产卵，可在存档文件中把 p_end_tick 设为 p_start_tick + 10。
		 */

		public float p_start_tick = 0;
		public float p_end_tick = 0;
		public float lastTick = 0;

		private HediffDef_InsectEgg HediffDefOfEgg => (HediffDef_InsectEgg)def;

		public string parentDef => HediffDefOfEgg.parentDef;

		public List<string> parentDefs => HediffDefOfEgg.parentDefs;
		public List<GeneDef> eggGenes;

		// TODO：我想把“father”改成“fertilizer”，但那是破坏性改动。
		public Pawn father;					//可以是蛋中定义的 parentkind
		public Pawn implanter;					//可以是任意小人
		public bool canbefertilized = true;
		public bool fertilized => father != null;
		public float eggssize = 0.1f;
		protected List<Pawn> babies;
		float Gestation = 0f;

		///宫缩持续时间，实际上是额外的一个状态阶段，一个为了让分娩过程更显眼的粗暴 hack
		//protected const int TicksPerHour = 2500;
		protected int contractions = 0;

		public override string LabelBase
		{
			get
			{
				if (Prefs.DevMode && implanter != null)
				{
						return implanter.kindDef.race.label + "Hediff_InsectEgg_egg".Translate();
				}

				if (eggssize <= 0.10f)
					return "Hediff_InsectEgg_small".Translate();
				if (eggssize <= 0.3f)
					return "Hediff_InsectEgg_medium".Translate();
				else if (eggssize <= 0.5f)
					return "Hediff_InsectEgg_big".Translate();
				else
					return "Hediff_InsectEgg_huge".Translate();
			}
		}

		public override string LabelInBrackets
		{
			get
			{
				if (Prefs.DevMode)
				{
					if (fertilized)
						return "Hediff_InsectEgg_fertilized".Translate();
					else
						return "Hediff_InsectEgg_unfertilized".Translate();
				}
				return null;
			}
		}

		public float GestationProgress
		{
			get => Gestation;
			set => Gestation = value;
		}

		public override bool TryMergeWith(Hediff other)
		{
			return false;
		}

		public override void Tick()
		{
			var thisTick = Find.TickManager.TicksGame;
			GestationProgress = (1 + thisTick - p_start_tick) / (p_end_tick - p_start_tick);

			if ((thisTick - lastTick) >= 1000)
			{
				lastTick = thisTick;
				//分娩耗时一小时
				if (GestationProgress >= 1 && contractions == 0 && !(pawn.jobs.curDriver is JobDriver_Sex))
				{
					if (PawnUtility.ShouldSendNotificationAbout(pawn) && (pawn.IsColonist || pawn.IsPrisonerOfColony))
					{
						string key = "RJW_EggContractions";
						string text = TranslatorFormattedStringExtensions.Translate(key, pawn.LabelIndefinite());
						Messages.Message(text, pawn, MessageTypeDefOf.NeutralEvent);
					}
					contractions++;
					if (!Genital_Helper.has_ovipositorF(pawn))
						pawn.health.AddHediff(xxx.submitting);
				}
				else if (GestationProgress >= 1 && contractions != 0 && (pawn.CarriedBy == null || pawn.CarriedByCaravan()))
				{
					if (PawnUtility.ShouldSendNotificationAbout(pawn) && (pawn.IsColonist || pawn.IsPrisonerOfColony))
					{
						string key1 = "RJW_GaveBirthEggTitle";
						string message_title = TranslatorFormattedStringExtensions.Translate(key1, pawn.LabelIndefinite());
						string key2 = "RJW_GaveBirthEggText";
						string message_text = TranslatorFormattedStringExtensions.Translate(key2, pawn.LabelIndefinite());

						Messages.Message(message_text, pawn, MessageTypeDefOf.SituationResolved);
					}
					GiveBirth();
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref father, "father", true);
			Scribe_References.Look(ref implanter, "implanter", true);
			Scribe_Values.Look(ref Gestation, "Gestation");
			Scribe_Values.Look(ref p_start_tick, "p_start_tick", 0);
			Scribe_Values.Look(ref p_end_tick, "p_end_tick", 0);
			Scribe_Values.Look(ref lastTick, "lastTick", 0);
			Scribe_Values.Look(ref eggssize, "eggssize", 0.1f);
			Scribe_Values.Look(ref canbefertilized, "canbefertilized", true);
			Scribe_Collections.Look(ref eggGenes, label: "genes", lookMode: LookMode.Def);
			Scribe_Collections.Look(ref babies, "babies", saveDestroyedThings: true,  lookMode: LookMode.Deep, ctorArgs: new object[0]);
		}
		public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
		{
			base.Notify_PawnDied(dinfo, culprit);
			GiveBirth();
		}

		//以后应改成先产下蛋，再在几个 tick 内孵化
		[SyncMethod]
		public void GiveBirth()
		{
			Pawn mother = pawn;
			Pawn baby = null;

			if (RJWSettings.DevMode) ModLog.Message($"{pawn} is giving birth to an InsectEgg");
			if (fertilized)
			{
				if (RJWSettings.DevMode) ModLog.Message($"Egg was fertilized - Fertilizer {father} and Implanter {implanter}");
				PawnKindDef spawn_kind_def = implanter.kindDef;

				Faction spawn_faction = DetermineSpawnFaction(mother);
				spawn_kind_def = AdjustSpawnKindDef(mother, spawn_kind_def);

				if (!spawn_kind_def.RaceProps.Humanlike)
				{
					BirthFertilizedAnimalEgg(mother, spawn_kind_def);
				}
				else //类人婴儿
				{
					baby = ProcessHumanLikeInsectEgg(mother, spawn_kind_def, spawn_faction);
				}
				UpdateBirtherRecords(mother);
			}
			else
			{
				BirthUnfertilizedEggs(mother);
			}
			// 产后
			ProcessPostBirth(mother, baby);

			mother.health.RemoveHediff(this);
		}

		/// <summary>
		/// 增加蛋的出生统计。
		/// </summary>
		/// <param name="mother">产下新蛋的小人</param>
		private static void UpdateBirtherRecords(Pawn mother)
		{

			if (RJWSettings.DevMode) ModLog.Message($"Updating {mother}s records after birthing an egg"); 
			
			mother.records.AddTo(xxx.CountOfBirthEgg, 1);
		}

		/// <summary>
		/// 若为花痴则调整 Pawn_Kind_Def。
		/// 问题在于后续处理无法很好地应对花痴。
		/// </summary>
		/// <param name="mother">母亲，用于可能的备选查询</param>
		/// <param name="spawn_kind_def">当前的 spawn_kind_def</param>
		/// <returns></returns>
		[SyncMethod]
		private PawnKindDef AdjustSpawnKindDef(Pawn mother, PawnKindDef spawn_kind_def)
		{
			if (spawn_kind_def.defName.Contains("Nymph"))
			{
				//孩子是花痴，尝试寻找其他 PawnKindDef
				var spawn_kind_def_list = new List<PawnKindDef>();
				spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == spawn_kind_def.race && !x.defName.Contains("Nymph")));
				//未找到其他 PawnKindDef，尝试用母亲
				if (spawn_kind_def_list.NullOrEmpty())
					spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == mother.kindDef.race && !x.defName.Contains("Nymph")));
				//未找到其他 PawnKindDef，尝试用植入者
				if (spawn_kind_def_list.NullOrEmpty() && implanter != null)
					spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == implanter.kindDef.race && !x.defName.Contains("Nymph")));
				//未找到其他 PawnKindDef，回退到通用殖民者
				if (spawn_kind_def_list.NullOrEmpty())
					spawn_kind_def = PawnKindDefOf.Colonist;

				spawn_kind_def = spawn_kind_def_list.RandomElement();
			}

			return spawn_kind_def;
		}

		[SyncMethod]
		private void ProcessPostBirth(Pawn mother, Pawn baby)
		{
			if (mother.Spawned)
			{
				// 生成黏液
				if (mother.caller != null)
				{
					mother.caller.DoCall();
				}
				if (baby != null)
				{
					if (baby.caller != null)
					{
						baby.caller.DoCall();
					}
				}
				FilthMaker.TryMakeFilth(mother.Position, mother.Map, ThingDefOf.Filth_AmnioticFluid, mother.LabelIndefinite(), 5);
				int howmuch = Rand.Range(1, 3);
				int i = 0;
				if (xxx.is_insect(baby) || xxx.is_insect(mother) || xxx.is_insect(implanter) || xxx.is_insect(father))
				{
					while (i++ < howmuch)
					{
						Thing jelly = ThingMaker.MakeThing(ThingDefOf.InsectJelly);
						if (mother.MapHeld?.areaManager?.Home[mother.PositionHeld] == false)
							jelly.SetForbidden(true, false);
						GenPlace.TryPlaceThing(jelly, mother.PositionHeld, mother.MapHeld, ThingPlaceMode.Near);
					}
				}
			}
		}

		/// <summary>
		/// 产出未受精的蛋。
		/// </summary>
		/// <param name="mother"></param>
		private void BirthUnfertilizedEggs(Pawn mother)
		{
			if (RJWSettings.DevMode) ModLog.Message($"{mother} is giving 'birth' to unfertilized Insect-Eggs.");

			if (PawnUtility.ShouldSendNotificationAbout(pawn) && (pawn.IsColonist || pawn.IsPrisonerOfColony))
			{
				string key = "EggDead";
				string text = TranslatorFormattedStringExtensions.Translate(key, pawn.LabelIndefinite()).CapitalizeFirst();
				Messages.Message(text, pawn, MessageTypeDefOf.SituationResolved);
			}
			Thing egg = CreateUnfertilizedEggThing();
			var map = mother.MapHeld;
			if (map != null)
			{
				var pos = mother.PositionHeld;
				if (map?.areaManager?.Home[pos] == false)
					egg.SetForbidden(true, false);
				GenPlace.TryPlaceThing(egg, pos, map, ThingPlaceMode.Near);
			}
			else
			{
				//TODO 若母亲在远行队中则放入远行队/若母亲有物品栏则放入物品栏？
			}
			
		}

		private Pawn ProcessHumanLikeInsectEgg(Pawn mother, PawnKindDef spawn_kind_def, Faction spawn_faction)
		{
			ModLog.Message("Hediff_InsectEgg::BirthBaby() " + spawn_kind_def + " of " + spawn_faction + " in " + (int)(50 * implanter.GetStatValue(StatDefOf.PsychicSensitivity)) + " chance!");

			PawnGenerationRequest request = new PawnGenerationRequest(
				kind: spawn_kind_def,
				faction: spawn_faction,
				forceGenerateNewPawn: true,
				developmentalStages: DevelopmentalStage.Newborn,
				allowDowned: true,
				canGeneratePawnRelations: false,
				colonistRelationChanceFactor: 0,
				allowFood: false,
				allowAddictions: false,
				relationWithExtraPawnChanceFactor: 0,
				//forceNoIdeo: true,
				forbidAnyTitle: true,
				forceNoBackstory: true);

			Pawn baby = PawnGenerator.GeneratePawn(request);
			// 若我们有基因而婴儿尚未设置任何基因，则把受精时保存的基因作为内源基因加入
			if (RJWPregnancySettings.egg_pregnancy_genes && eggGenes != null && eggGenes.Count > 0)
			{
				if (RJWSettings.DevMode) ModLog.Message($"Setting {eggGenes.Count} genes for {baby}");
				foreach (var gene in baby.genes.GenesListForReading)
					baby.genes.RemoveGene(gene);
				foreach (var gene in eggGenes)
					baby.genes.AddGene(gene, false);
			} else
			{
				if (RJWSettings.DevMode) ModLog.Message($"Did not find genes for {baby}");
			}

			DetermineIfBabyIsPrisonerOrSlave(mother, baby);

			SetBabyRelations(baby,implanter,father, mother);

			PawnUtility.TrySpawnHatchedOrBornPawn(baby, mother);

			if (spawn_faction == Faction.OfInsects || (spawn_faction != null && (spawn_faction.def.defName.Contains("insect") || spawn_faction == implanter.Faction)))
			{
				Lord lord = implanter.GetLord();
				if (lord != null)
				{
					lord.AddPawn(baby);
				}
			}

			Hediff_BasePregnancy.BabyPostBirth(mother, father, baby);
			//Sexualizer.sexualize_pawn(baby);

			// 把婴儿移到母亲前方，而不是放在其身上
			if (mother.Spawned)
				if (mother.CurrentBed() != null)
				{
					baby.Position = baby.Position + new IntVec3(0, 0, 1).RotatedBy(mother.CurrentBed().Rotation);
				}

			return baby;
		}

		private void SetBabyRelations(Pawn Baby, Pawn EggProducer, Pawn Fertilizer, Pawn Host)
		{
			if (RJWSettings.DevMode) ModLog.Message($"Setting Relations for {Baby}: Egg-Mother={EggProducer}, Egg-Fertilizer={Fertilizer}, Egg-Host={Host}");
            if (!RJWSettings.Disable_egg_pregnancy_relations)
				if (Baby.RaceProps.IsFlesh)
				{
					if (EggProducer != null)
						Baby.relations.AddDirectRelation(PawnRelationDefOf.Parent, EggProducer);
					// 注意：根据受精者的性别，它也会被标注为“母亲”。
					if (Fertilizer != null)
						Baby.relations.AddDirectRelation(PawnRelationDefOf.Parent, Fertilizer);
					if (Host != null && Host != EggProducer)
						Baby.relations.AddDirectRelation(PawnRelationDefOf.ParentBirth, Host);
				}
		}

		private static void DetermineIfBabyIsPrisonerOrSlave(Pawn mother, Pawn baby)
		{
			if (mother.IsSlaveOfColony)
			{
				if (mother.SlaveFaction != null)
					baby.SetFaction(mother.SlaveFaction);
				else if (mother.HomeFaction != null)
					baby.SetFaction(mother.HomeFaction);
				else if (mother.Faction != null)
					baby.SetFaction(mother.Faction);
				else
					baby.SetFaction(Faction.OfPlayer);
				baby.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
			}
			else if (mother.IsPrisonerOfColony)
			{
				if (mother.HomeFaction != null)
					baby.SetFaction(mother.HomeFaction);
				baby.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
			}
		}

		private Thing CreateUnfertilizedEggThing()
		{
			var eggDef = DefDatabase<ThingDef>.GetNamedSilentFail(HediffDefOfEgg.UnFertEggDef);
			if (eggDef == null)
				eggDef = DefDatabase<ThingDef>.GetNamedSilentFail("RJW_EggInsectUnfertilized");

			return ThingMaker.MakeThing(eggDef);
		}

		[SyncMethod]
		private void BirthFertilizedAnimalEgg(Pawn mother, PawnKindDef spawn_kind_def)
		{
			if (RJWSettings.DevMode) ModLog.Message($"{mother} is birthing an fertilized egg of {spawn_kind_def} ");

			Thing egg = CreateFertilizedEggThing();
			string childrendef = "";
			PawnKindDef children = null;

			//生成孩子
			List<string> childlist = new List<string>();
			if (!HediffDefOfEgg.childrenDefs.NullOrEmpty())
			{
				foreach (var child in HediffDefOfEgg.childrenDefs)
				{
					if (DefDatabase<PawnKindDef>.GetNamedSilentFail(child) != null)
						childlist.AddDistinct(child);
				}
				childrendef = childlist.RandomElement();                                                    //尝试查找预定义
			}
			if (!childrendef.NullOrEmpty())
				children = DefDatabase<PawnKindDef>.GetNamedSilentFail(childrendef);

			if (children == null)                                                                           //使用 fatherDef
				children = spawn_kind_def;

			//把孩子放入蛋中
			if (children != null)
			{
				var t = egg.TryGetComp<CompHatcher>();
				t.Props.hatcherPawn = children;
				t.hatcheeParent = implanter;
				t.otherParent = father;
				t.hatcheeFaction = implanter.Faction;
			}

			var map = mother.MapHeld;
			if (map != null)
			{
				var pos = mother.PositionHeld;
				if (map?.areaManager?.Home[pos] == false)
					egg.SetForbidden(true, false);
				GenPlace.TryPlaceThing(egg, pos, map, ThingPlaceMode.Near);
			}
			else
			{
				//TODO 若母亲在远行队中则放入远行队/若母亲有物品栏则放入物品栏？
			}
		}

		private Thing CreateFertilizedEggThing()
		{
			var eggDef = DefDatabase<ThingDef>.GetNamedSilentFail(HediffDefOfEgg.FertEggDef);
			if (eggDef == null)
				eggDef = DefDatabase<ThingDef>.GetNamedSilentFail("RJW_EggInsectFertilized");

			return ThingMaker.MakeThing(eggDef);
		}

		private Faction DetermineSpawnFaction(Pawn mother)
		{
			Faction spawn_faction;

			//核心的虫巢昆虫……大概
			if (implanter.Faction == Faction.OfInsects || father.Faction == Faction.OfInsects || mother.Faction == Faction.OfInsects)
			{
				spawn_faction = Faction.OfInsects;
				int chance = 5;

				//随机使昆虫变为中立/可驯服
				if (father.Faction == Faction.OfInsects)
					chance = 5;
				if (father.Faction != Faction.OfInsects)
					chance = 10;
				if (father.Faction == Faction.OfPlayer)
					chance = 25;
				if (implanter.Faction == Faction.OfPlayer)
					chance += 25;
				if (implanter.Faction == Faction.OfPlayer && xxx.is_human(implanter))
					chance += (int)(25 * implanter.GetStatValue(StatDefOf.PsychicSensitivity));
				if (Rand.Range(0, 100) <= chance)
					spawn_faction = null;

				//出生时有一定几率驯服昆虫 
				if (spawn_faction == null)
					if (implanter.Faction == Faction.OfPlayer && xxx.is_human(implanter))
						if (Rand.Range(0, 100) <= (int)(50 * implanter.GetStatValue(StatDefOf.PsychicSensitivity)))
							spawn_faction = Faction.OfPlayer;
			}
			//类人
			else if (xxx.is_human(implanter))
			{
				spawn_faction = implanter.Faction;
			}
			//动物，生成植入者所属派系（若不是玩家派系/未驯服）
			else if (!xxx.is_human(implanter) && !(implanter.Faction?.IsPlayer ?? false))
			{
				spawn_faction = implanter.Faction;
			}
			//生成无派系个体（大概可驯服）
			else
			{
				spawn_faction = null;
			}

			return spawn_faction;
		}

		/// <summary>
		/// 设置蛋的父亲。
		/// 若可能，也会确定婴儿的基因并存入蛋怀孕中。
		/// 例外是机器人或不死者（模组内容）。
		/// </summary>
		/// <param name="Pawn"> 要设为父亲的小人 </param>
		public void Fertilize(Pawn Pawn)
		{
			if (implanter == null)
			{
				return;
			}
			if (xxx.ImmortalsIsActive && (Pawn.health.hediffSet.HasHediff(xxx.IH_Immortal) || pawn.health.hediffSet.HasHediff(xxx.IH_Immortal)))
			{
				if (RJWSettings.DevMode) ModLog.Message($"{xxx.get_pawnname(pawn)} is immortal no Fertilize egg {this} cancel");
				return;
			}
			if (AndroidsCompatibility.IsAndroid(pawn))
			{
				return;
			}

			if (!fertilized && canbefertilized && GestationProgress < 0.5)
			{
				if (RJWSettings.DevMode) ModLog.Message($"{xxx.get_pawnname(pawn)} had eggs inside fertilized (Fertilized by {xxx.get_pawnname(Pawn)}): {this}");
				father = Pawn;
				ChangeEgg(implanter);
			}

			// 若父母双方均为人类/都拥有基因，则设置后代的基因
			if (RJWPregnancySettings.egg_pregnancy_genes && father != null && implanter != null && father.genes != null && implanter.genes != null)
			{
				if (RJWSettings.DevMode) ModLog.Message($"Setting InsectEgg-Genes for {this}");
				DetermineChildGenes();
			} else
			{
				if (RJWSettings.DevMode) ModLog.Message($"Could not set Genes for {this}");
			}
		}

		/// <summary>
		/// 若植入者和受精者都拥有基因，则设置孩子的基因。
		/// 有意单独成一个方法，便于通过 Harmony 补丁访问。
		/// </summary>
		private void DetermineChildGenes()
		{
			eggGenes = PregnancyUtility.GetInheritedGenes(father, implanter);
			if (RJWSettings.DevMode) ModLog.Message($"Set Genes for {this} from {father} & {implanter} - total of {eggGenes.Count} genes set.");
		}

		/// <summary>
		/// 设置植入者，并由此确定基础蛋类型。
		/// 若该蛋为自体受精，则蛋也会被受精（同样以植入者为父亲）。
		/// </summary>
		/// <param name="pawn">要作为植入者的小人。</param>
		public void InitImplanter(Pawn pawn)
		{
			if (implanter == null)
			{
				if (RJWSettings.DevMode) ModLog.Message("Hediff_InsectEgg:: set implanter:" + xxx.get_pawnname(pawn));
				implanter = pawn;
				ChangeEgg(implanter);

				if (!implanter.health.hediffSet.HasHediff(xxx.sterilized))
				{
					if (HediffDefOfEgg.selffertilized)
						Fertilize(implanter);
				}
				else
					canbefertilized = false;
			}
		}

		//在植入/受精后更改蛋的类型
		public void ChangeEgg(Pawn Pawn)
		{
			if (Pawn != null)
			{
				float p_end_tick_mods = GetIncubationPeriod(Pawn);

				p_start_tick = Find.TickManager.TicksGame;
				p_end_tick = p_start_tick + p_end_tick_mods;
				lastTick = p_start_tick;

				eggssize = PregnancyHelper.CalculateEggSize(this.def as HediffDef_InsectEgg, Pawn);
				// 严重度为 0 时没有蛋的尺寸
				if (eggssize < 0.01f)
					eggssize = 0.01f;

				// 把卵巢中的蛋尺寸上限设为 10%
				if (this.Part.def == xxx.genitalsDef)
					if (eggssize > 0.1f && Genital_Helper.has_ovipositorF(pawn))
						if (Genital_Helper.has_ovipositorF(pawn))
							eggssize = 0.1f;

				Severity = eggssize;
			}
		}

		private float GetIncubationPeriod(Pawn implanter)
		{
			float incubationTicks;

			if (implanter.RaceProps?.gestationPeriodDays < 1f)
			{
				incubationTicks = GenDate.TicksPerDay;
			}
			else
			{
				incubationTicks = implanter.RaceProps.gestationPeriodDays * GenDate.TicksPerDay;
			}

			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("FertilityEnhancer")))
				incubationTicks /= 2f;

			incubationTicks *= RJWPregnancySettings.egg_pregnancy_duration;

			return incubationTicks;
		}

		//用于设置植入者/使蛋受精
		public bool IsParent(Pawn parent)
		{
			//任何人都可以让蛋受精
			if (RJWPregnancySettings.egg_pregnancy_fertilize_anyone) return true;

			//只有设定的蛋亲本或植入者才能受精
			else return parentDef == parent.kindDef.defName
						|| parentDefs.Contains(parent.kindDef.defName)
						|| implanter.kindDef == parent.kindDef; // 未知的蛋
		}

		public override string DebugString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.DebugString());
			stringBuilder.AppendLine(" Gestation progress: " + GestationProgress.ToStringPercent());
			if (RJWSettings.DevMode) stringBuilder.AppendLine(" Implanter: " + xxx.get_pawnname(implanter));
			if (RJWSettings.DevMode) stringBuilder.AppendLine(" Father: " + xxx.get_pawnname(father));
			return stringBuilder.ToString();
		}
	}
}
