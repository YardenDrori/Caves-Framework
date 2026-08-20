using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CavesFramework;

public class RatioConfig
{
  public float rate = 0f;
  public bool isCompounding = false;

  private float[] factorCache;
  private int cachedMapId = -1;

  public float FactorAtCell(IntVec3 cell, Map map)
  {
    if (rate == 0f)
    {
      return 1f;
    }

    if (factorCache == null || cachedMapId != map.uniqueID)
    {
      BuildCache(map);
    }

    return factorCache[map.cellIndices.CellToIndex(cell)];
  }

  private void BuildCache(Map map)
  {
    MapGenFloatGrid distFromExit = MapGenerator.FloatGridNamed("DistanceFromExit");
    CellIndices cellIndices = map.cellIndices;

    factorCache = new float[cellIndices.NumGridCells];
    foreach (IntVec3 c in map.AllCells)
    {
      float dist = Mathf.Sqrt(distFromExit[c]);
      float factor = isCompounding ? Mathf.Pow(Mathf.Max(1f + rate, 0f), dist) : 1f + rate * dist;

      factorCache[cellIndices.CellToIndex(c)] = Mathf.Max(0f, factor);
    }
    cachedMapId = map.uniqueID;
  }
}

public class RatioConfigCellPicker : RatioConfig
{
  public bool TryPick(Map map, HashSet<IntVec3> candidatesCache, Predicate<IntVec3, Map> validator, out IntVec3 result)
  {
    result = IntVec3.Invalid;
    for (int i = 0; i < 1000; i++)
    {
      bool res = candidatesCache.TryRandomElementByWeight(c => FactorAtCell(c, map), out result);
      if (!res)
      {
        return false;
      }
      if (!validator(result, map))
      {
        candidatesCache.Remove(result);
        continue;
      }
      return res;
    }
    return false;
  }
}
