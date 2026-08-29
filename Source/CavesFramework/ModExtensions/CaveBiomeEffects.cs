using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public class CaveEffects : DefModExtension
{
  public List<EffecterDef> effectors;

  public override IEnumerable<string> ConfigErrors()
  {
    if (effectors.NullOrEmpty())
    {
      yield return "effects not specified.";
    }
  }
}
