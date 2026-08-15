using System;
using Verse;

namespace CavesFramework;

public static class CaveGridUtility
{
  // csharpier-ignore
  public static int NeighborCount(
    IntVec3 cell,
    Map map,
    bool countOutOfBoundsCells,
    Predicate<IntVec3> match
  )
  {
    //this is -1 because we always end up counting ourselves with this method so this is to compensate
    int count = -1;
    for (int x = -1; x <= 1; x++)
    {
      for (int z = -1; z <= 1; z++)
      {
        IntVec3 targetCell = new IntVec3
        {
          x = cell.x + x,
          y = 0,
          z = cell.z + z,
        };

        if (!targetCell.InBounds(map))
        {
          if (countOutOfBoundsCells)
          {
            count++;
          }
          continue;
        }

        if (match(targetCell)) { count++; }
      }
    }
    return count;
  }

  public static MapGenFloatGrid CloneGrid(MapGenFloatGrid grid, Map map)
  {
    MapGenFloatGrid clone = new MapGenFloatGrid(map);
    foreach (IntVec3 allcell in map.AllCells)
    {
      clone[allcell] = grid[allcell];
    }
    return clone;
  }
}
