using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public class EffecterSpawnerConfig
{
  public readonly EffecterDef effecter;

  //fixed rate ignores how big the cave is
  public float? mtbHoursPerSpawn;

  //scales with cave size higher -> spawn more
  public float? spawnsPerHourPer10kEmptyCells;

  public IEnumerable<string> ConfigErrors()
  {
    //xor check
    if (mtbHoursPerSpawn.HasValue && spawnsPerHourPer10kEmptyCells.HasValue)
    {
      yield return "an effect has both mtbHoursPerSpawn and spawnsPerHourPer10kEmptyCells populated, they are mutually exclusive.";
    }
    if (!mtbHoursPerSpawn.HasValue && !spawnsPerHourPer10kEmptyCells.HasValue)
    {
      yield return "an effect doesn't specify mtbHoursPerSpawn nor spawnsPerHourPer10kEmptyCells.";
    }

    //meaningfull input check
    if (mtbHoursPerSpawn.HasValue && mtbHoursPerSpawn <= 0)
    {
      yield return "an effect's mtbHoursPerSpawn has a non positive value";
    }
    if (spawnsPerHourPer10kEmptyCells.HasValue && spawnsPerHourPer10kEmptyCells <= 0)
    {
      yield return "an effect's spawnsPerHourPer10kEmptyCells has a non positive value";
    }
    if (effecter == null)
    {
      yield return "an effect has a null def.";
    }
  }
}
