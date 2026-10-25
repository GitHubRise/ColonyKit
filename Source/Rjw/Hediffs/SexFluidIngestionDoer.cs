#nullable disable

using Verse;

namespace rjw
{
	public abstract class SexFluidIngestionDoer
	{
		public abstract void Ingested(Pawn pawn, SexFluidDef fluid, float amount, ISexPartHediff fromPart, ISexPartHediff toPart = null);
	}
}
