#nullable disable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace rjw
{
	[StaticConstructorOnStartup]
	internal static class First
	{
		/*private static void show_bpr(String body_part_record_def_name)
		{
			var bpr = BodyDefOf.Human.AllParts.Find((BodyPartRecord can) => String.Equals(can.def.defName, body_part_record_def_name));
			--ModLog.Message(body_part_record_def_name + " BPR internals:");
			--ModLog.Message("  def: " + bpr.def.ToString());
			--ModLog.Message("  parts: " + bpr.parts.ToString());
			--ModLog.Message("  parts.count: " + bpr.parts.Count.ToString());
			--ModLog.Message("  height: " + bpr.height.ToString());
			--ModLog.Message("  depth: " + bpr.depth.ToString());
			--ModLog.Message("  coverage: " + bpr.coverage.ToString());
			--ModLog.Message("  groups: " + bpr.groups.ToString());
			--ModLog.Message("  groups.count: " + bpr.groups.Count.ToString());
			--ModLog.Message("  parent: " + bpr.parent.ToString());
			--ModLog.Message ("  fleshCoverage: " + bpr.fleshCoverage.ToString ());
			--ModLog.Message ("  absoluteCoverage: " + bpr.absoluteCoverage.ToString ());
			--ModLog.Message ("  absoluteFleshCoverage: " + bpr.absoluteFleshCoverage.ToString ());
		}*/

		//儿童模组用了相同的 defname，但没有 worker 类，所以在这里覆盖。
		public static void inject_Reproduction()
		{
			PawnCapacityDef reproduction = DefDatabase<PawnCapacityDef>.GetNamed("RJW_Fertility");
			reproduction.workerClass = typeof(PawnCapacityWorker_Fertility);
		}

		public static void inject_whoringtab()
		{
			//InjectTab(typeof(MainTab.MainTabWindow_Brothel), def => def.race?.Humanlike == true);
		}

		private static void InjectTab(Type tabType, Func<ThingDef, bool> qualifier)
		{
			var defs = DefDatabase<ThingDef>.AllDefs.Where(qualifier).ToList();
			defs.RemoveDuplicates();

			var tabBase = InspectTabManager.GetSharedInstance(tabType);

			foreach (var def in defs)
			{
				if (def.inspectorTabs == null || def.inspectorTabsResolved == null) continue;

				if (!def.inspectorTabs.Contains(tabType))
				{
					def.inspectorTabs.Add(tabType);
					def.inspectorTabsResolved.Add(tabBase);
					//ModLog.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
				}
			}
		}

		private static void inject_recipes()
		{
			//--ModLog.Message(" First::inject_recipes");
			// 将制作人造私处的配方注入手工制作点或机械加工台。
			// 但同时也会动态检测是否加载了 EPOE，若已加载，则把配方改为注入 EPOE 的
			// 制作台。

			try
			{
				//原版工作台
				var cra_spo = DefDatabase<ThingDef>.GetNamed("CraftingSpot");
				var mac_ben = DefDatabase<ThingDef>.GetNamed("TableMachining");
				var fab_ben = DefDatabase<ThingDef>.GetNamed("FabricationBench");
				var tai_ben = DefDatabase<ThingDef>.GetNamed("ElectricTailoringBench");

				// 将束缚装备配方注入对应的制作台
				//if (xxx.config.bondage_gear_enabled)
				//{
					//fab_ben.AllRecipes.Add(DefDatabase<RecipeDef>.GetNamed("MakeHololock"));
					//tai_ben.AllRecipes.Add(DefDatabase<RecipeDef>.GetNamed("MakeArmbinder"));
					//tai_ben.AllRecipes.Add(DefDatabase<RecipeDef>.GetNamed("MakeChastityBelt"));
					//tai_ben.AllRecipes.Add(DefDatabase<RecipeDef>.GetNamed("MakeChastityBeltO"));
					//tai_ben.AllRecipes.Add(DefDatabase<RecipeDef>.GetNamed("MakeChastityCage"));
					//tai_ben.AllRecipes.Add(DefDatabase<RecipeDef>.GetNamed("MakeBallGag"));
					//tai_ben.AllRecipes.Add(DefDatabase<RecipeDef>.GetNamed("MakeRingGag"));
				//}
			}
			catch
			{
				ModLog.Warning("Unable to inject recipes.");
				ModLog.Warning("Yay! Your medieval mod broke recipes. And, likely, parts too, expect errors.");
			}
		}

		/*private static void show_bs(Backstory bs)
		{
			--ModLog.Message("Backstory \"" + bs.Title + "\" internals:");
			--ModLog.Message("  identifier: " + bs.identifier);
			--ModLog.Message("  slot: " + bs.slot.ToString());
			--ModLog.Message("  Title: " + bs.Title);
			--ModLog.Message("  TitleShort: " + bs.TitleShort);
			--ModLog.Message("  baseDesc: " + bs.baseDesc);
			--ModLog.Message("  skillGains: " + ((bs.skillGains == null) ? "null" : bs.skillGains.ToString()));
			--ModLog.Message("  skillGainsResolved: " + ((bs.skillGainsResolved == null) ? "null" : bs.skillGainsResolved.ToString()));
			--ModLog.Message("  workDisables: " + bs.workDisables.ToString());
			--ModLog.Message("  requiredWorkTags: " + bs.requiredWorkTags.ToString());
			--ModLog.Message("  spawnCategories: " + bs.spawnCategories.ToString());
			--ModLog.Message("  bodyTypeGlobal: " + bs.bodyTypeGlobal.ToString());
			--ModLog.Message("  bodyTypeFemale: " + bs.bodyTypeFemale.ToString());
			--ModLog.Message("  bodyTypeMale: " + bs.bodyTypeMale.ToString());
			--ModLog.Message("  forcedTraits: " + ((bs.forcedTraits == null) ? "null" : bs.forcedTraits.ToString()));
			--ModLog.Message("  disallowedTraits: " + ((bs.disallowedTraits == null) ? "null" : bs.disallowedTraits.ToString()));
			--ModLog.Message("  shuffleable: " + bs.shuffleable.ToString());
		}*/

		//快速检查是否加载了另一个模组。
		private static bool IsLoaded(string mod)
		{
			return LoadedModManager.RunningModsListForReading.Any(x => x.Name == mod);
		}

		private static void CheckingCompatibleMods()
		{
			{//Humanoid Alien Races Framework 2.0
				xxx.xenophobia = DefDatabase<TraitDef>.GetNamedSilentFail("HAR_Xenophobia");
				if (xxx.xenophobia is null)
				{
					xxx.AlienFrameworkIsActive = false;
				}
				else
				{
					xxx.AlienFrameworkIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("Humanoid Alien Races 2.0 is detected. Xenophile and Xenophobe traits active.");
				}
			}

			{//关系-性向类模组
				{//RomanceDiversified
					xxx.straight = DefDatabase<TraitDef>.GetNamedSilentFail("Straight");
					xxx.faithful = DefDatabase<TraitDef>.GetNamedSilentFail("Faithful");
					xxx.philanderer = DefDatabase<TraitDef>.GetNamedSilentFail("Philanderer");
					xxx.polyamorous = DefDatabase<TraitDef>.GetNamedSilentFail("Polyamorous");
					if (xxx.straight is null || xxx.faithful is null || xxx.philanderer is null)
					{
						xxx.RomanceDiversifiedIsActive = false;
					}
					else
					{
						xxx.RomanceDiversifiedIsActive = true;
						if (RJWSettings.DevMode) ModLog.Message("RomanceDiversified is detected.");
					}

					// 任何含有多边恋特质的模组
					if(xxx.polyamorous is null)
					{
						xxx.AnyPolyamoryModIsActive = false;
					}
					else
					{
						xxx.AnyPolyamoryModIsActive = true;
					}
				}

				{//[SYR] Individuality
					xxx.SYR_CreativeThinker = DefDatabase<TraitDef>.GetNamedSilentFail("SYR_CreativeThinker");
					xxx.SYR_Haggler = DefDatabase<TraitDef>.GetNamedSilentFail("SYR_Haggler");
					if (xxx.SYR_CreativeThinker is null || xxx.SYR_Haggler is null)
					{
						xxx.IndividualityIsActive = false;
						if (RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.SYRIndividuality)
							RJWPreferenceSettings.sexuality_distribution = RJWPreferenceSettings.Rjw_sexuality.Vanilla;
					}
					else
					{
						xxx.IndividualityIsActive = true;
						if (RJWSettings.DevMode) ModLog.Message("Individuality is detected.");
					}
				}

				{//Psychology
					xxx.prude = DefDatabase<TraitDef>.GetNamedSilentFail("Prude");
					xxx.lecher = DefDatabase<TraitDef>.GetNamedSilentFail("Lecher");
					xxx.polygamous = DefDatabase<TraitDef>.GetNamedSilentFail("Polygamous");
					if (xxx.prude is null || xxx.lecher is null || xxx.polygamous is null)
					{
						xxx.PsychologyIsActive = false;
						if (RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.Psychology)
							RJWPreferenceSettings.sexuality_distribution = RJWPreferenceSettings.Rjw_sexuality.Vanilla;
					}
					else
					{
						xxx.PsychologyIsActive = true;
						if (RJWSettings.DevMode) ModLog.Message("Psychology is detected. (Note: only partially supported)");
					}

					// 任何含有多配偶特质的模组
					if (xxx.polygamous is null)
					{
						xxx.AnyPolygamyModIsActive = false;
					}
					else
					{
						xxx.AnyPolygamyModIsActive = true;
					}
				}
				{//Rimpsyche
					var RPS_loaded = ModLister.GetActiveModWithIdentifier("maux36.rimpsyche.sexuality", ignorePostfix: true) != null;
					xxx.RimpsycheSexualityIsActive = RPS_loaded;
					if (!RPS_loaded)
					{
						if (RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.RimPsyche)
							RJWPreferenceSettings.sexuality_distribution = RJWPreferenceSettings.Rjw_sexuality.Vanilla;
					}
					else
					{
						RJWPreferenceSettings.sexuality_distribution = RJWPreferenceSettings.Rjw_sexuality.RimPsyche;
					}
				}

				if (xxx.PsychologyIsActive == false && 
					xxx.IndividualityIsActive == false && 
					xxx.RimpsycheSexualityIsActive == false && 
					RJWPreferenceSettings.sexuality_distribution != RJWPreferenceSettings.Rjw_sexuality.RimJobWorld)
				{
					RJWPreferenceSettings.sexuality_distribution = RJWPreferenceSettings.Rjw_sexuality.Vanilla;
				}
			}

			{//SimpleSlavery
				xxx.Enslaved = DefDatabase<HediffDef>.GetNamedSilentFail("Enslaved");
				if (xxx.Enslaved is null)
				{
					xxx.SimpleSlaveryIsActive = false;
				}
				else
				{
					xxx.SimpleSlaveryIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("SimpleSlavery is detected.");
				}
			}

			{//[KV] Consolidated Traits
				xxx.RCT_NeatFreak = DefDatabase<TraitDef>.GetNamedSilentFail("RCT_NeatFreak");
				xxx.RCT_Savant = DefDatabase<TraitDef>.GetNamedSilentFail("RCT_Savant");
				xxx.RCT_Inventor = DefDatabase<TraitDef>.GetNamedSilentFail("RCT_Inventor");
				xxx.RCT_AnimalLover = DefDatabase<TraitDef>.GetNamedSilentFail("RCT_AnimalLover");
				if (xxx.RCT_NeatFreak is null || xxx.RCT_Savant is null || xxx.RCT_Inventor is null || xxx.RCT_AnimalLover is null)
				{
					xxx.CTIsActive = false;
				}
				else
				{
					xxx.CTIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("Consolidated Traits found, adding trait compatibility.");
				}
			}


			{//Rimworld of Magic
				xxx.Succubus = DefDatabase<TraitDef>.GetNamedSilentFail("Succubus");
				xxx.Warlock = DefDatabase<TraitDef>.GetNamedSilentFail("Warlock");
				xxx.TM_Mana = DefDatabase<NeedDef>.GetNamedSilentFail("TM_Mana");
				xxx.TM_ShapeshiftHD = DefDatabase<HediffDef>.GetNamedSilentFail("TM_ShapeshiftHD");
				if (xxx.Succubus is null || xxx.Warlock is null || xxx.TM_Mana is null || xxx.TM_ShapeshiftHD is null)
				{
					xxx.RoMIsActive = false;
				}
				else
				{
					xxx.RoMIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("Rimworld of Magic is detected.");
				}
			}


			{//Nightmare Incarnation
				xxx.NI_Need_Mana = DefDatabase<NeedDef>.GetNamedSilentFail("NI_Need_Mana");
				if (xxx.NI_Need_Mana is null)
				{
					xxx.NightmareIncarnationIsActive = false;
				}
				else
				{
					xxx.NightmareIncarnationIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("Nightmare Incarnation is detected.");
				}
			}

			{//DubsBadHygiene
				xxx.DBHThirst = DefDatabase<NeedDef>.GetNamedSilentFail("DBHThirst");
				if (xxx.DBHThirst is null)
				{
					xxx.DubsBadHygieneIsActive = false;
				}
				else
				{
					xxx.DubsBadHygieneIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("Dubs Bad Hygiene is detected.");
				}
			}

			{//Immortals
				xxx.IH_Immortal = DefDatabase<HediffDef>.GetNamedSilentFail("IH_Immortal");
				if (xxx.IH_Immortal is null)
				{
					xxx.ImmortalsIsActive = false;
				}
				else
				{
					xxx.ImmortalsIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("Immortals is detected.");
				}
			}

			{// RJW-EX
				xxx.RjwEx_Armbinder = DefDatabase<HediffDef>.GetNamedSilentFail("Armbinder");
				if (xxx.RjwEx_Armbinder is null) { xxx.RjwExIsActive = false;}
				else { xxx.RjwExIsActive = true; }

			}

			{//Combat Extended
				xxx.MuscleSpasms = DefDatabase<HediffDef>.GetNamedSilentFail("MuscleSpasms");
				if (xxx.MuscleSpasms is null)
				{
					xxx.CombatExtendedIsActive = false;
				}
				else
				{
					xxx.CombatExtendedIsActive = true;
					if (RJWSettings.DevMode) ModLog.Message("Combat Extended is detected. Current compatibility unknown, use at your own risk. ");
				}
			}
		}

		static First()
		{
			ModMetaData self = ModLister.GetModWithIdentifier("rim.job.world")
				?? ModLister.GetModWithIdentifier("local.colonykit");
			ModLog.Message($"Initializing RimJobWorld {self?.ModVersion ?? "bundled"}");

			// 检查必需的模组
			//CheckModRequirements();
			CheckIncompatibleMods();
			CheckingCompatibleMods();

			inject_Reproduction();
			//inject_recipes(); // 不再需要。工作台已在配方定义中定义

			//bondage_gear_tradeability.init();

			var har = new Harmony("rjw");
			har.PatchAll(Assembly.GetExecutingAssembly());
			PATCH_Pawn_ApparelTracker_TryDrop.apply(har);
		}

		internal static void CheckModRequirements()
		{
			//--ModLog.Message("First::CheckModRequirements() called");
			List<string> required_mods = new List<string> {
				"HugsLib",
			};
			foreach (string required_mod in required_mods)
			{
				bool found = false;
				foreach (ModMetaData installed_mod in ModLister.AllInstalledMods)
				{
					if (installed_mod.Active && installed_mod.Name.Contains(required_mod))
					{
						found = true;
					}

					if (!found)
					{
						ErrorMissingRequirement(required_mod);
					}
				}
			}
		}

		internal static void CheckIncompatibleMods()
		{
			//--ModLog.Message("First::CheckIncompatibleMods() called");
			List<string> incompatible_mods = new List<string> {
				"Bogus Test Mod That Doesn't Exist",
				"Lost Forest"
			};
			foreach (string incompatible_mod in incompatible_mods)
			{
				foreach (ModMetaData installed_mod in ModLister.AllInstalledMods)
				{
					if (installed_mod.Active && installed_mod.Name.Contains(incompatible_mod))
					{
						ErrorIncompatibleMod(installed_mod);
					}
				}
			}
		}

		internal static void ErrorMissingRequirement(string missing)
		{
			ModLog.Error("Initialization error:  Unable to find required mod '" + missing + "' in mod list");
		}

		internal static void ErrorIncompatibleMod(ModMetaData othermod)
		{
			ModLog.Error("Initialization Error:  Incompatible mod '" + othermod.Name + "' detected in mod list");
		}
	}
}
