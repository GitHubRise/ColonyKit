#nullable disable

using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Text;
using Multiplayer.API;
using System.Linq;
using RimWorld.Planet;
using static rjw.PregnancyHelper;

namespace rjw
{
	public abstract class Hediff_BasePregnancy : HediffWithComps
	//public abstract class Hediff_BasePregnancy : HediffWithParents
	{
		///<summary>
		///此状态类用于模拟怀孕。
		///</summary>	

		//静态字段
		private const int MiscarryCheckInterval = 1000;

		protected const string starvationMessage = "MessageMiscarriedStarvation";
		protected const string poorHealthMessage = "MessageMiscarriedPoorHealth";

		protected static readonly HashSet<string> non_genetic_traits = new HashSet<string>(DefDatabase<StringListDef>.GetNamed("NonInheritedTraits").strings);

		//字段
		///所有婴儿都应预先制作并存储在这里
		public List<Pawn> babies;
		///对父亲的引用，有时会为 null
		public Pawn father;
		///怀孕是否可见？
		public bool is_discovered = false;
		public bool is_parent_known = false;
		public bool ShouldMiscarry = false;
		public bool ImmortalMiscarry = false;
		///是否检查怀孕类型？
		public bool is_checked = false;
		///机械体怀孕，false - 生成敌对机械体
		public bool is_hacked = false;
		///宫缩持续时间，实际相当于额外的状态阶段，是一个让分娩过程更显著的脏技巧
		public int contractions;
		///妊娠进度
		public float p_start_tick = 0;
		public float p_end_tick = 0;
		public float lastTick = 0;
		public GeneSet geneSet = null;
		//public string last_name
		//{
		//	get
		//	{
		//		string last_name = "";
		//		if (xxx.is_human(father))
		//			last_name = NameTriple.FromString(father.Name.ToStringFull).Last;
		//		else if (xxx.is_human(pawn))
		//			last_name = NameTriple.FromString(pawn.Name.ToStringFull).Last;
		//		return last_name;
		//	}
		//}

		//public float skin_whiteness
		//{
		//	get
		//	{
		//		float skin_whiteness = Rand.Range(0, 1);
		//		if (xxx.has_traits(pawn) && pawn.RaceProps.Humanlike)
		//		{
		//			skin_whiteness = pawn.story.melanin;
		//		}
		//		if (father != null && xxx.has_traits(father) && father.RaceProps.Humanlike)
		//		{
		//			skin_whiteness = Rand.Range(skin_whiteness, father.story.melanin);
		//		}
		//		return skin_whiteness;
		//	}
		//}

		//public List<Trait> traitpool
		//{
		//	get
		//	{
		//		List<Trait> traitpool = new List<Trait>();
		//		if (xxx.has_traits(pawn) && pawn.RaceProps.Humanlike)
		//		{
		//			foreach (Trait momtrait in pawn.story.traits.allTraits)
		//			{
		//				if (!RJWPregnancySettings.trait_filtering_enabled || !non_genetic_traits.Contains(momtrait.def.defName))
		//					traitpool.Add(momtrait);
		//			}
		//		}
		//		if (father != null && xxx.has_traits(father) && father.RaceProps.Humanlike)
		//		{
		//			foreach (Trait poptrait in father.story.traits.allTraits)
		//			{
		//				if (!RJWPregnancySettings.trait_filtering_enabled || !non_genetic_traits.Contains(poptrait.def.defName))
		//					traitpool.Add(poptrait);
		//			}
		//		}
		//		return traitpool;
		//	}
		//}

		//
		// 属性
		//
		public float GestationProgress
		{
			get => Severity;
			private set => Severity = value;
		}

		private bool IsSeverelyWounded
		{
			get
			{
				float num = 0;
				List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
				foreach (Hediff h in hediffs)
				{
					//出于某种原因这会报错，
					//ModLog.Message(" 1 " + h.Part.IsCorePart);
					//ModLog.Message(" 2 " + h.Part.parent.IsCorePart);
					//if (h.Part.IsCorePart || h.Part.parent.IsCorePart)
					if (h is Hediff_Injury && (!h.IsPermanent() || !h.IsTended()))
					{
						if (h.Part != null)
							if (h.Part.IsCorePart || h.Part.parent.IsCorePart)
								num += h.Severity;
					}
				}
				List<Hediff_MissingPart> missingPartsCommonAncestors = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
				foreach (Hediff_MissingPart mp in missingPartsCommonAncestors)
				{
					if (mp.IsFreshNonSolidExtremity)
					{
						if (mp.Part != null)
							if (mp.Part.IsCorePart || mp.Part.parent.IsCorePart)
								num += mp.Part.def.GetMaxHealth(pawn);
					}
				}
				return num > 38 * pawn.RaceProps.baseHealthScale;
			}
		}

		/// <summary>
		/// 表示怀孕可以通过常规方式中止。
		/// </summary>
		public virtual bool canBeAborted
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// 表示怀孕可能会流产。
		/// </summary>
		public virtual bool canMiscarry
		{
			get
			{
				return true;
			}
		}

		public override string TipStringExtra
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(base.TipStringExtra);
				if(is_parent_known)
					stringBuilder.AppendLine("Father: " + xxx.get_pawnname(father));
				else
					stringBuilder.AppendLine("Father: " + "Unknown");

				return stringBuilder.ToString();
			}
		}

		/// <summary>
		/// 不要合并怀孕
		/// </summary>
		//public override bool TryMergeWith(Hediff other)
		//{
		//	return false;
		//}

		public override void PostMake()
		{
			// 确保该状态始终应用到躯干，不管指令是否正确
			base.PostMake();
			BodyPartRecord torso = pawn.RaceProps.body.AllParts.Find(x => x.def.defName == "Torso");
			if (Part != torso)
				Part = torso;

			//(debug->add heddif)
			//初始化空怀孕、立即分娩，会在分娩时导致错误
			//Initialize(pawn, Trytogetfather(ref pawn));
		}

		public override bool Visible => is_discovered;

		public virtual void DiscoverPregnancy()
		{
			is_discovered = true;
			if (PawnUtility.ShouldSendNotificationAbout(this.pawn) && (RJWPregnancySettings.animal_pregnancy_notifications_enabled || !this.pawn.IsAnimal()))
			{
				if (!is_checked)
				{
					string key1 = "RJW_PregnantTitle";
					string message_title = TranslatorFormattedStringExtensions.Translate(key1, pawn.LabelIndefinite()).CapitalizeFirst();
					string key2 = "RJW_PregnantText";

					string message_text = TranslatorFormattedStringExtensions.Translate(key2, pawn.LabelIndefinite()).CapitalizeFirst();
					Find.LetterStack.ReceiveLetter(message_title, message_text, LetterDefOf.NeutralEvent, pawn, null);

				}
				else
				{
					PregnancyMessage();
				}
			}
		}

		public virtual void CheckPregnancy()
		{
			is_checked = true;
			if (!is_discovered)
				DiscoverPregnancy();
			else
				PregnancyMessage();
		}

		public virtual void PregnancyMessage()
		{
			string key1 = "RJW_PregnantTitle";
			string message_title = TranslatorFormattedStringExtensions.Translate(key1, pawn.LabelIndefinite());
			string key2 = "RJW_PregnantNormal";
			string message_text = TranslatorFormattedStringExtensions.Translate(key2, pawn.LabelIndefinite());
			Find.LetterStack.ReceiveLetter(message_title, message_text, LetterDefOf.NeutralEvent, pawn, null);
		}

		// 快捷方法：按给定部位名称直接返回身体部位实例
		internal static BodyPartRecord GetPawnBodyPart(Pawn pawn, String bodyPart)
		{
			return pawn.RaceProps.body.AllParts.Find(x => x.def == DefDatabase<BodyPartDef>.GetNamed(bodyPart));
		}

		public virtual void Miscarry()
		{
			if (!babies.NullOrEmpty())
				foreach (Pawn baby in babies)
				{
					baby.Destroy();
					baby.Discard();
				}
			pawn.health?.RemoveHediff(this);
		}

		/// <summary>
		/// 在堕胎时调用（尚未实现）
		/// </summary>
		public virtual void Abort() 
		{
			Miscarry();
		}

		/// <summary>
		/// 机械体可以移除怀孕
		/// </summary>
		public virtual void Kill()
		{
			Miscarry();
		}

		[SyncMethod]
		public Pawn partstospawn(Pawn baby, Pawn mother, Pawn dad)
		{
			//决定要继承哪些部位
			//默认使用自己的部位
			Pawn partstospawn = baby;
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			//用母亲的部位生成
			if (mother != null && Rand.Range(0, 100) <= 10)
				partstospawn = mother;
			//用父亲的部位生成
			if (dad != null && Rand.Range(0, 100) <= 10)
				partstospawn = dad;

			//ModLog.Message(" Pregnancy partstospawn " + partstospawn);
			return partstospawn;
		}

		[SyncMethod]
		public bool spawnfutachild(Pawn baby, Pawn mother, Pawn dad)
		{
			int futachance = 0;
			if (mother != null && Genital_Helper.is_futa(mother))
				futachance = futachance + 25;
			if (dad != null && Genital_Helper.is_futa(dad))
				futachance = futachance + 25;

			//ModLog.Message(" Pregnancy spawnfutachild " + futachance);
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			//有 1% 的概率婴儿会是扶她
			//bug……还是……特性……自然总会找到出路
			return (Rand.Range(0, 100) <= futachance);
		}

		[SyncMethod]
		public static Pawn Trytogetfather(ref Pawn mother)
		{
			//用调试方式分娩时没有父亲
			//Postmake.initialize() 没有父亲
			ModLog.Warning("Hediff_BasePregnancy::Trytogetfather() - debug? no father defined, trying to add one");

			Pawn pawn = mother;
			Pawn father = null;

			//可能的父亲
			List<Pawn> partners = pawn.relations.RelatedPawns.Where(x =>
					Genital_Helper.has_penis_fertile(x) && !x.Dead &&
					(pawn.relations.DirectRelationExists(PawnRelationDefOf.Lover, x) ||
					pawn.relations.DirectRelationExists(PawnRelationDefOf.Fiance, x) ||
					pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, x))
					).ToList();

			//添加羁绊动物
			if (RJWSettings.bestiality_enabled && xxx.is_zoophile(mother))
				partners.AddRange(pawn.relations.RelatedPawns.Where(x =>
					Genital_Helper.has_penis_fertile(x) &&
					pawn.relations.DirectRelationExists(PawnRelationDefOf.Bond, x)).ToList());

			if (partners.Any())
			{
				father = partners.RandomElement();
				ModLog.Warning("Hediff_BasePregnancy::Trytogetfather() - father set to: " + xxx.get_pawnname(father));
			}

			if (father == null)
			{
				father = mother;
				ModLog.Warning("Hediff_BasePregnancy::Trytogetfather() - father is null, set to: " + xxx.get_pawnname(mother));
			}
			return father;
		}

		public override void Tick()
		{
			ageTicks++;
			float thisTick = Find.TickManager.TicksGame;
			GestationProgress = (1 + thisTick - p_start_tick) / (p_end_tick - p_start_tick);

			if ((thisTick - lastTick) >= 1000)
			{
				if (babies.NullOrEmpty())
				{
					ModLog.Warning(" no babies (debug?) " + this.GetType().Name);
					if (father == null)
					{
						father = Trytogetfather(ref pawn);
					}
					Initialize(pawn, father, SelectDnaGivingParent(pawn, father));
					GestationProgress = (1 + thisTick - p_start_tick) / (p_end_tick - p_start_tick);
					return;
				}
				lastTick = thisTick;
				if (canMiscarry)
				{
					//与不朽伴侣时流产
					if (ImmortalMiscarry)
					{
						if (RJWSettings.DevMode) ModLog.Message($"{this} parent(s) immortal, cancel pregnancy");
						Miscarry();
						return;
					}
					//ModLog.Message(" Pregnancy is ticking for " + pawn + " this is " + this.def.defName + " will end in " + 1/progress_per_tick/TicksPerDay + " days resulting in "+ babies[0].def.defName);
					if (!Find.Storyteller.difficulty.babiesAreHealthy)
					{
						//饥饿后流产
						if (pawn.needs.food != null && pawn.needs.food.CurCategory == HungerCategory.Starving)
						{
							var hed = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
							if (hed != null)
								if (hed.Severity > 0.4)
								{
									if (Visible && PawnUtility.ShouldSendNotificationAbout(pawn))
									{
										string text = "MessageMiscarriedStarvation".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
										Messages.Message(text, pawn, MessageTypeDefOf.NegativeHealthEvent);
									}
									Miscarry();
									return;
								}
						}
						//只在怀孕有一定进展时让殴打产生影响
						//在 SeverelyWounded 后流产
						if (Visible && (IsSeverelyWounded || ShouldMiscarry))
						{
							if (Visible && PawnUtility.ShouldSendNotificationAbout(pawn))
							{
								string text = "MessageMiscarriedPoorHealth".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
								Messages.Message(text, pawn, MessageTypeDefOf.NegativeHealthEvent);
							}
							Miscarry();
							return;
						}

					}
				}

				// 检查怀孕是否已足够明显，能在该体型上“显怀”
				if (!is_discovered)
				{
					BodyTypeDef bodyT = pawn?.story?.bodyType;
					//float threshold = 0f;

					if ((bodyT == BodyTypeDefOf.Thin && GestationProgress > 0.25f) ||
						(bodyT == BodyTypeDefOf.Female && GestationProgress > 0.35f) ||
						(GestationProgress > 0.50f)) // todo: 模组体型？（例如 FemaleBB）
						DiscoverPregnancy();

					//switch (bodyT)
					//{
					//case BodyType.Thin: threshold = 0.3f; break;
					//case BodyType.Female: threshold = 0.389f; break;
					//case BodyType.Male: threshold = 0.41f; break; 
					//default: threshold = 0.5f; break;
					//}
					//if (GestationProgress > threshold){ DiscoverPregnancy(); }
				}

				if (CurStageIndex == 3)
				{
					if (contractions == 0)
					{
						if (PawnUtility.ShouldSendNotificationAbout(pawn) && (RJWPregnancySettings.animal_pregnancy_notifications_enabled || !this.pawn.IsAnimal()))
						{
							string text = "RJW_Contractions".Translate(pawn.LabelIndefinite());
							Messages.Message(text, pawn, MessageTypeDefOf.NeutralEvent);
						}
						contractions++;
					}
					if (GestationProgress >= 1 && (pawn.CarriedBy == null || pawn.CarriedByCaravan()))//分娩需要一小时
					{
						if (PawnUtility.ShouldSendNotificationAbout(pawn) && (RJWPregnancySettings.animal_pregnancy_notifications_enabled || !this.pawn.IsAnimal()))
						{
							string message_title = "RJW_GaveBirthTitle".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
							string message_text = "RJW_GaveBirthText".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
							string pawn_possessive = pawn.gender.GetPossessive();
							string baby_text = ((babies.Count == 1) ? "RJW_ABaby".Translate(pawn_possessive) : "RJW_NBabies".Translate(babies.Count, pawn_possessive));
							message_text = message_text + baby_text;
							Find.LetterStack.ReceiveLetter(message_title, message_text, LetterDefOf.PositiveEvent, pawn);
						}
						GiveBirth();
					}
				}
			}
		}

		/// <summary>
		/// 若处于宫缩阶段则分娩，即使母亲已死亡
		/// </summary>
		public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
		{
			base.Notify_PawnDied(dinfo,culprit);
			if (CurStageIndex == 3)
				GiveBirth();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref father, "father", true);
			Scribe_Deep.Look(ref geneSet, "genes");
			Scribe_Values.Look(ref is_checked, "is_checked");
			Scribe_Values.Look(ref is_hacked, "is_hacked");
			Scribe_Values.Look(ref is_discovered, "is_discovered");
			Scribe_Values.Look(ref is_parent_known, "is_parent_known");
			Scribe_Values.Look(ref ShouldMiscarry, "ShouldMiscarry");
			Scribe_Values.Look(ref ImmortalMiscarry, "ImmortalMiscarry");
			Scribe_Values.Look(ref contractions, "contractions");
			Scribe_Collections.Look(ref babies, "babies", saveDestroyedThings: true, lookMode: LookMode.Deep, ctorArgs: new object[0]);
			Scribe_Values.Look(ref p_start_tick, "p_start_tick", 0);
			Scribe_Values.Look(ref p_end_tick, "p_end_tick", 0);
			Scribe_Values.Look(ref lastTick, "lastTick", 0);
		}

		/// <summary>
		/// 提供婴儿“DNA”的亲本。指示婴儿
		/// 应与母亲还是父亲同物种。
		/// </summary>
		public enum DnaGivingParent
		{
			Mother,
			Father
		}

		/// <summary>
		/// 决定婴儿应具有哪个亲本的物种。在某些配置下
		/// 此方法的结果是随机的！
		/// </summary>
		[SyncMethod]
		public static DnaGivingParent SelectDnaGivingParent(Pawn mother, Pawn father)
		{
			bool IsAndroidmother = AndroidsCompatibility.IsAndroid(mother);
			bool IsAndroidfather = AndroidsCompatibility.IsAndroid(father);
			//仿生人只能生出非仿生人
			if (IsAndroidmother && !IsAndroidfather)
			{
				return DnaGivingParent.Father;
			}
			else if (!IsAndroidmother && IsAndroidfather)
			{
				return DnaGivingParent.Mother;
			}
			else if (IsAndroidmother && IsAndroidfather)
			{
				ModLog.Warning("Both parents are andoids, what have you done monster!");
				//这永远不应发生，但无所谓
			}

			bool is_humanmother = xxx.is_human(mother);
			bool is_humanfather = xxx.is_human(father);
			//决定先继承哪个亲本
			if (father != null && RJWPregnancySettings.use_parent_method)
			{
				//ModLog.Message("The baby race needs definition");
				//类人性
				if (is_humanmother && is_humanfather)
				{
					//ModLog.Message("It's of two humanlikes");
					if (!Rand.Chance(RJWPregnancySettings.humanlike_DNA_from_mother))
					{
						//ModLog.Message("Mother will birth father race");
						return DnaGivingParent.Father;
					}
					else
					{
						//ModLog.Message("Mother will birth own race");
						return DnaGivingParent.Mother;
					}
				}
				//兽交
				else if (is_humanmother ^ is_humanfather)
				{
					//TODO：若 ModsConfig.BiotechActive 则修复来自母亲的基因继承（另见第 528 行）
					//if (RJWPregnancySettings.bestiality_DNA_inheritance == 0.0f || ModsConfig.BiotechActive)
					if (RJWPregnancySettings.bestiality_DNA_inheritance == 0.0f)
					{
						//ModLog.Message("mother will birth beast");
						if (is_humanmother)
							return DnaGivingParent.Father;
						else
							return DnaGivingParent.Mother;
					}
					else if (RJWPregnancySettings.bestiality_DNA_inheritance == 1.0f)
					{
						//ModLog.Message("mother will birth humanlike");
						if (is_humanmother)
							return DnaGivingParent.Mother;
						else
							return DnaGivingParent.Father;
					}
					else
					{
						if (!Rand.Chance(RJWPregnancySettings.bestial_DNA_from_mother))
						{
							//ModLog.Message("Mother will birth father race");
							return DnaGivingParent.Father;
						}
						else
						{
							//ModLog.Message("Mother will birth own race");
							return DnaGivingParent.Mother;
						}
					}

				}
				//兽性
				else
				{
					if (!Rand.Chance(RJWPregnancySettings.bestial_DNA_from_mother))
					{
						//ModLog.Message("Mother will birth father race");
						return DnaGivingParent.Father;
					}
					else
					{
						//ModLog.Message("Mother will birth own race");
						return DnaGivingParent.Mother;
					}
				}
			}
			else
			{
				return DnaGivingParent.Mother;
			}
		}

		//这应负责在到期时生成待出生的小人。应考虑所有设置以及父母双方的种族
		[SyncMethod]
		protected virtual void GenerateBabies(DnaGivingParent dnaGivingParent)
		{
			Pawn mother = pawn;
			//ModLog.Message("Generating babies for " + this.def.defName);
			if (mother == null)
			{
				ModLog.Error("Hediff_BasePregnancy::GenerateBabies() - no mother defined");
				return;
			}

			if (father == null)
			{
				ModLog.Error("Hediff_BasePregnancy::GenerateBabies() - no father defined");
				return;
			}

			//婴儿将拥有父母平均数量的特质，这样有望兼容各种修改特质数量上限的模组
			//int trait_count = 0;
			// 不再如此。改为以游戏原本生成的特质数量作为参考

			Pawn parent = (dnaGivingParent == DnaGivingParent.Father) ? father : mother;
			//ModLog.Message(" The main parent is " + parent);
			//ModLog.Message("Mother: " + xxx.get_pawnname(mother) + " kind: " + mother.kindDef);
			//ModLog.Message("Father: " + xxx.get_pawnname(father) + " kind: " + father.kindDef);
			//ModLog.Message("Baby base: " + xxx.get_pawnname(parent) + " kind: " + parent.kindDef);

			string last_name = "";
			if (xxx.is_human(father))
				last_name = NameTriple.FromString(father.Name.ToStringFull).Last;
			else if (xxx.is_human(pawn))
				last_name = NameTriple.FromString(pawn.Name.ToStringFull).Last;

			//float skin_whiteness = Rand.Range(0, 1);
			//if (xxx.has_traits(pawn) && pawn.RaceProps.Humanlike)
			//{
			//	skin_whiteness = pawn.story.melanin;
			//}
			//if (father != null && xxx.has_traits(father) && father.RaceProps.Humanlike)
			//{
			//	skin_whiteness = Rand.Range(skin_whiteness, father.story.melanin);
			//}

			List<Trait> traitpool = new List<Trait>();
			List<Trait> momtraits = new List<Trait>();
			List<Trait> poptraits = new List<Trait>();
			List<Trait> traits_to_inherit = new List<Trait>();
			System.Random rd = new System.Random();
			int rand_trait_index = 0;
			float max_num_momtraits_inherited = RJWPregnancySettings.max_num_momtraits_inherited;
            float max_num_poptraits_inherited = RJWPregnancySettings.max_num_poptraits_inherited;
			float max_num_traits_inherited = max_num_momtraits_inherited + max_num_poptraits_inherited;
			int i = 1;
			int j = 1;
			//创建 list 对象，用于存储父亲和母亲的特质
			if (xxx.has_traits(pawn) && pawn.RaceProps.Humanlike)
			{
				foreach (Trait momtrait in pawn.story.traits.allTraits)
				{
					if (!non_genetic_traits.Contains(momtrait.def.defName) && !momtrait.ScenForced)
						momtraits.Add(momtrait);
				}
			}
			if (father != null && xxx.has_traits(father) && father.RaceProps.Humanlike)
			{
				foreach (Trait poptrait in father.story.traits.allTraits)
				{
					if (!non_genetic_traits.Contains(poptrait.def.defName) && !poptrait.ScenForced)
						poptraits.Add(poptrait);
				}
			}
			//把父亲和母亲的特质加入待继承特质列表
			if(!momtraits.NullOrEmpty())
            {
				i = 1; 
				while (momtraits.Count > 0 && i <= max_num_momtraits_inherited)
				{
					rand_trait_index = rd.Next(0, momtraits.Count);
					traits_to_inherit.Add(momtraits[rand_trait_index]);
					momtraits.RemoveAt(rand_trait_index);
                }
            }
			if(!poptraits.NullOrEmpty())
            {
				j = 1;
				while (poptraits.Count > 0 && j <= max_num_poptraits_inherited)
				{
					rand_trait_index = rd.Next(0, poptraits.Count);
					traits_to_inherit.Add(poptraits[rand_trait_index]);
					poptraits.RemoveAt(rand_trait_index);
				}
			}

			//如果只有母方特质、甚至没有母方特质需要考虑，就无需检查父母间的重复特质
			//此后 traits_to_inherit 列表即可转入 traitpool
			if (poptraits.NullOrEmpty() || momtraits.NullOrEmpty())
            {
				foreach(Trait traits in traits_to_inherit)
                {
					traitpool.Add(traits);
                }
            }
			//如果父母双方都有特质，则需要检查父母之间是否存在重复特质
			else
            {
			//如果 traits_to_inherit 的长度等于特质数量上限，就说明父母双方的特质已达上限且没有重复，此列表可直接使用
			//那么后面的大段 if 代码块不会执行，会直接跳到最后一个 foreach 代码块
				if (traits_to_inherit.Count() != max_num_traits_inherited)
				{
			//若不相等，可能是因为去除了重复元素，或父方或母方的特质数少于可从其继承的特质上限
			//先确认 momtraits 不为 null，再检查母方所有特质是否都已加入婴儿的 traitpool，以及母方特质数是否达到上限
					if (momtraits.Count != 0 && i != max_num_momtraits_inherited)
					{
						while (poptraits != null && momtraits.Count > 0 && i <= max_num_momtraits_inherited)
						{
							rand_trait_index = rd.Next(0, momtraits.Count);
							//如果新选中的特质与 traits_to_inherit 中已有的特质不重复
							if (!traits_to_inherit.Contains(momtraits[rand_trait_index]))
							{
								traits_to_inherit.Add(momtraits[rand_trait_index]);
							}
							momtraits.RemoveAt(rand_trait_index);
						}
					}
			//按处理 momtraits 的同样方式处理 poptraits
					if (poptraits != null && poptraits.Count != 0 && j != max_num_poptraits_inherited)
					{
						while (poptraits.Count > 0 && i <= max_num_poptraits_inherited)
						{
							rand_trait_index = rd.Next(0, poptraits.Count);
							//如果新选中的特质与 traits_to_inherit 中已有的特质不重复
							if (!traits_to_inherit.Contains(poptraits[rand_trait_index]))
							{
								traits_to_inherit.Add(poptraits[rand_trait_index]);
							}
							poptraits.RemoveAt(rand_trait_index);
						}
					}
				}
			//把最终确定的 trait_to_inherit 列表中的所有特质加入 traitpool 列表，以便后续处理
			//如果上面的 if 代码块已执行，下面的 foreach 代码块仍需执行，因此不必把这个 foreach 代码块放进 else 代码块
				foreach (Trait traits in traits_to_inherit)
				{
					traitpool.Add(traits);
				}
			}
			

			//小人生成请求
			PawnKindDef spawn_kind_def = parent.kindDef;
			Faction spawn_faction = mother.IsPrisoner ? null : mother.Faction;

			//ModLog.Message(" default child spawn_kind_def - " + spawn_kind_def);
			string MotherRaceName = "";
			string FatherRaceName = "";
			MotherRaceName = mother.kindDef.race.defName;
			if (father != null)
				FatherRaceName = father.kindDef.race.defName;
			//ModLog.Message(" MotherRaceName is " + MotherRaceName);
			//ModLog.Message(" FatherRaceName is " + FatherRaceName);
			if (MotherRaceName != FatherRaceName && FatherRaceName != "")
			{
				var groups = DefDatabase<RaceGroupDef>.AllDefs.Where(x => !x.hybridRaceParents.NullOrEmpty() && !x.hybridChildKindDef.NullOrEmpty());
				//ModLog.Message(" found custom RaceGroupDefs " + groups.Count());
				foreach (var t in groups)
				{
					//ModLog.Message(" trying custom RaceGroupDef " + t.defName);
					//ModLog.Message(" custom hybridRaceParents " + t.hybridRaceParents.Count());
					//ModLog.Message(" contains hybridRaceParents MotherRaceName? " + t.hybridRaceParents.Contains(MotherRaceName));
					//ModLog.Message(" contains hybridRaceParents FatherRaceName? " + t.hybridRaceParents.Contains(FatherRaceName));
					if ((t.hybridRaceParents.Contains(MotherRaceName) && t.hybridRaceParents.Contains(FatherRaceName))
						|| (t.hybridRaceParents.Contains("Any") && (t.hybridRaceParents.Contains(MotherRaceName) || t.hybridRaceParents.Contains(FatherRaceName))))
					{
						//ModLog.Message(" has hybridRaceParents");
						if (t.hybridChildKindDef.Contains("MotherKindDef"))
							spawn_kind_def = mother.kindDef;
						else if (t.hybridChildKindDef.Contains("FatherKindDef") && father != null)
							spawn_kind_def = father.kindDef;
						else
						{
							//ModLog.Message(" trying hybridChildKindDef " + t.defName);
							var child_kind_def_list = new List<PawnKindDef>();
							child_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => t.hybridChildKindDef.Contains(x.defName)));

							//ModLog.Message(" found custom hybridChildKindDefs " + t.hybridChildKindDef.Count);
							if (!child_kind_def_list.NullOrEmpty())
								spawn_kind_def = child_kind_def_list.RandomElement();
						}
					}
				}
			}

			if (spawn_kind_def.defName.Contains("Nymph"))
			{
				//孩子是花痴，尝试寻找其他 PawnKindDef
				var spawn_kind_def_list = new List<PawnKindDef>();
				spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == spawn_kind_def.race && !x.defName.Contains("Nymph")));
				//未找到其他 PawnKindDef，尝试用母亲
				if (spawn_kind_def_list.NullOrEmpty())
					spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == mother.kindDef.race && !x.defName.Contains("Nymph")));
				//未找到其他 PawnKindDef，尝试用父亲
				if (spawn_kind_def_list.NullOrEmpty() && father !=null)
					spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == father.kindDef.race && !x.defName.Contains("Nymph")));
				//未找到其他 PawnKindDef，回退到通用殖民者
				if (spawn_kind_def_list.NullOrEmpty())
					spawn_kind_def = PawnKindDefOf.Colonist;

				spawn_kind_def = spawn_kind_def_list.RandomElement();
			}
			//ModLog.Message(" final child spawn_kind_def - " + spawn_kind_def);

			//与史莱姆的怀孕只会生出史莱姆
			//应以某种方式与上面的代码合并
			if (xxx.is_slime(mother))
			{
				parent = mother;
				spawn_kind_def = parent.kindDef;
			}
			if (father != null)
			{
				if (xxx.is_slime(father))
				{
					parent = father;
					spawn_kind_def = parent.kindDef;
				}
			}

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
				//fixedMelanin: skin_whiteness,
				fixedLastName: last_name,
				//forceNoIdeo: true,
				forbidAnyTitle: true,
				forceNoBackstory: true,
				forcedEndogenes: PregnancyUtility.GetInheritedGenes(father, mother)
				);

			//ModLog.Message(" Generated request, making babies");
			//每胎数量。使用主要亲本的每胎数量，并按生育力削减。
			float litter_size = (parent.RaceProps.litterSizeCurve == null) ? 1 : Rand.ByCurve(parent.RaceProps.litterSizeCurve);
			//ModLog.Message(" base Litter size " + litter_size);
			litter_size *= Math.Min(mother.health.capacities.GetLevel(xxx.reproduction), 1);
			litter_size *= Math.Min(father == null ? 1 : father.health.capacities.GetLevel(xxx.reproduction), 1);
			litter_size = Math.Max(1, litter_size);
			//ModLog.Message(" Litter size (w fertility) " + litter_size);

			float max_litter = GetMaxLitterSize(spawn_kind_def);

			//在每胎数量/上限范围内随机生成婴儿数量
			litter_size = (Rand.Range(litter_size, max_litter));
			//ModLog.Message(" Litter size roll 1:" + litter_size);
			litter_size = Mathf.RoundToInt(litter_size);
			//ModLog.Message(" Litter size roll 2:" + litter_size);
			litter_size = Math.Max(1, litter_size);
			//ModLog.Message(" final Litter size " + litter_size);

			for (i = 0; i < litter_size; i++)
			{
				Pawn baby = PawnGenerator.GeneratePawn(request);

				if (xxx.is_human(baby))
				{
					if (mother.IsSlaveOfColony)
					{
						//ModLog.Message("mother.SlaveFaction " + mother.SlaveFaction);
						//ModLog.Message("mother.HomeFaction " + mother.HomeFaction);
						//ModLog.Message("mother.Faction " + mother.Faction);

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
						//ModLog.Message("mother.HomeFaction " + mother.HomeFaction);
						if (mother.HomeFaction != null)
							baby.SetFaction(mother.HomeFaction);
						baby.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
					}

					//选择要赋予孩子的特质。与 CnP 不同，这里允许出现一些随机特质
					if (traitpool.Count > 0)
					{
						updateTraits(baby, traitpool);
					}
				}
				babies.Add(baby);
			}
		}

		private float GetMaxLitterSize(PawnKindDef childKindDef)
		{
			//婴儿体型与母亲体型之比（多胎怀孕有额外的子女加成，人类约 1.7，Orassan 约 2.2 倍）
			//假设母亲的腹部占其体型的 1/3
			float babySize = childKindDef.RaceProps.lifeStageAges[0].def.bodySizeFactor; // 成年体型/5，大概吧？
			babySize *= childKindDef.RaceProps.baseBodySize; // 成年体型
			//ModLog.Message(" Baby size " + babySize);
			float maxLitterSize = 1f / 3f / babySize;
			//ModLog.Message(" Max size " + maxLitterSize);
			return maxLitterSize;
		}

		/// <summary>
		/// 更新小人的特质
		/// 以小人的原有特质和给定的特质列表作为选择特质的来源。
		/// </summary>
		/// <param name="pawn">类人小人</param>
		/// <param name="parenttraitpool">父母特质列表</param>
		/// <param name="traitLimit">允许的特质数量上限</param>
		[SyncMethod]
		void updateTraits(Pawn pawn, List<Trait> parenttraitpool, int traitLimit = -1)
		{
			if (pawn?.story?.traits == null)
			{
				return;
			}

			if (traitLimit == -1)
			{
				traitLimit = pawn.story.traits.allTraits.Count;
			}

			//个人特质池
			List<Trait> personalTraitPool = new List<Trait>(pawn.story.traits.allTraits);
			//父母特质池
			if (parenttraitpool != null)
			{
				personalTraitPool.AddRange(parenttraitpool);
			}

			//游戏建议的特质。
			var forcedTraits = personalTraitPool
				.Where(x => x.ScenForced)
				.Distinct(new TraitComparer(ignoreDegree: true)); // 结果可能很混乱，因为游戏允许在剧本编辑器中造出这种混乱

			List<Trait> selectedTraits = new List<Trait>();
			selectedTraits.AddRange(forcedTraits); // 强制执行剧本规定的特质

			var comparer = new TraitComparer(); // 特质比较的实现，因为缺少它时游戏会按引用比较特质，把它们全都当成唯一的。

			while (selectedTraits.Count < traitLimit && personalTraitPool.Count > 0)
			{
				int index = Rand.Range(0, personalTraitPool.Count); // 取出特质并从池中移除
				var trait = personalTraitPool[index];
				personalTraitPool.RemoveAt(index);

				if (!selectedTraits.Any(x => comparer.Equals(x, trait) ||  // 跳过与已添加特质冲突的特质
											 x.def.ConflictsWith(trait)))
				{
					selectedTraits.Add(new Trait(trait.def, trait.Degree, false));
				}
			}

			pawn.story.traits.allTraits = selectedTraits;
		}

		//处理小人的生成
		//此类由其他脚本扩展
		public abstract void GiveBirth();

		//添加关系
		//怀孕后的效果
		//更新出生统计
		//需要时把婴儿设为扶他
		public virtual void PostBirth(Pawn mother, Pawn father, Pawn baby)
		{
			BabyPostBirth(mother, father, baby);

			if (baby.playerSettings != null && mother.playerSettings != null)
			{
				baby.playerSettings.AreaRestrictionInPawnCurrentMap = mother.playerSettings.AreaRestrictionInPawnCurrentMap; //还是这里该用实际生效的那一方？
			}

			//if (xxx.is_human(baby))
			//{
			//	baby.story.melanin = skin_whiteness;
			//	baby.story.birthLastName = last_name;
			//}

			//ModLog.Message("" + this.GetType().ToString() + " post PostBirth 1: " + baby.story.birthLastName);

			//这个大概只对旧版部件、旧版 sexualizer 和部件添加器有效，或许可以更新/修复，但随便吧
			//生成扶他
			//bool isfuta = spawnfutachild(baby, mother, father);

			//if (!RacePartDef_Helper.TryRacePartDef_partAdders(baby))
			//{
			//	RemoveBabyParts(baby, Genital_Helper.get_AllPartsHediffList(baby));
			//	if (isfuta)
			//	{
			//		SexPartAdder.add_genitals(baby, partstospawn(baby, mother, father), Gender.Male);
			//		SexPartAdder.add_genitals(baby, partstospawn(baby, mother, father), Gender.Female);
			//		SexPartAdder.add_breasts(baby, partstospawn(baby, mother, father), Gender.Female);
			//		baby.gender = Gender.Female;                //为扶他设为女性，婴儿已连同关系等一起生成，应该不会报错
			//	}
			//	else
			//	{
			//		SexPartAdder.add_genitals(baby, partstospawn(baby, mother, father));
			//		SexPartAdder.add_breasts(baby, partstospawn(baby, mother, father));
			//	}
			//	SexPartAdder.add_anus(baby, partstospawn(baby, mother, father));
			//}

			//ModLog.Message("" + this.GetType().ToString() + " post PostBirth 2: " + baby.story.birthLastName);
			if (mother.Spawned)
			{
				// 把婴儿移到母亲前方，而不是放在其身上
				if (mother.CurrentBed() != null)
				{
					baby.Position = baby.Position + new IntVec3(0, 0, 1).RotatedBy(mother.CurrentBed().Rotation);
				}
				// 生成黏液
				FilthMaker.TryMakeFilth(mother.Position, mother.Map, ThingDefOf.Filth_AmnioticFluid, mother.LabelIndefinite(), 5);
				mother.caller?.DoCall();
				baby.caller?.DoCall();
				father?.caller?.DoCall();
			}

			//ModLog.Message("" + this.GetType().ToString() + " post PostBirth 3: " + baby.story.birthLastName);
			if (xxx.is_human(baby))
				mother.records.AddTo(xxx.CountOfBirthHuman, 1);
			if (xxx.is_animal(baby))
				mother.records.AddTo(xxx.CountOfBirthAnimal, 1);
		}

		public static void RemoveBabyParts(Pawn baby, List<Hediff> list)
		{
			//ModLog.Message(" RemoveBabyParts( " + xxx.get_pawnname(baby) + " ) - " + list.Count);
			if (!list.NullOrEmpty())
				foreach (var x in list)
				{
					//ModLog.Message(" RemoveBabyParts( " + xxx.get_pawnname(baby) + " ) - " + x.def.defName);

					baby.health.RemoveHediff(x);
					//baby.health.RemoveHediff(baby.health.hediffSet.hediffs.Remove(x));
				}
		}
		[SyncMethod]
		public static void BabyPostBirth(Pawn mother, Pawn father, Pawn baby)
		{
			if (!xxx.is_human(baby)) return;

			baby.story.Childhood = null;
			baby.story.Adulthood = null;

			try
			{
				// 把孩子设为平民
				String bsDef = null;
				bool isTribal = false;

				// 若父母双方均为部落民，则把孩子设为部落民
				if (father != null && father.story != null)
				{
					if (mother.story.GetBackstory(BackstorySlot.Adulthood) != null && father.story.GetBackstory(BackstorySlot.Adulthood).spawnCategories.Contains("Tribal"))
						isTribal = true;
					else if (mother.story.GetBackstory(BackstorySlot.Adulthood) == null && father.story.GetBackstory(BackstorySlot.Childhood).spawnCategories.Contains("Tribal"))
						isTribal = true;
				}
				else
				{
					//(int)Find.GameInitData.playerFaction.def.techLevel < 4;
					if ((int)mother.Faction.def.techLevel < 4)
						isTribal = true;
				}

				if (isTribal)
				{
					bsDef = "rjw_childT";
					if (baby.GetRJWPawnData().RaceSupportDef != null)
						if (!baby.GetRJWPawnData().RaceSupportDef.backstoryChildTribal.NullOrEmpty())
							bsDef = baby.GetRJWPawnData().RaceSupportDef.backstoryChildTribal.RandomElement();
				}
				else
				{
					bsDef = "rjw_childC";
					if (baby.GetRJWPawnData().RaceSupportDef != null)
						if (!baby.GetRJWPawnData().RaceSupportDef.backstoryChildCivil.NullOrEmpty())
							bsDef = baby.GetRJWPawnData().RaceSupportDef.backstoryChildCivil.RandomElement();
				}

				baby.story.Childhood = DefDatabase<BackstoryDef>.AllDefsListForReading.FirstOrDefault(x => x.defName.Contains(bsDef));
			}
			catch (Exception e)
			{
				ModLog.Warning(e.ToString());
			}
		}

		protected void InitializeIfNoBabies()
		{
			if (babies.NullOrEmpty())
			{
				ModLog.Warning(" no babies (debug?) " + this.GetType().Name);
				if (father == null)
				{
					father = Trytogetfather(ref pawn);
				}
				Initialize(pawn, father, SelectDnaGivingParent(pawn, father));
			}
		}

		//此方法承担构造函数的工作，因为状态是通过 HediffMaker 而非常规面向对象方式创建的
		//这段逻辑不能放在 PostMake() 中，因为那时还没有父亲。
		public void Initialize(Pawn mother, Pawn dad, DnaGivingParent dnaGivingParent)
		{
			father = dad;
			//ModLog.Message("" + this.GetType().ToString() + " pregnancy hediff generated: " + this.Label);
			//ModLog.Message("" + this.GetType().ToString() + " mother: " + mother + " father: " + dad);

			BodyPartRecord torso = mother.RaceProps.body.AllParts.Find(x => x.def.defName == "Torso");
			mother.health.AddHediff(this, torso);
			if (father != null)
			{
				babies = new List<Pawn>();
				contractions = 0;
				//ModLog.Message("" + this.GetType().ToString() + " generating babies before: " + this.babies.Count);
				GenerateBabies(dnaGivingParent);
				//if (ModsConfig.BiotechActive)
				//	geneSet = PregnancyUtility.GetInheritedGeneSet(father, mother);
				//	SetParents(mother, father, PregnancyUtility.GetInheritedGeneSet(father, mother));
			}

			float gestationPeriod = GetGestationPeriod();
			p_start_tick = Find.TickManager.TicksGame;
			p_end_tick = p_start_tick + gestationPeriod;
			lastTick = p_start_tick;

			if (xxx.ImmortalsIsActive && (mother.health.hediffSet.HasHediff(xxx.IH_Immortal) || father.health.hediffSet.HasHediff(xxx.IH_Immortal)))
			{
				ImmortalMiscarry = true;
				ShouldMiscarry = true;
			}
			//ModLog.Message("" + this.GetType().ToString() + " generating babies after: " + this.babies.Count);
		}

		private float GetGestationPeriod()
		{
			float gestationPeriod;
			if (babies[0].RaceProps?.gestationPeriodDays < 1)
			{
				if (xxx.is_human(babies[0]))
					gestationPeriod = 45 * GenDate.TicksPerDay; //默认人类
				else
					gestationPeriod = GenDate.TicksPerDay;
			}
			else
			{
				gestationPeriod = babies[0].RaceProps.gestationPeriodDays * GenDate.TicksPerDay;
			}

			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("FertilityEnhancer")))
				gestationPeriod /= 1.25f;

			gestationPeriod *= RJWPregnancySettings.normal_pregnancy_duration;
			return gestationPeriod;
		}

		private static Dictionary<Type, string> _hediffOfClass = null;
		protected static Dictionary<Type, string> hediffOfClass
		{
			get
			{
				if (_hediffOfClass == null)
				{
					_hediffOfClass = new Dictionary<Type, string>();
					var allRJWPregnancies = GenTypes.AllSubclasses(typeof(Hediff_BasePregnancy));

					foreach (var pregClass in allRJWPregnancies)
					{
						var attribute = (RJWAssociatedHediffAttribute)pregClass.GetCustomAttributes(typeof(RJWAssociatedHediffAttribute), false).FirstOrDefault();
						if (attribute != null)
						{
							_hediffOfClass[pregClass] = attribute.defName;
						}
					}
				}

				return _hediffOfClass;
			}
		}


		public static T Create<T>(Pawn mother, Pawn father) where T : Hediff_BasePregnancy
		{
			return Create<T>(mother, father, SelectDnaGivingParent(mother, father));
		}

		/// <summary>
		/// 创建怀孕状态并将其赋予母亲 
		/// </summary>
		/// <typeparam name="T">怀孕类型，应为 Hediff_BasePregnancy 的子类</typeparam>
		/// <param name="mother"></param>
		/// <param name="father"></param>
		/// <param name="dnaGivingParent">提供婴儿“DNA”的亲本。</param>
		/// <returns>创建的状态</returns>
		public static T Create<T>(Pawn mother, Pawn father, DnaGivingParent dnaGivingParent) where T : Hediff_BasePregnancy
		{
			if (mother == null)
				return null;
			//if (mother.RaceHasOviPregnancy() && !(T is Hediff_MechanoidPregnancy))
			//{
			//	//return null;
			//}
			//else
			//{
			//}

			BodyPartRecord torso = mother.RaceProps.body.AllParts.Find(x => x.def.defName == "Torso");

			string defName = hediffOfClass.ContainsKey(typeof(T)) ? hediffOfClass[typeof(T)] : "RJW_pregnancy";
			if (RJWSettings.DevMode) ModLog.Message($"Hediff_BasePregnancy::create hediff:{defName} class:{typeof(T).FullName}");

			T hediff = HediffHelper.MakeHediff<T>(HediffDef.Named(defName), mother, torso);
			hediff.Initialize(mother, father, dnaGivingParent);
			return hediff;
		}

		/// <summary>
		/// 所有已知 RJW 怀孕状态名称的列表（模组可以注册新的名称）
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> KnownPregnancies()
		{
			return hediffOfClass.Values.Distinct(); // todo：性能
		}

		public override string DebugString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.DebugString());
			stringBuilder.AppendLine("Gestation progress: " + GestationProgress.ToStringPercent());
			//stringBuilder.AppendLine("Time left: " + ((int)((1f - GestationProgress) * babies[babies.Count-1].RaceProps.gestationPeriodDays * TicksPerDay * RJWPregnancySettings.normal_pregnancy_duration)).ToStringTicksToPeriod());
			return stringBuilder.ToString();
		}
		//public override IEnumerable<Gizmo> GetGizmos()
		//{
		//	foreach (Gizmo gizmo in base.GetGizmos())
		//	{
		//		yield return gizmo;
		//	}
		//	if (!DebugSettings.ShowDevGizmos)
		//	{
		//		yield break;
		//	}
		//	if (CurStageIndex < 2)
		//	{
		//		Command_Action command_Action = new Command_Action();
		//		command_Action.defaultLabel = "DEV: Next trimester";
		//		command_Action.action = delegate
		//		{
		//			HediffStage hediffStage = def.stages[CurStageIndex + 1];
		//			severityInt = hediffStage.minSeverity;
		//		};
		//		yield return command_Action;
		//	}
		//	if (ModsConfig.BiotechActive && pawn.RaceProps.Humanlike && pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor) == null)
		//	{
		//		Command_Action command_Action2 = new Command_Action();
		//		command_Action2.defaultLabel = "DEV: Start Labor";
		//		command_Action2.action = delegate
		//		{
		//			StartLabor();
		//			pawn.health.RemoveHediff(this);
		//		};
		//		yield return command_Action2;
		//	}
		//}
		//public void StartLabor()
		//{
		//	if (ModLister.CheckBiotech("labor"))
		//	{
		//		((Hediff_Labor)pawn.health.AddHediff(HediffDefOf.PregnancyLabor)).SetParents(pawn, father, PregnancyUtility.GetInheritedGeneSet(father, pawn));
		//		Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.MorningSickness);
		//		if (firstHediffOfDef != null)
		//		{
		//			pawn.health.RemoveHediff(firstHediffOfDef);
		//		}
		//		Hediff firstHediffOfDef2 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyMood);
		//		if (firstHediffOfDef2 != null)
		//		{
		//			pawn.health.RemoveHediff(firstHediffOfDef2);
		//		}
		//		if (PawnUtility.ShouldSendNotificationAbout(pawn))
		//		{
		//			Find.LetterStack.ReceiveLetter("LetterColonistPregnancyLaborLabel".Translate(pawn), "LetterColonistPregnancyLabor".Translate(pawn), LetterDefOf.NeutralEvent);
		//		}
		//	}
		//}
	}
}
