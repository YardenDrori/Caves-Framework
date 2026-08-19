using RimWorld;
using Verse;

namespace CavesFramework;

public class GenStep_CavExitWithDistanceGrid : GenStep_PlaceCaveExit
{
  public bool skipCountingRocks = false;
  public bool usePathDistance = true;

  //for children of this class wanting different spawn behavior
  protected bool overrideVanilla = false;

  public override void Generate(Map map, GenStepParams parms)
  {
    if (!overrideVanilla)
    {
      base.Generate(map, parms);
    }

    if (!MapGenerator.PlayerStartSpotValid)
    {
      return;
    }
    IntVec3 exitPos = MapGenerator.PlayerStartSpot;
    MapGenFloatGrid caves = MapGenerator.Caves;

    MapGenFloatGrid distanceFromExit = MapGenerator.FloatGridNamed("DistanceFromExit");

    if (!usePathDistance)
    {
      foreach (var allcell in map.AllCells)
      {
        if (skipCountingRocks && CaveGridUtility.IsAnyRock(caves[allcell]))
        {
          distanceFromExit[allcell] = -1f;
          continue;
        }
        distanceFromExit[allcell] = allcell.DistanceToSquared(exitPos);
      }
      return;
    }

    map.floodFiller.FloodFill(exitPos, c => !CaveGridUtility.IsAnyRock(caves[c]), (IntVec3 c, int traversalDist) => distanceFromExit[c] = traversalDist * traversalDist);
    foreach (var allcell in map.AllCells)
    {
      bool isRock = CaveGridUtility.IsAnyRock(caves[allcell]);
      if (isRock)
      {
        if (skipCountingRocks)
        {
          distanceFromExit[allcell] = -1f;
        }
        else
        {
          //can't walk through rock, fall back to straight-line distance
          distanceFromExit[allcell] = allcell.DistanceToSquared(exitPos);
        }
        continue;
      }

      //flood fill never reached this cell (disconnected pocket) - only the root
      //cell can legitimately end up at 0 from the fill itself
      if (distanceFromExit[allcell] == 0f && allcell != exitPos)
      {
        distanceFromExit[allcell] = allcell.DistanceToSquared(exitPos);
      }
    }
  }
}
