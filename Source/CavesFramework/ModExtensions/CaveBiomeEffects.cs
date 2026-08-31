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
    }
    foreach (EffecterSpawnerConfig effect in effectsToSpawn)
    {
      //xor check
      if (effect.mtbHoursPerSpawn.HasValue && effect.spawnsPerHourPer10kEmptyCells.HasValue)
      {
        yield return "an effect has both mtbHoursPerSpawn and spawnsPerHourPer10kEmptyCells populated, they are mutually exclusive.";
      }
      if (!effect.mtbHoursPerSpawn.HasValue && !effect.spawnsPerHourPer10kEmptyCells.HasValue)
      {
        yield return "an effect doesn't specify mtbHoursPerSpawn nor spawnsPerHourPer10kEmptyCells.";
      }

      //meaningfull input check
      if (effect.mtbHoursPerSpawn.HasValue && effect.mtbHoursPerSpawn <= 0)
      {
        yield return "an effect's mtbHoursPerSpawn has a non positive value";
      }
      if (effect.spawnsPerHourPer10kEmptyCells.HasValue && effect.spawnsPerHourPer10kEmptyCells <= 0)
      {
        yield return "an effect's spawnsPerHourPer10kEmptyCells has a non positive value";
      }
      if (effect.effecter == null)
      {
        yield return "an effect has a null def.";
      }
    }
  }
}
