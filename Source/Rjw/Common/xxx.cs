#nullable disable

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Multiplayer.API;
using Prepatcher;
using RimWorld;
using Verse;

namespace rjw
{
	public static class xxx
	{
		public static readonly BindingFlags ins_public_or_no = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		public static readonly config config = DefDatabase<config>.GetNamed("the_one");

		/// <summary>
		/// 在几十个文件中使用的硬编码魔法值，放在这里和放在别处一样糟糕 <br />
		/// 至少应当归入相关的标记类型中
		/// </summary>
		public static readonly int max_rapists_per_prisoner = 6;

		/// <summary>
		/// 同一场性行为允许的发起者数量。
		/// 群交只对人类之间生效，动物/机械保持一对一；返回值已按 1–8 限制。
		/// </summary>
		public static int max_sex_partners_for(Pawn initiator, Pawn target)
		{
			if (initiator == null || target == null) return 1;
			if (!is_human(initiator) || !is_human(target)) return 1;
			return RJWPreferenceSettings.MaxSexPartners;
		}

		//CombatExtended
		public static bool CombatExtendedIsActive;
		public static HediffDef MuscleSpasms;

		//RomanceDiversified
		public static bool RomanceDiversifiedIsActive; //检查模组是否启用的取巧做法
		public static TraitDef straight;
		public static TraitDef faithful;
		public static TraitDef philanderer;
		public static TraitDef polyamorous;

		//Psychology
		public static bool PsychologyIsActive;
		public static TraitDef prude;
		public static TraitDef lecher;
		public static TraitDef polygamous;

		//Rimpsyche
		public static bool RimpsycheSexualityIsActive;

		//Other poly mods
		public static bool AnyPolyamoryModIsActive;
		public static bool AnyPolygamyModIsActive;

		//[SYR] Individuality
		public static bool IndividualityIsActive;
		public static TraitDef SYR_CreativeThinker;
		public static TraitDef SYR_Haggler;

		//Rimworld of Magic
		public static bool RoMIsActive;
		public static TraitDef Succubus;
		public static TraitDef Warlock;
		public static NeedDef TM_Mana;
		public static HediffDef TM_ShapeshiftHD;

		//Nightmare Incarnation
		public static bool NightmareIncarnationIsActive;
		public static NeedDef NI_Need_Mana;

		//Consolidated
		public static bool CTIsActive;
		public static TraitDef RCT_NeatFreak;
		public static TraitDef RCT_Savant;
		public static TraitDef RCT_Inventor;
		public static TraitDef RCT_AnimalLover;

		//SimpleSlavery
		public static bool SimpleSlaveryIsActive;
		public static HediffDef Enslaved;

		//Dubs Bad Hygiene
		public static bool DubsBadHygieneIsActive;
		public static NeedDef DBHThirst;

		//Alien Framework
		public static bool AlienFrameworkIsActive;
		public static TraitDef xenophobia; // 度数：1：排外，-1：亲外

		//Immortals
		public static bool ImmortalsIsActive;
		public static HediffDef IH_Immortal;

		//Character Editor
		public static bool CharacterEditorActive = HarmonyLib.AccessTools.TypeByName("CharacterEditor.TraitTool") != null;

		//RJW-EX
		public static bool RjwExIsActive;
		public static HediffDef RjwEx_Armbinder;

		//RJW Children
		public static HediffDef RJW_BabyState = DefDatabase<HediffDef>.GetNamed("RJW_BabyState");
		public static HediffDef RJW_NoManipulationFlag = DefDatabase<HediffDef>.GetNamed("RJW_NoManipulationFlag");

		//rjw
		public static readonly TraitDef nymphomaniac = DefDatabase<TraitDef>.GetNamed("Nymphomaniac");
		public static readonly TraitDef rapist = DefDatabase<TraitDef>.GetNamed("Rapist");
		public static readonly TraitDef masochist = DefDatabase<TraitDef>.GetNamed("Masochist");
		public static readonly TraitDef necrophiliac = DefDatabase<TraitDef>.GetNamed("Necrophiliac");
		public static readonly TraitDef zoophile = DefDatabase<TraitDef>.GetNamed("Zoophile");

		//用于阻止繁殖的状态
		public static readonly HediffDef sterilized = DefDatabase<HediffDef>.GetNamed("Sterilized");

		//用于身体破损的状态（因作为 CP 被强暴太多次所致）
		public static readonly HediffDef feelingBroken = DefDatabase<HediffDef>.GetNamed("FeelingBroken");

		public static readonly HediffDef submitting = DefDatabase<HediffDef>.GetNamed("Hediff_Submitting");
		public static readonly HediffDef humanlikeParaphilia = DefDatabase<HediffDef>.GetNamed("Humanlike_Paraphilia");

		public static PawnCapacityDef reproduction = DefDatabase<PawnCapacityDef>.GetNamed("RJW_Fertility");

		//rjw 身体部位
		//活体生物
		public static readonly BodyPartDef genitalsDef = DefDatabase<BodyPartDef>.GetNamed("Genitals");
		public static readonly BodyPartDef breastsDef = DefDatabase<BodyPartDef>.GetNamed("Chest");
		public static readonly BodyPartDef anusDef = DefDatabase<BodyPartDef>.GetNamed("Anus");
		//机械族
		public static readonly BodyPartDef mechgenitalsDef = DefDatabase<BodyPartDef>.GetNamed("MechGenitals");
		public static readonly BodyPartDef mechbreastsDef = DefDatabase<BodyPartDef>.GetNamed("MechChest");
		public static readonly BodyPartDef mechanusDef = DefDatabase<BodyPartDef>.GetNamed("MechAnus");

