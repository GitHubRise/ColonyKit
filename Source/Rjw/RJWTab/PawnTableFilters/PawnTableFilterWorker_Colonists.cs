using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rjw.MainTab;

public class PawnTableFilterWorker_Colonists : PawnTableFilterWorker
{
    public override IEnumerable<Pawn> Filter(IEnumerable<Pawn> allMapPawns)
    {
        return allMapPawns.Where(p =>
            p.RaceProps.Humanlike && p.IsColonist && !xxx.is_slave(p));
    }
}
