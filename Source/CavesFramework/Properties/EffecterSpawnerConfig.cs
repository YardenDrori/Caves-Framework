using Verse;

namespace CavesFramework;

public class EffecterSpawnerConfig
{
  public EffecterDef effecter;

  //fixed rate, ignores how big the cave is
  public float? mtbHoursPerSpawn;

  //scales with cave size, higher -> spawn more
  public float? spawnsPerHourPer10kEmptyCells;
}
