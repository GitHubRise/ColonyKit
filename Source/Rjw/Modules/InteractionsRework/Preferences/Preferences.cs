#nullable disable

using Verse;
using System.Collections.Generic;

namespace rjw.Modules.Interactions.Preferences
{
  public abstract class InteractionPreferences
  {

    public bool IsWhoring(Pawn pawn, Pawn partner, SexProps props)
    {
      return props.isWhoring && pawn == props.initiator; // 妓女始终是发起者。
    }

    public bool IsFuckingWhore(Pawn pawn, Pawn partner, SexProps props)
    {
      return props.isWhoring && pawn != props.initiator;
    }

    public bool IsRaping(Pawn pawn, Pawn partner, SexProps props)
    {
      return props.isRape && pawn == props.initiator;
    }

    public bool IsGettingRaped(Pawn pawn, Pawn partner, SexProps props)
    {
      return props.isRape && pawn != props.initiator;
    }
    public virtual bool IsActive(Pawn pawn, Pawn partner, SexProps props)
    {
      return false;
    }
    public virtual IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
    {
      yield break;
    }
  }
}
