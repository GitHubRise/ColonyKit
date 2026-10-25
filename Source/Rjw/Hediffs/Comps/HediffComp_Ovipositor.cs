#nullable disable

using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using RimWorld;

using Verse;

namespace rjw
{
	public class HediffComp_Ovipositor : HediffComp
	{
		public IntRange eggInterval;

		private int nextEggTick;

		private static HediffDef_InsectEgg UnknownEgg = DefDatabase<HediffDef_InsectEgg>.GetNamed("RJW_UnknownEgg");

		public HediffCompProperties_Ovipositor Props => props as HediffCompProperties_Ovipositor;

		public override void CompPostMake()
		{
			eggInterval = Props.eggIntervalTicks;
		}
		/// <summary>
		/// 产卵 tick 数
		/// </summary>
		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);

			// 小人（动物）处于迷雾中，不产卵，以节省 tick
			if (parent.pawn.MapHeld == null) return;
			if (parent.pawn.Fogged())
				return;

			var thisTick = Find.TickManager.TicksGame;

			if (thisTick >= nextEggTick)
			{
				var def = parent.def as HediffDef_SexPart;
				var pawn = parent.pawn;
				var isPlayerFaction = pawn.Faction?.IsPlayer ?? false; // 殖民者/动物/机械体/变异体
				var onHomeMap = pawn.Map?.IsPlayerHome ?? false;

				if (!onHomeMap && !isPlayerFaction && !pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony)
				{
					return;
				}
				
				if ((pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) <= 0.5) || !xxx.can_do_loving(pawn))
				{
					return;
				}

				float maxEggsSize = PregnancyHelper.GetMaxEggsSize(pawn);
				float eggedSize = GetCombinedEggSize(pawn);

				if (RJWSettings.DevMode) ModLog.Message($"{xxx.get_pawnname(pawn)} filled with {eggedSize} out of max capacity of {maxEggsSize} eggs.");
				if (eggedSize < maxEggsSize)
				{
					HediffDef_InsectEgg egg;

					if (RJWSettings.DevMode) ModLog.Message($" Trying to find a {pawn.kindDef} egg");
					if (!TryGetEgg(pawn.kindDef.defName, out egg))
					{
						if (RJWSettings.DevMode) ModLog.Message($" No {pawn.kindDef} egg found, defaulting to UnknownEgg");
						egg = UnknownEgg;
					}

					if (RJWSettings.DevMode) ModLog.Message($"I choose you, {egg}!");

					var parentPart = parent.Part;
					if (parentPart != null)
					{
						AddEggs(egg, parentPart, Props.eggCount);
					}
				}

				// 为下一个卵重置。
				nextEggTick = RandomNextEggTick(thisTick);
			}
		}

		private static float GetCombinedEggSize(Pawn pawn)
		{
			return PregnancyHelper.GetCurrentEggSize(pawn);
		}

		[SyncMethod]
		private int RandomNextEggTick(int fromTick)
		{
			return fromTick + eggInterval.RandomInRange;
		}

		[SyncMethod]
		private static bool TryGetEgg(string defname, out HediffDef_InsectEgg result)
		{
			var possibleEggs = PossibleEggs(defname);

			if(RJWSettings.DevMode) ModLog.Message($"TryGetEgg: Possible eggs for {defname} are {possibleEggs.Select(egg => egg.defName).ToCommaList()}");
			
			return possibleEggs.TryRandomElement(out result);
		}

		public static IEnumerable<HediffDef_InsectEgg> PossibleEggs(string defName)
		{
			if (defName == null)
			{
				return Enumerable.Empty<HediffDef_InsectEgg>();
			}
			var eggOptions = DefDatabase<HediffDef_InsectEgg>.AllDefs.Where(eggDef => eggDef.CanHaveParent(defName)).ToList();

			// 若有其他可选，则移除未知的卵
			if(eggOptions.Count > 1)
			{
				eggOptions = eggOptions.Where(eggDef => eggDef.defName != "RJW_UnknownEgg").ToList();
			}
			return eggOptions;
		}

		[SyncMethod]
		public void AddEggs(HediffDef_InsectEgg eggDef, BodyPartRecord bodyPart, FloatRange range)
		{
			float eggScore = range.RandomInRange + 0.5f;

			if (RJWSettings.DevMode)
			{
				ModLog.Message($"Added egg count: {eggScore}, int: {(int)eggScore}");
				ModLog.Message($"Min egg count: {range.min}, int: {(int)range.min}");
				ModLog.Message($"Max egg count: {range.max}, int: {(int)range.max}");
			}

			int eggsToAdd = (int) eggScore;
			for (int i = 0; i < eggsToAdd; i++)
			{
				var addedEgg = parent.pawn.health.AddHediff(eggDef, bodyPart) as Hediff_InsectEgg;
				addedEgg?.InitImplanter(parent.pawn);
			}
		}

		public override void CompExposeData()
		{
			base.CompExposeData();
			Scribe_Values.Look(ref nextEggTick, "nextEggTick");
			Scribe_Values.Look(ref eggInterval, "eggInterval", defaultValue: Props.eggIntervalTicks);
		}
	}
}
