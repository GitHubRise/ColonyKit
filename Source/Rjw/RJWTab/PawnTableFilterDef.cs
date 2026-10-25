using System;
using System.Collections.Generic;
using Verse;

namespace rjw.MainTab;

public class PawnTableFilterDef : Def
{
    public Type? workerClass;

    private PawnTableFilterWorker? worker;

    public PawnTableFilterWorker? Worker
    {
        get
        {
            if (worker == null && workerClass != null)
                worker = (PawnTableFilterWorker)Activator.CreateInstance(workerClass);
            return worker;
        }
    }

    public IEnumerable<Pawn> Filter(IEnumerable<Pawn> allMapPawns)
    {
        return Worker?.Filter(allMapPawns) ?? allMapPawns;
    }
}
