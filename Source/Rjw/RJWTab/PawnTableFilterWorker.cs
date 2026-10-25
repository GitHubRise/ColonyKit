using System.Collections.Generic;
using Verse;

namespace rjw.MainTab;

public abstract class PawnTableFilterWorker
{
    public abstract IEnumerable<Pawn> Filter(IEnumerable<Pawn> allMapPawns);
}
