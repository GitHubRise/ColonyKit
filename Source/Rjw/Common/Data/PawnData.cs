#nullable disable

using System;
using Verse;
using System.Linq;
using RimWorld;
using System.Collections.Generic;

namespace rjw
{
	/// <summary>
	/// 工具数据对象，以及一组针对 Pawn 的扩展方法
	/// </summary>
	public class PawnData : IExposable
	{
		public Pawn Pawn = null;
		public bool Comfort = false;
		public bool Service = false;
		public bool Breeding = false;
		public bool Milking = false;
		public bool Hero = false;
		public bool Ironman = false;
		public string HeroOwner = "";
		public bool BreedingAnimal = false;

		public bool CanChangeDesignationColonist = false;
		public bool CanChangeDesignationPrisoner = false;
		public bool CanDesignateService = false;
		public bool CanDesignateMilking = false;
		public bool CanDesignateComfort = false;
		public bool CanDesignateBreedingAnimal = false;
		public bool CanDesignateBreeding = false;
		public bool CanDesignateHero = false;

		public bool isSlime = false;
		public bool isDemon = false;
		public bool oviPregnancy = false;
		public bool sexualized = false;

		public float raceSexDrive = 1.0f;

		public SexProps SexProps;// 用于右键（RMB）交互缓存的临时变量

		public List<Hediff> genitals = new List<Hediff>();
		public List<Hediff> breasts = new List<Hediff>();
		public List<Hediff> anus = new List<Hediff>();
		public List<Hediff> torso = new List<Hediff>();

		public RaceGroupDef RaceSupportDef
		{
			get
			{
				if (raceSupportDef == null)
					RaceGroupDef_Helper.TryGetRaceGroupDef(Pawn, out raceSupportDef);

				return raceSupportDef;
			}
		}

		public RaceGroupDef raceSupportDef = null;


		public PawnData(Pawn pawn)
		{
			//ModLog.Message("Creating pawndata for " + pawn);
			Pawn = pawn;
			//ModLog.Message("This data is valid " + this.IsValid);
		}

		public void Initialize()
		{
			if (RaceGroupDef_Helper.TryGetRaceGroupDef(Pawn, out var raceGroupDef))
			{
				raceSupportDef = raceGroupDef;
				//ModLog.Message("RaceSupportDef " + RaceSupportDef);
				oviPregnancy = raceGroupDef.oviPregnancy;
				raceSexDrive = raceGroupDef.raceSexDrive;
			}

			isDemon = Pawn.Has(RaceTagDefOf.Demon);
			isSlime = Pawn.Has(RaceTagDefOf.Slime);
			//ModLog.Warning("PawnData:: Pawn:" + xxx.get_pawnname(pawn));
			//ModLog.Warning("PawnData:: isSlime:" + isSlime);
			//ModLog.Warning("PawnData:: isDemon:" + isDemon);
			//ModLog.Warning("PawnData:: oviPregnancy:" + oviPregnancy);
			
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref Comfort, "Comfort", false, true);
			Scribe_Values.Look(ref Service, "Service", false, true);
			Scribe_Values.Look(ref Breeding, "Breeding", false, true);
			Scribe_Values.Look(ref Milking, "Milking", false, true);
			Scribe_Values.Look(ref Hero, "Hero", false, true);
			Scribe_Values.Look(ref Ironman, "Ironman", false, true);
			Scribe_Values.Look(ref HeroOwner, "HeroOwner", "", true);
			Scribe_Values.Look(ref BreedingAnimal, "BreedingAnimal", false, true);
			Scribe_Values.Look(ref CanChangeDesignationColonist, "CanChangeDesignationColonist", false, true);
			Scribe_Values.Look(ref CanChangeDesignationPrisoner, "CanChangeDesignationPrisoner", false, true);
			Scribe_Values.Look(ref CanDesignateService, "CanDesignateService", false, true);
			Scribe_Values.Look(ref CanDesignateMilking, "CanDesignateMilking", false, true);
			Scribe_Values.Look(ref CanDesignateComfort, "CanDesignateComfort", false, true);
			Scribe_Values.Look(ref CanDesignateBreedingAnimal, "CanDesignateBreedingAnimal", false, true);
			Scribe_Values.Look(ref CanDesignateBreeding, "CanDesignateBreeding", false, true);
			Scribe_Values.Look(ref CanDesignateHero, "CanDesignateHero", false, true);
			Scribe_Values.Look(ref isSlime, "isSlime", false, true);
			Scribe_Values.Look(ref isDemon, "isDemon", false, true);
			Scribe_Values.Look(ref oviPregnancy, "oviPregnancy", false, true);
			Scribe_Values.Look(ref sexualized, "sexualized", false, true);
			Scribe_Values.Look(ref raceSexDrive, "raceSexDrive", 1.0f, true);
			Scribe_Defs.Look(ref raceSupportDef, "RaceSupportDef");

			//ModLog.Message($"Scribed PawnData for {Pawn}, {Scribe.mode} mode");

			if (Scribe.mode == LoadSaveMode.PostLoadInit && Pawn != null)
			{
				if (Hero) DesignatorsData.rjwHero.AddDistinct(Pawn);
				if (Comfort) DesignatorsData.rjwComfort.AddDistinct(Pawn);
				if (Service) DesignatorsData.rjwService.AddDistinct(Pawn);
				if (Milking) DesignatorsData.rjwMilking.AddDistinct(Pawn);
				if (Breeding) DesignatorsData.rjwBreeding.AddDistinct(Pawn);
				if (BreedingAnimal) DesignatorsData.rjwBreedingAnimal.AddDistinct(Pawn);
			}
	}

	public bool IsValid { get { return Pawn != null; } }
	}
}
