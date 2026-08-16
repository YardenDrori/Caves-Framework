using System;
using System.Collections.Generic;
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
    int count = 0;
    for (int x = -1; x <= 1; x++)
    {
      for (int z = -1; z <= 1; z++)
      {
        if (x == 0 && z == 0) { continue; }

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

  public static List<List<IntVec3>> GetCaveRegions(Map map)
  {
    MapGenFloatGrid caves = MapGenerator.Caves;

    HashSet<IntVec3> visited = new();

    List<List<IntVec3>> allRegions = new();

    foreach (IntVec3 allcell in map.AllCells)
    {
      if (visited.Contains(allcell))
      {
        continue;
      }
      visited.Add(allcell);

      List<IntVec3> region = new();

      map.floodFiller.FloodFill(
        allcell,
        c =>
        {
          return !CaveGridConstants.IsAnyRock(caves[c]);
        },
        c =>
        {
          region.Add(c);
          visited.Add(c);
          return false;
        }
      );

      if (region.NullOrEmpty())
      {
        continue;
      }

      allRegions.Add(region);
    }

    return allRegions;
  }
}
