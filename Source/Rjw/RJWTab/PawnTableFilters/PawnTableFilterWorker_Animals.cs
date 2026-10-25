using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace rjw.MainTab;

public class PawnTableFilterWorker_Animals : PawnTableFilterWorker
{
    public override IEnumerable<Pawn> Filter(IEnumerable<Pawn> allMapPawns)
    {
        return allMapPawns.Where(p =>
            p.Faction == Faction.OfPlayer && xxx.is_animal(p));
    }
}
