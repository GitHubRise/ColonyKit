using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class Exemptions
	{
		public List<string> BodyDefs;
		public List<string> Hediffs;
		public List<string> RaceDefs;
	}

	public class Needy : NeedDef
	{
		public Exemptions Exemptions;
	}

	public enum HygieneCategory : byte
	{
		Filthy,
		Grimy,
		Fine,
		Clean,
		Squeaky
	}

	public enum ContaminationLevel
	{
		Treated,
		Untreated,
		Contaminated
	}
}