		//性事后想法
		public static readonly ThoughtDef got_raped = DefDatabase<ThoughtDef>.GetNamed("GotRaped");
		public static readonly ThoughtDef got_anal_raped = DefDatabase<ThoughtDef>.GetNamed("GotAnalRaped");
		public static readonly ThoughtDef got_anal_raped_byfemale = DefDatabase<ThoughtDef>.GetNamed("GotAnalRapedByFemale");
		public static readonly ThoughtDef got_raped_unconscious = DefDatabase<ThoughtDef>.GetNamed("GotRapedUnconscious");
		public static readonly ThoughtDef masochist_got_raped_unconscious = DefDatabase<ThoughtDef>.GetNamed("MasochistGotRapedUnconscious");

		public static readonly ThoughtDef got_bred = DefDatabase<ThoughtDef>.GetNamed("GotBredByAnimal");
		public static readonly ThoughtDef got_anal_bred = DefDatabase<ThoughtDef>.GetNamed("GotAnalBredByAnimal");
		public static readonly ThoughtDef got_licked = DefDatabase<ThoughtDef>.GetNamed("GotLickedByAnimal");
		public static readonly ThoughtDef got_groped = DefDatabase<ThoughtDef>.GetNamed("GotGropedByAnimal");

		public static readonly ThoughtDef masochist_got_raped = DefDatabase<ThoughtDef>.GetNamed("MasochistGotRaped");
		public static readonly ThoughtDef masochist_got_anal_raped = DefDatabase<ThoughtDef>.GetNamed("MasochistGotAnalRaped");
		public static readonly ThoughtDef masochist_got_anal_raped_byfemale = DefDatabase<ThoughtDef>.GetNamed("MasochistGotAnalRapedByFemale");
		public static readonly ThoughtDef masochist_got_bred = DefDatabase<ThoughtDef>.GetNamed("MasochistGotBredByAnimal");
		public static readonly ThoughtDef masochist_got_anal_bred = DefDatabase<ThoughtDef>.GetNamed("MasochistGotAnalBredByAnimal");
		public static readonly ThoughtDef masochist_got_licked = DefDatabase<ThoughtDef>.GetNamed("MasochistGotLickedByAnimal");
		public static readonly ThoughtDef masochist_got_groped = DefDatabase<ThoughtDef>.GetNamed("MasochistGotGropedByAnimal");
		public static readonly ThoughtDef allowed_animal_to_breed = DefDatabase<ThoughtDef>.GetNamed("AllowedAnimalToBreed");
		public static readonly ThoughtDef allowed_animal_to_lick = DefDatabase<ThoughtDef>.GetNamed("AllowedAnimalToLick");
		public static readonly ThoughtDef allowed_animal_to_grope = DefDatabase<ThoughtDef>.GetNamed("AllowedAnimalToGrope");
		public static readonly ThoughtDef zoophile_got_bred = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotBredByAnimal");
		public static readonly ThoughtDef zoophile_got_anal_bred = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotAnalBredByAnimal");
		public static readonly ThoughtDef zoophile_got_licked = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotLickedByAnimal");
		public static readonly ThoughtDef zoophile_got_groped = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotGropedByAnimal");
		public static readonly ThoughtDef hate_my_rapist = DefDatabase<ThoughtDef>.GetNamed("HateMyRapist");
		public static readonly ThoughtDef kinda_like_my_rapist = DefDatabase<ThoughtDef>.GetNamed("KindaLikeMyRapist");
		public static readonly ThoughtDef allowed_me_to_get_raped = DefDatabase<ThoughtDef>.GetNamed("AllowedMeToGetRaped");
		public static readonly ThoughtDef stole_some_lovin = DefDatabase<ThoughtDef>.GetNamed("StoleSomeLovin");
		public static readonly ThoughtDef bloodlust_stole_some_lovin = DefDatabase<ThoughtDef>.GetNamed("BloodlustStoleSomeLovin");
		public static readonly ThoughtDef violated_corpse = DefDatabase<ThoughtDef>.GetNamed("ViolatedCorpse");
		public static readonly ThoughtDef gave_virginity = DefDatabase<ThoughtDef>.GetNamed("FortunateGaveVirginity");
		public static readonly ThoughtDef lost_virginity = DefDatabase<ThoughtDef>.GetNamed("UnfortunateLostVirginity");
		public static readonly ThoughtDef took_virginity = DefDatabase<ThoughtDef>.GetNamed("TookVirginity");

		public static readonly JobDef Masturbate = DefDatabase<JobDef>.GetNamed("RJW_Masturbate");
		public static readonly JobDef casual_sex = DefDatabase<JobDef>.GetNamed("JoinInBed");
		public static readonly JobDef knotted = DefDatabase<JobDef>.GetNamed("RJW_Knotted");
		public static readonly JobDef gettin_loved = DefDatabase<JobDef>.GetNamed("GettinLoved");
		public static readonly JobDef gettin_raped = DefDatabase<JobDef>.GetNamed("GettinRaped");
		public static readonly JobDef gettin_bred = DefDatabase<JobDef>.GetNamed("GettinBred");
		public static readonly JobDef RapeCP = DefDatabase<JobDef>.GetNamed("RapeComfortPawn");
		public static readonly JobDef RapeEnemy = DefDatabase<JobDef>.GetNamed("RapeEnemy");
		public static readonly JobDef RapeRandom = DefDatabase<JobDef>.GetNamed("RandomRape");
		public static readonly JobDef RapeCorpse = DefDatabase<JobDef>.GetNamed("ViolateCorpse");
		public static readonly JobDef bestiality = DefDatabase<JobDef>.GetNamed("Bestiality");
		public static readonly JobDef bestialityForFemale = DefDatabase<JobDef>.GetNamed("BestialityForFemale");
		public static readonly JobDef whore_inviting_visitors = DefDatabase<JobDef>.GetNamedSilentFail("WhoreInvitingVisitors");
		public static readonly JobDef whore_is_serving_visitors = DefDatabase<JobDef>.GetNamedSilentFail("WhoreIsServingVisitors");
		public static readonly JobDef animalMate = DefDatabase<JobDef>.GetNamed("RJW_Mate");
		public static readonly JobDef animalBreed = DefDatabase<JobDef>.GetNamed("Breed");
		public static readonly JobDef quick_sex = DefDatabase<JobDef>.GetNamed("Quickie");
		public static readonly JobDef getting_quickie = DefDatabase<JobDef>.GetNamed("GettingQuickie");
		public static readonly JobDef struggle_in_BondageGear = DefDatabase<JobDef>.GetNamed("StruggleInBondageGear");
		public static readonly JobDef unlock_BondageGear = DefDatabase<JobDef>.GetNamed("UnlockBondageGear");
		public static readonly JobDef give_BondageGear = DefDatabase<JobDef>.GetNamed("GiveBondageGear");

