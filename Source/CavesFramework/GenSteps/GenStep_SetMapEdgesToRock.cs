using Verse;

namespace CavesFramework;

public class GenStep_SetMapEdgesToRock : GenStep
{
  public FloatRange thickness = new FloatRange(1f, 1f);

  public override int SeedPart => 952084420;

  public override void Generate(Map map, GenStepParams parms)
  {
    float thicknessInstance = thickness.RandomInRange;
    int guaranteedThickness = (int)thicknessInstance;
    float additionalThicknessChance = thicknessInstance - guaranteedThickness;
    MapGenFloatGrid caves = MapGenerator.Caves;
    foreach (IntVec3 allcell in map.AllCells)
    {
      if (allcell.CloseToEdge(map, guaranteedThickness))
      {
        caves[allcell] = CaveGridUtility.border;
      }
      else if (allcell.CloseToEdge(map, guaranteedThickness + 1) && Rand.Chance(additionalThicknessChance))
      {
        caves[allcell] = CaveGridUtility.border;
      }
    }
  }
}
