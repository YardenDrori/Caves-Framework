using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public class CaveEffects : DefModExtension
{
  public List<EffecterSpawnerConfig> effectsToSpawn;

  public override IEnumerable<string> ConfigErrors()
  {
    if (effectsToSpawn.NullOrEmpty())
    {
      yield return "effects not specified.";
      yield break;
    }
    foreach (EffecterSpawnerConfig effect in effectsToSpawn)
    {
      foreach (string err in effect.ConfigErrors())
      {
        yield return err;
      }
    }
  }
}