		public static readonly FleckDef mote_noheart = DefDatabase<FleckDef>.GetNamed("Mote_NoHeart");

		public static readonly RecordDef GetRapedAsComfortPawn = DefDatabase<RecordDef>.GetNamed("GetRapedAsComfortPrisoner");
		public static readonly RecordDef CountOfFappin = DefDatabase<RecordDef>.GetNamed("CountOfFappin");
		public static readonly RecordDef CountOfSex = DefDatabase<RecordDef>.GetNamed("CountOfSex");
		public static readonly RecordDef CountOfSexWithHumanlikes = DefDatabase<RecordDef>.GetNamed("CountOfSexWithHumanlikes");
		public static readonly RecordDef CountOfSexWithAnimals = DefDatabase<RecordDef>.GetNamed("CountOfSexWithAnimals");
		public static readonly RecordDef CountOfSexWithInsects = DefDatabase<RecordDef>.GetNamed("CountOfSexWithInsects");
		public static readonly RecordDef CountOfSexWithOthers = DefDatabase<RecordDef>.GetNamed("CountOfSexWithOthers");
		public static readonly RecordDef CountOfSexWithCorpse = DefDatabase<RecordDef>.GetNamed("CountOfSexWithCorpse");
		public static readonly RecordDef CountOfRapedHumanlikes = DefDatabase<RecordDef>.GetNamed("CountOfRapedHumanlikes");
		public static readonly RecordDef CountOfBeenRapedByHumanlikes = DefDatabase<RecordDef>.GetNamed("CountOfBeenRapedByHumanlikes");
		public static readonly RecordDef CountOfRapedAnimals = DefDatabase<RecordDef>.GetNamed("CountOfRapedAnimals");
		public static readonly RecordDef CountOfBeenRapedByAnimals = DefDatabase<RecordDef>.GetNamed("CountOfBeenRapedByAnimals");
		public static readonly RecordDef CountOfRapedInsects = DefDatabase<RecordDef>.GetNamed("CountOfRapedInsects");
		public static readonly RecordDef CountOfBeenRapedByInsects = DefDatabase<RecordDef>.GetNamed("CountOfBeenRapedByInsects");
		public static readonly RecordDef CountOfRapedOthers = DefDatabase<RecordDef>.GetNamed("CountOfRapedOthers");
		public static readonly RecordDef CountOfBeenRapedByOthers = DefDatabase<RecordDef>.GetNamed("CountOfBeenRapedByOthers");
		public static readonly RecordDef CountOfBirthHuman = DefDatabase<RecordDef>.GetNamed("CountOfBirthHuman");
		public static readonly RecordDef CountOfBirthAnimal = DefDatabase<RecordDef>.GetNamed("CountOfBirthAnimal");
		public static readonly RecordDef CountOfBirthEgg = DefDatabase<RecordDef>.GetNamed("CountOfBirthEgg");

		public static readonly MeditationFocusDef SexMeditationFocus = DefDatabase<MeditationFocusDef>.GetNamed("Sex");

		public enum rjwSextype { None, Vaginal, Anal, Oral, Masturbation, DoublePenetration, Boobjob, Handjob, Footjob, Fingering, Scissoring, MutualMasturbation, Fisting, Rimming, Fellatio, Cunnilingus, Sixtynine }
		public static readonly ReadOnlyCollection <rjwSextype> rjwSextypeCollection = Array.AsReadOnly((rjwSextype[])Enum.GetValues(typeof(rjwSextype)));

		public static void bootstrap(Map m)
		{
			if (m.GetComponent<MapCom_Injector>() == null)
				m.components.Add(new MapCom_Injector(m));
		}

		/// <summary>
		/// 简单方法，快速检查列表中是否存在匹配项。
		/// </summary>
		/// <param name="haystack"></param>
		/// <param name="needles"></param>
		/// <returns></returns>
		public static bool ContainsAny(this string haystack, params string[] needles) { return needles.Any(haystack.Contains); }

#nullable enable
		public static bool has_traits([NotNullWhen(true)] Pawn? pawn)
		{
			return pawn?.story?.traits != null;
		}
#nullable disable

		[SyncMethod]
		public static string random_pick_a_trait(this Pawn pawn)
		{
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			return has_traits(pawn) ? pawn.story.traits.allTraits.RandomElement().def.defName : null;
		}

		public static bool is_psychopath(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(TraitDefOf.Psychopath);
		}

		public static bool is_ascetic(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(TraitDefOf.Ascetic);
		}

		public static bool is_bloodlust(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(TraitDefOf.Bloodlust);
		}

		public static bool is_brawler(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(TraitDefOf.Brawler);
		}

		public static bool is_kind(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(TraitDefOf.Kind);
		}

		public static bool is_rapist(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(rapist);
		}

		public static bool is_necrophiliac(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(necrophiliac);
		}

