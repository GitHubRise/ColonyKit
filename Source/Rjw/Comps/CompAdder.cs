using Verse;
using System.Linq;
using RimWorld;

namespace rjw
{
	[StaticConstructorOnStartup]
	public static class AddComp
	{
		static AddComp()
		{
			AddRJWComp();
		}

		/// <summary>
		/// 启动时自动为所有种族添加该组件。
		/// </summary>
		public static void AddRJWComp()
		{
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(thingDef =>
					thingDef.race != null &&
					thingDef.thingClass != typeof(Corpse) && thingDef.thingClass != typeof(UnnaturalCorpse) &&
					thingDef.thingClass.Name != "VehiclePawn")) // Vehicle framework
			{
				thingDef.comps.Add(new CompProperties(typeof(CompRJW)));
				//ModLog.Message("AddRJWComp to race " + thingDef.label);
			}

			foreach (PawnKindDef pawnKindDef in DefDatabase<PawnKindDef>.AllDefs.Where(pawnKindDef => pawnKindDef.race.race != null))
			{
				if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawnKindDef, out RaceGroupDef? raceGroupDef))
				{
					//ModLog.Message("RaceGroupDef_Helper " + raceGroupDef.defName + " for " + pawnKindDef.race.defName);
					if (raceGroupDef?.oviPregnancy == true)
					{
						if (pawnKindDef.race.comps.Any(x => x is CompProperties_EggLayer))
						{
							//ModLog.Message(pawnKindDef.race.defName + " was already egglayer");
						}
						else
						{
							CompProperties_EggLayer eggProps = OviHelper.GenerateEggLayerProperties(pawnKindDef, raceGroupDef);
							pawnKindDef.race.comps.Add(eggProps);
							//ModLog.Message(pawnKindDef.race.defName + " is now egglayer and lays " + eggProps.eggFertilizedDef.defName + " eggs");
						}
					}
				}
			}

			// 不知为何，只有小人处于“可挤奶”的生命阶段时蛋才会生长
			// 这可能并不理想……
			foreach (LifeStageDef lifeStage in DefDatabase<LifeStageDef>.AllDefs)
			{
				if (lifeStage.reproductive)
				{
					lifeStage.milkable = true;
				}
			}
		}
	}
}