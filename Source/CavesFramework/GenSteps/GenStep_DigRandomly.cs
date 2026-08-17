using Verse;

namespace CavesFramework;

public class GenStep_DigRandomly : GenStep
{
  public FloatRange digPercentage = new FloatRange(0.55f, 0.55f);

  public override int SeedPart => 1506875925;

  public override void Generate(Map map, GenStepParams parms)
  {
    float fillPercentageInstance = digPercentage.RandomInRange;
    MapGenFloatGrid caves = MapGenerator.Caves;
    foreach (IntVec3 allcell in map.AllCells)
    {
      if (Rand.Chance(fillPercentageInstance))
      {
        caves[allcell] = CaveGridUtility.emptySpace;
      }
    }
  }
}