		public static bool is_zoophile(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(zoophile);
		}

		public static bool is_masochist(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(TraitDef.Named("Masochist"));
		}

		/// <summary>
		/// 如果给定小人拥有花痴特质，则返回 true。
		/// 该小人的 pawnKind 可能是也可能不是花痴类。
		/// </summary>
		public static bool is_nympho(Pawn pawn)
		{
			return has_traits(pawn) && pawn.story.traits.HasTrait(nymphomaniac);
		}

		public static bool is_slime(Pawn pawn)
		{
			string racename = pawn.kindDef.race.defName.ToLower();
			//if (Prefs.DevMode) ModLog.Message(" is_slime " + xxx.get_pawnname(pawn) + " " + racename.Contains("slime"));

			return racename.Contains("slime");
		}

		public static bool is_demon(Pawn pawn)
		{
			string racename = pawn.kindDef.race.defName.ToLower();
			//if (Prefs.DevMode) ModLog.Message(" is_demon " + xxx.get_pawnname(pawn) + " " + racename.Contains("demon"));

			return racename.Contains("demon");
		}

		public static bool is_asexual(Pawn pawn) => pawn.GetCompRJW().orientation == Orientation.Asexual;
		public static bool is_bisexual(Pawn pawn) => pawn.GetCompRJW().orientation == Orientation.Bisexual;
		public static bool is_homosexual(Pawn pawn) => (pawn.GetCompRJW().orientation == Orientation.Homosexual || pawn.GetCompRJW().orientation == Orientation.MostlyHomosexual);
		public static bool is_heterosexual(Pawn pawn) => (pawn.GetCompRJW().orientation == Orientation.Heterosexual || pawn.GetCompRJW().orientation == Orientation.MostlyHeterosexual);
		public static bool is_pansexual(Pawn pawn) => pawn.GetCompRJW().orientation == Orientation.Pansexual;

		/// <summary>
		/// 检查 <paramref name="pawn"/> 是否为奴隶。
		/// </summary>
		/// <param name="pawn">所讨论的小人。</param>
		/// <returns><paramref name="pawn"/> 是否为奴隶</returns>
		/// <remarks>
		/// 此处同时包含模组奴隶和原版奴隶。
		/// </remarks>
		public static bool is_slave(Pawn pawn)
		{
			return is_vanillaslave(pawn) || is_modslave(pawn);
		}

		/// <summary>
		/// 检查 <paramref name="pawn"/> 是否为奴隶。
		/// </summary>
		/// <param name="pawn">所讨论的小人。</param>
		/// <returns><paramref name="pawn"/> 是否为奴隶</returns>
		/// <remarks>
		/// 此处仅包含原版奴隶。
		/// </remarks>
		public static bool is_vanillaslave(Pawn pawn)
		{
			if (pawn.IsSlave)//1.3
				return true;
			else
				return false;
		}

		/// <summary>
		/// 检查 <paramref name="pawn"/> 是否为奴隶。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns><paramref name="pawn"/> 是否为奴隶</returns>
		/// <remarks>
		/// 此处只包含模组添加的奴隶。
		/// <para>
		/// 目前支持：
		/// <list type="bullet">
		///     <item>
		///         <term>Simple Slavery</term>
		///         <description></description>
		///     </item>
		/// </list>
		/// </para>
		/// </remarks>
		public static bool is_modslave(Pawn pawn)
		{
			if (SimpleSlaveryIsActive)
				return pawn?.health.hediffSet.HasHediff(xxx.Enslaved) ?? false;
			return false;
		}

		public static bool is_nympho_or_rapist_or_zoophile(Pawn pawn)
		{
			if (!has_traits(pawn)) { return false; }
			return (is_rapist(pawn) || is_nympho(pawn) || is_zoophile(pawn));
		}

		//Humanoid Alien Framework 特质
		public static bool is_xenophile(Pawn pawn)
		{
			if (!has_traits(pawn) || !AlienFrameworkIsActive) { return false; }
			return pawn.story.traits.DegreeOfTrait(xenophobia) == -1;
		}

		public static bool is_xenophobe(Pawn pawn)
		{
			if (!has_traits(pawn) || !AlienFrameworkIsActive) { return false; }
			return pawn.story.traits.DegreeOfTrait(xenophobia) == 1;
		}

		public static bool is_whore(Pawn pawn)
		{
			if (!has_traits(pawn)) { return false; }
			return pawn != null && pawn.IsDesignatedService() && !pawn.story.traits.HasTrait(TraitDefOf.Asexual);
			//return (pawn != null && pawn.ownership != null && pawn.ownership.OwnedBed is Building_WhoreBed && (!xxx.RomanceDiversifiedIsActive || !pawn.story.traits.HasTrait(xxx.asexual)));
		}

		public static bool is_lecher(Pawn pawn)
		{
			if (!has_traits(pawn)) { return false; }
			return RomanceDiversifiedIsActive && pawn.story.traits.HasTrait(philanderer) || PsychologyIsActive && pawn.story.traits.HasTrait(lecher);
		}

		public static bool is_prude(Pawn pawn)
		{
			if (!has_traits(pawn)) { return false; }
			if (is_nympho(pawn)) { return false; }
			if (pawn.IsIdeologicallyPrudish()) { return true; }
			return RomanceDiversifiedIsActive && pawn.story.traits.HasTrait(faithful) || PsychologyIsActive && pawn.story.traits.HasTrait(prude);
		}

		public static bool is_animal(Pawn pawn)
		{
			return pawn?.RaceProps?.Animal ?? false;
		}

		public static bool is_insect(Pawn pawn)
		{
			if (pawn == null) return false;
			bool isit = pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid
						|| pawn.RaceProps.FleshType.corpseCategory.ToString().Contains("CorpsesInsect")
						//genetic rim
						|| pawn.RaceProps.FleshType.defName.Contains("GR_Insectoid");
			//ModLog.Message("is_insect " + get_pawnname(pawn) + " - " + isit);
			return isit;
		}

