using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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

      List<IntVec3> region = GetRegion(
        allcell,
        map,
        c =>
        {
          return !CaveGridConstants.IsAnyRock(caves[c]);
        }
      );
      if (!region.NullOrEmpty())
      {
        foreach (var cell in region)
        {
          visited.Add(cell);
        }
        allRegions.Add(region);
      }
    }

    return allRegions;
  }

  public static List<IntVec3> GetRegion(IntVec3 root, Map map, Predicate<IntVec3> isPartOfRegion, bool spreadThroughCorners = false)
  {
    if (!isPartOfRegion(root))
    {
      return null;
    }

    //we have a list for return and a hashset for performance for contains checks
    List<IntVec3> region = new();
    HashSet<IntVec3> visited = new();
    region.Add(root);
    visited.Add(root);

    Queue<IntVec3> queue = new Queue<IntVec3>();
    queue.Enqueue(root);

    while (queue.Count > 0)
    {
      IntVec3 cell = queue.Dequeue();

      for (int x = -1; x <= 1; x++)
      {
        for (int z = -1; z <= 1; z++)
        {
          if (x == 0 && z == 0 || !spreadThroughCorners && x != 0 && z != 0)
          {
            continue;
          }

          IntVec3 currCell = new IntVec3(cell.x + x, cell.y, cell.z + z);
          if (visited.Contains(currCell))
          {
            continue;
          }

          if (!currCell.InBounds(map))
          {
            continue;
          }

          if (!isPartOfRegion(currCell))
          {
            continue;
          }

          visited.Add(currCell);
          region.Add(currCell);
          queue.Enqueue(currCell);
        }
      }
    }
    return region;
  }
}
