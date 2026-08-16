using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CavesFramework;

public class GenStep_CloseIsolatedPockets : GenStep
{
  public int maxCellCountPocketToClose = 50;
  public float? maxMapPercentagePocketToClose;
  public int keepMinRegions = 1;

  public override int SeedPart => 304649386;

  private void FillRegionWithRock(List<IntVec3> region)
  {
    MapGenFloatGrid caves = MapGenerator.Caves;
    foreach (IntVec3 cell in region)
    {
      caves[cell] = CaveGridConstants.rock;
    }
  }

  public override void Generate(Map map, GenStepParams parms)
  {
    int closeUpTo = maxCellCountPocketToClose;
    if (maxMapPercentagePocketToClose.HasValue)
    {
      closeUpTo = Math.Max((int)((float)map.Area * maxMapPercentagePocketToClose.Value), maxCellCountPocketToClose);
    }

    List<List<IntVec3>> allRegions = CaveGridUtility.GetCaveRegions(map);
    List<List<IntVec3>> regionsBySize = allRegions.OrderByDescending(region => region.Count).ToList();

    foreach (var reg in regionsBySize.Skip(keepMinRegions))
    {
      if (reg.Count <= closeUpTo)
      {
        FillRegionWithRock(reg);
      }
    }
    if (keepMinRegions > allRegions.Count)
    {
      Log.Warning("CF: could not keep " + keepMinRegions + " regions because there were only " + allRegions.Count + " generated.");
    }
  }
}