		public static bool is_mechanoid(Pawn pawn)
		{
			if (pawn == null) return false;
			if (AndroidsCompatibility.IsAndroid(pawn)) return false;

			bool isit = pawn.RaceProps.IsMechanoid
						|| pawn.RaceProps.FleshType == FleshTypeDefOf.Mechanoid
						|| pawn.RaceProps.FleshType.corpseCategory.ToString().Contains("CorpsesMechanoid")
						//genetic rim
						|| pawn.RaceProps.FleshType.defName.Contains("GR_Mechanoid")
						//android tiers
						|| pawn.RaceProps.FleshType.defName.Contains("MechanisedInfantry")
						|| pawn.RaceProps.FleshType.defName.Contains("Android")
						;
			//ModLog.Message("is_mechanoid " + get_pawnname(pawn) + " - " + isit);
			return isit;
		}

		public static bool is_tooluser(Pawn pawn)
		{
			return pawn.RaceProps.ToolUser;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_human(Pawn pawn)
		{
			if (pawn == null) return false;
			return pawn.RaceProps.Humanlike;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_female(Pawn pawn)
		{
			return pawn.gender == Gender.Female;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_male(Pawn pawn)
		{
			return pawn.gender == Gender.Male;
		}

		public static bool is_healthy(Pawn pawn)
		{
			return !pawn.Dead &&
				pawn.health.capacities.CanBeAwake &&
				bleedingRate(pawn) <= 0.0f &&
				pawn.health.hediffSet.PainTotal < config.significant_pain_threshold;
		}

		/// <summary>
		/// 检查 <paramref name="pawn"/> 是否不会很快死亡。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// 检查以下各项：
		/// <list type="bullet">
		///     <item>
		///         <term>死亡</term>
		///         <description><paramref name="pawn"/> 未死亡</description>
		///     </item>
		///     <item>
		///         <term>能够清醒</term>
		///         <description><paramref name="pawn"/> 能够保持清醒</description>
		///     </item>
		///     <item>
		///         <term>总失血率</term>
		///         <description>小于或等于 0.1</description>
		///     </item>
		/// </list>
		/// </para>
		/// </remarks>
		public static bool is_healthy_enough(Pawn pawn)
		{
			return !pawn.Dead &&
				pawn.health.capacities.CanBeAwake &&
				bleedingRate(pawn) <= 0.1f;
		}

		/// <summary>
		/// <paramref name="pawn"/> 能否主动发起行动或作出回应——例如卖淫等
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// 检查以下各项：
		/// <list type="bullet">
		///     <item>
		///         <term>健康</term>
		///         <description>参见 <see cref="is_healthy"/></description>
		///     </item>
		///     <item>
		///         <term>未倒地</term>
		///         <description></description>
		///     </item>
		///     <item>
		///         <term>未被暂停</term>
		///         <description></description>
		///     </item>
		/// </list>
		/// </para>
		/// </remarks>
		public static bool IsTargetPawnOkay(Pawn pawn)
		{
			return xxx.is_healthy(pawn) && !pawn.Downed && !pawn.Suspended;
		}

		/// <summary>
		/// 检查 <paramref name="pawn"/> 是否未处于濒死过程中。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// 检查以下各项：
		/// <list type="bullet">
		///     <item>
		///         <term>死亡</term>
		///         <description><paramref name="pawn"/> 未死亡</description>
		///     </item>
		///     <item>
		///         <term>总失血率</term>
		///         <description>小于或等于 0.3</description>
		///     </item>
		/// </list>
		/// </para>
		/// </remarks>
		public static bool is_not_dying(Pawn pawn)
		{
			return !pawn.Dead &&
				bleedingRate(pawn) <= 0.3f;
		}

		public static bool is_starved(Pawn pawn)
		{
			return pawn?.needs?.food != null &&
				pawn.needs.food.Starving;
		}
		public static float bleedingRate(Pawn pawn)
		{
			return pawn?.health?.hediffSet?.BleedRateTotal ?? 0f;
		}

		/// <summary>
		/// 尽力获取 <paramref name="pawn"/> 的有意义名称。
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns><paramref name="pawn"/> 的名称。默认为 <c>noname</c>。</returns>
		public static string get_pawnname(Pawn pawn)
		{
			//ModLog.Message("xxx::get_pawnname is "+ pawn.KindLabelDefinite());
			//ModLog.Message("xxx::get_pawnname is "+ pawn.KindLabelIndefinite());
			if (pawn == null) return "null";

			string name = pawn.Label;
			if (name != null)
			{
				if (pawn.Name?.ToStringShort != null)
					name = pawn.Name.ToStringShort;
			}
			else
				name = "noname";

			return name;
		}

		public static bool is_gettin_rapedNow(Pawn pawn)
		{
			if (pawn?.jobs?.curDriver != null)
			{
				return pawn.jobs.curDriver.GetType() == typeof(JobDriver_SexBaseRecieverRaped);
			}
			return false;
		}

		//格子检查开销小，寻路开销大。寻路检查放在最后。)

		public static float need_some_sex(Pawn pawn)
		{
			// 3=> 非人形生物永远发情
			float horniness_degree = 3f;
			Need_Sex need_sex = pawn.needs.TryGetNeed<Need_Sex>();
			if (need_sex == null) return horniness_degree;
			if (need_sex.CurLevel < need_sex.thresh_frustrated()) horniness_degree = 3f;
			else if (need_sex.CurLevel < need_sex.thresh_horny()) horniness_degree = 2f;
			else if (need_sex.CurLevel < need_sex.thresh_satisfied()) horniness_degree = 1f;
			else horniness_degree = 0f;
			return horniness_degree;
		}
		public enum SexNeed
		{
			Frustrated,
			Horny,
			Neutral,
			Satisfied
		};

		/// <summary>
		/// 获取 <paramref name="pawn"/> 的性需求。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// 使用以下之一：
		/// <list type="bullet">
		///     <item>
		///         <term>固定阈值</term>
		///         <description><see cref="need_sex_fixed"/></description>
		///     </item>
		///     <item>
		///         <term>原始阈值</term>
		///         <description><see cref="need_sex_broken"/></description>
		///     </item>
		/// </list>
		/// 取决于 <see cref="RJWSettings.sexneed_fix"/>。
		/// </para>
		/// </remarks>
		public static SexNeed need_sex(Pawn pawn)
		{
			// 3=> 非人形生物永远发情，因为它们没有该需求
			Need_Sex need_sex = pawn.needs.TryGetNeed<Need_Sex>();
			if (need_sex == null) return SexNeed.Frustrated;

			if (RJWSettings.sexneed_fix)
				return need_sex_fixed(pawn);
			else
				return need_sex_broken(pawn);
		}

		/// <summary>
		/// 原始的 rjw 性阈值
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>
		/// <list type="table">
		///     <listheader>
		///         <term>性需求</term>
		///         <description>条件</description>
		///     </listheader>
		///     <item>
		///         <term>满足</term>
		///         <description>当前水平 ≥ 满足阈值</description>
		///     </item>
		///     <item>
		///         <term>中性</term>
		///         <description>当前水平 ≥ 中性阈值（但 &lt; 满足）</description>
		///     </item>
		///     <item>
		///         <term>发情</term>
		///         <description>当前水平 ≥ 发情阈值（但 &lt; 中性）</description>
		///     </item>
		///     <item>
		///         <term>沮丧</term>
		///         <description>当前水平 &lt; 发情阈值</description>
		///     </item>
		/// </list>
		/// </returns>
		private static SexNeed need_sex_broken(Pawn pawn)
		{
			Need_Sex need_sex = pawn.needs.TryGetNeed<Need_Sex>();
			if (need_sex.CurLevel >= need_sex.thresh_satisfied())
				return SexNeed.Satisfied;
			else if (need_sex.CurLevel >= need_sex.thresh_neutral())
				return SexNeed.Neutral;
			else if (need_sex.CurLevel >= need_sex.thresh_horny())
				return SexNeed.Horny;
			else
				return SexNeed.Frustrated;
		}

		/// <summary>
		/// 固定的 rjw 性阈值
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>
		/// <list type="table">
		///     <listheader>
		///         <term>性需求</term>
		///         <description>条件</description>
		///     </listheader>
		///     <item>
		///         <term>沮丧</term>
		///         <description>当前水平 ≤ 沮丧阈值 </description>
		///     </item>
		///     <item>
		///         <term>发情</term>
		///         <description>当前水平 ≤ 发情阈值（但 &gt; 沮丧）</description>
		///     </item>
		///     <item>
		///         <term>中性</term>
		///         <description>当前水平 ≤ 中性阈值（但 &gt; 发情）  </description>
		///     </item>
		///     <item>
		///         <term>满足</term>
		///         <description>当前水平 &gt; 中性阈值</description>
		///     </item>
		/// </list>
		/// </returns>
		private static SexNeed need_sex_fixed(Pawn pawn)
		{
			// 3=> 非人形生物永远发情
			Need_Sex need_sex = pawn.needs.TryGetNeed<Need_Sex>();
			if (need_sex.CurLevel <= need_sex.thresh_frustrated())
				return SexNeed.Frustrated;
			else if (need_sex.CurLevel <= need_sex.thresh_horny())
				return SexNeed.Horny;
			else if (need_sex.CurLevel <= need_sex.thresh_neutral())
				return SexNeed.Neutral;
			else
				return SexNeed.Satisfied;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_frustrated(Pawn pawn)
		{
			return need_sex(pawn) == SexNeed.Frustrated;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_horny(Pawn pawn)
		{
			return need_sex(pawn) == SexNeed.Horny;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_hornyorfrustrated(Pawn pawn)
		{
			return (need_sex(pawn) == SexNeed.Horny || need_sex(pawn) == SexNeed.Frustrated);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_neutral(Pawn pawn)
		{
			return need_sex(pawn) == SexNeed.Neutral;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool is_satisfied(Pawn pawn)
		{
			return need_sex(pawn) == SexNeed.Satisfied;
		}

		/// <summary> 检查该小人是否有任何伴侣不具备多角恋/多配偶特质或理念；也就是那种会因对方到处乱睡而发怒的人。 </summary>
		/// <returns> 如果该小人至少有一名不具备多角特质或理念的恋人则为 True。没有伴侣或所有伴侣都是多角则为 False。 </returns>
		public static bool HasNonPolyPartner(Pawn pawn, bool onCurrentMap = false)
		{
			if (pawn.relations == null) // 大概是机器人，或者天知道是什么模组造出来的怪物
				return false;

			var partners = LovePartnerRelationUtility.ExistingLovePartners(pawn, allowDead: false).Select(rel => rel.otherPawn);
			
			// 如果完全没有伴侣，可以直接返回。
			if (!partners.Any())
				return false;

			foreach (var partner in partners)
			{
				// 处于停滞状态的伴侣永远不会发现！
				if (RJWHookupSettings.CheatIfSafe && partner.Suspended)
					continue;

				// 其他地图上的人也不会发现，因为在外偷情显然永远都不会被发现
				if (RJWHookupSettings.CheatIfSafe && onCurrentMap) // 只在当前地图上检查
					if (pawn.Map == null || partner.Map == null || partner.Map != pawn.Map)
						continue;

				if (partner.IsPoly())
				{
					// 我们找到了一个具有多角特质或理念的伴侣！  但他们可能有多个伴侣，所以继续检查。
					continue;
				}

				// 我们找到了一个不多角的伴侣。
				return true;
			}

			// 如果走到这里，说明我们检查了每个伴侣且全都有多角特质，因此该小人没有非多角伴侣。
			return false;
		}

		/// <summary> 检查该小人的关系网中是否有任何已建立羁绊的小人</summary>
		/// <returns> 如果该小人至少有一名已建立羁绊的小人则为 True。关系网中没有已建立羁绊的小人则为 False。 </returns>
		public static bool HasBond(Pawn pawn)
		{
			if (pawn.relations.DirectRelations.Any((DirectPawnRelation dr) => dr.def == PawnRelationDefOf.Bond))
				return true;
			return false;
		}

		public static float get_sex_satisfaction(Pawn pawn)
		{
			try
			{
				return pawn.GetStatValue(RJWStatDefOf.SexSatisfaction, false);
			}
			catch (NullReferenceException)
			//未填充统计数据，对非人形生物/尸体会报错
			//这里以及下面的代码大概应该重写，把计算放在这里进行
			{
				//ModLog.Warning(e.ToString());
				return 1f;
			}
		}

		public static float get_vulnerability(Pawn pawn)
		{
			try
			{
				return pawn.GetStatValue(RJWStatDefOf.Vulnerability, false);
			}
			catch (NullReferenceException)
			//未填充统计数据，对非人形生物/尸体会报错
			{
				//ModLog.Warning(e.ToString());
				return 1f;
			}
		}

		public static float get_sex_drive(Pawn pawn)
		{
			try
			{
				return pawn.GetStatValue(RJWStatDefOf.SexFrequency, false) * (pawn.GetRJWPawnData().raceSexDrive);
			}
			catch (NullReferenceException)
			//未填充统计数据，对非人形生物/尸体会报错
			{
				//ModLog.Warning(e.ToString());
				return 1f;
			}
		}

		public static bool IsSingleOrPartnersNotHere(Pawn pawn)
		{
			var relations = LovePartnerRelationUtility.ExistingLovePartners(pawn, allowDead: false);

			if (!relations.Any())
			{
				return true;
			}
			return relations.All(rel => rel.otherPawn.Map != pawn.Map);
		}

		//基础检查
		public static bool can_do_loving(Pawn pawn)
		{
			if (is_mechanoid(pawn))
				return false;

			if (is_human(pawn))
			{
				bool humansex = true;
				AgeCategory ageCategory = pawn.GetAgeCategory();

				//ModLog.Message("can_do_loving pawn " + pawn.Name);
				//ModLog.Message("	 AgeBiologicalYears " + pawn.ageTracker.AgeBiologicalYears);
				//ModLog.Message("	 CurLifeStage " + pawn.ageTracker.CurLifeStage.defName);
				//ModLog.Message("	 reproductive " + pawn.ageTracker.CurLifeStage.reproductive);
				//ModLog.Message("	 Growth " + pawn.ageTracker.Growth);
				//ModLog.Message("	 Adult " + pawn.ageTracker.Adult);
				//ModLog.Message("	 AgeCategory " + pawn.ageTracker.Adult);

				//foreach ( var stage in pawn.RaceProps.lifeStageAges)
				//{
				//	ModLog.Message("	 lifeStage  ");
				//	ModLog.Message("	 def " + stage.def);
				//	ModLog.Message("	 minAge " + stage.minAge);
				//	ModLog.Message("	 reproductive " + stage.def.reproductive);
				//	ModLog.Message("	 milkable " + stage.def.milkable);
				//	ModLog.Message("	 shearable " + stage.def.shearable);
				//}

				if (ageCategory == AgeCategory.Child)
				{
					humansex = false;
				}

				if (!pawn.apparel.WornApparel.NullOrEmpty())
					if (pawn.apparel.WornApparel.Where(x => x.def.defName.ToLower().Contains("warcasket")).Any())
						humansex = false;

				return humansex;
			}
			if (is_animal(pawn))
			{
				if (pawn.GetRJWPawnData().RaceSupportDef?.teenAge != null && pawn.GetRJWPawnData().RaceSupportDef?.teenAge != 0)
				{
					int age = pawn.ageTracker.AgeBiologicalYears;
					int t = pawn.GetRJWPawnData().RaceSupportDef.teenAge;
					if (age < t)
						return false;
				}
				//对昆虫使用 CurLifeStage/Growth，因为它们不可繁殖
				else if (!pawn.ageTracker.CurLifeStage.reproductive)
					if (pawn.ageTracker.Growth < 1)
						return false;

				return true;
			}
			return false;
		}

		public static bool can_do_animalsex(Pawn pawn1, Pawn pawn2)
		{
			if (xxx.is_animal(pawn1) && xxx.is_animal(pawn2))
			{
				if (RJWSettings.animal_on_animal_enabled)
					return true;
			}
			else if (RJWSettings.bestiality_enabled)
			{
				return true;
			}

			return false;
		}

		public static bool can_masturbate(Pawn pawn)
		{
			if (!pawn.HasParts()) return false;

			if (!RJWPreferenceSettings.FapInArmbinders)
				if (Genital_Helper.hands_blocked(pawn))
					return false;
			if (!RJWPreferenceSettings.FapInBelts)
				if (Genital_Helper.genitals_blocked(pawn))
					return false;

			if (!xxx.can_be_fucked(pawn) && !xxx.can_fuck(pawn)) //TODO: 以后应该改进这一点
				return false;

			return true;
		}

		/// <summary>
		/// 对 <paramref name="pawn"/> 进行插入器官检查。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		public static bool can_fuck(Pawn pawn)
		{
			//这可能会对人形机械体造成问题，比如 misc. bots 或其他自定义种族的机械体
			if (is_mechanoid(pawn))
				return true;

			if (!can_do_loving(pawn))
				return false;

			var parts = pawn.GetGenitalsList();

			if (Genital_Helper.penis_blocked(pawn) || (!Genital_Helper.has_penis_fertile(pawn, parts) && !Genital_Helper.has_penis_infertile(pawn, parts) && !Genital_Helper.has_ovipositorF(pawn, parts))) return false;

			return true;
		}

		/// <summary>
		/// 对 <paramref name="pawn"/> 进行腔口检查。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		public static bool can_be_fucked(Pawn pawn)
		{
			if (is_mechanoid(pawn))
				if (MechanoidsCompatibility.IsMechanoidSexy(pawn))
					return true;
				else
					return false;

			if (!can_do_loving(pawn))
				return false;

			if (Genital_Helper.has_anus(pawn) && !Genital_Helper.anus_blocked(pawn))
				return true;
			if (Genital_Helper.has_vagina(pawn) && !Genital_Helper.vagina_blocked(pawn))
				return true;
			if (Genital_Helper.has_mouth(pawn) && !Genital_Helper.oral_blocked(pawn))
				return true;
			//不确定下面的内容，女性强暴男性时需要检查所有代码，所以算了
			//if ((Genital_Helper.has_penis(pawn) || Genital_Helper.has_penis_infertile(pawn) || Genital_Helper.has_ovipositorF(pawn)) && !Genital_Helper.penis_blocked(pawn))
			//	return true;
			//if (Genital_Helper.has_breasts(pawn) && !Genital_Helper.breasts_blocked(pawn))
			//	return true;
			//if (pawn.health.hediffSet.GetNotMissingParts().Any(part => part.IsInGroup(BodyPartGroupDefOf.RightHand) || part.IsInGroup(BodyPartGroupDefOf.LeftHand)) && !Genital_Helper.hands_blocked(pawn))
			//	return true;

			return false;
		}

		public static bool can_rape(Pawn pawn, bool forced = false)
		{
			if (!RJWSettings.rape_enabled)
				return false;

			if (is_mechanoid(pawn))
				return true;

			if (!(can_fuck(pawn) ||
				(!is_male(pawn) && get_vulnerability(pawn) < RJWSettings.nonFutaWomenRaping_MaxVulnerability && can_be_fucked(pawn))))
				return false;

			if (is_human(pawn))
			{
				if (pawn.ageTracker.Growth < 1 && !pawn.ageTracker.CurLifeStage.reproductive)
					return false;

				if (RJWSettings.WildMode || forced)
					return true;

				return need_some_sex(pawn) > 0;
			}

			return true;
		}

		public static bool can_get_raped(Pawn pawn)
		{
			if (!RJWSettings.rape_enabled)
				return false;

			if (!can_be_fucked(pawn))
				return false;

			if (is_human(pawn))
			{
				if (pawn.ageTracker.Growth < 1 && !pawn.ageTracker.CurLifeStage.reproductive)
					return false;

				if (RJWSettings.WildMode)
					return true;

				if (!(RJWSettings.rapee_MinVulnerability_human >= 0 && get_vulnerability(pawn) > RJWSettings.rapee_MinVulnerability_human))
					return false;
			}

			return true;
		}

		public static bool is_Virgin(Pawn pawn)
		{
			//if (RJWSettings.DevMode) ModLog.Message("xxx::is_Virgin check:" +get_pawnname(pawn));
			if (pawn.relations != null)
				if (pawn.relations.ChildrenCount > 0)
				{
					//if (RJWSettings.DevMode) ModLog.Message("xxx::is_Virgin " + " ChildrenCount " + pawn.relations.ChildrenCount);
					return false;
				}

			if (!(
				pawn.records.GetValue(GetRapedAsComfortPawn) == 0 &&
				pawn.records.GetValue(CountOfSex) == 0 &&
				pawn.records.GetValue(CountOfSexWithHumanlikes) == 0 &&
				pawn.records.GetValue(CountOfSexWithAnimals) == 0 &&
				pawn.records.GetValue(CountOfSexWithInsects) == 0 &&
				pawn.records.GetValue(CountOfSexWithOthers) == 0 &&
				pawn.records.GetValue(CountOfSexWithCorpse) == 0 &&
				//pawn.records.GetValue(CountOfWhore) == 0 &&
				pawn.records.GetValue(CountOfRapedHumanlikes) == 0 &&
				pawn.records.GetValue(CountOfBeenRapedByHumanlikes) == 0 &&
				pawn.records.GetValue(CountOfRapedAnimals) == 0 &&
				pawn.records.GetValue(CountOfBeenRapedByAnimals) == 0 &&
				pawn.records.GetValue(CountOfRapedInsects) == 0 &&
				pawn.records.GetValue(CountOfBeenRapedByInsects) == 0 &&
				pawn.records.GetValue(CountOfRapedOthers) == 0 &&
				pawn.records.GetValue(CountOfBeenRapedByOthers) == 0 &&
				pawn.records.GetAsInt(xxx.CountOfBirthHuman) == 0 &&
				pawn.records.GetAsInt(xxx.CountOfBirthAnimal) == 0 &&
				pawn.records.GetAsInt(xxx.CountOfBirthEgg) == 0
				))
			{
				//if (RJWSettings.DevMode) ModLog.Message("xxx::is_Virgin " + "records check fail");
				return false;
			}
			return true;
		}

#nullable enable

		[PrepatcherField]
		[InjectComponent]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CompRJW? GetCompRJW(this Pawn pawn)
		{
			// 调用 pawn.GetCompRJW().<method> 检查性取向等。
			return pawn.GetComp<CompRJW>();
		}
	}
}
