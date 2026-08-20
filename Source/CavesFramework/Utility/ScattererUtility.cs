using UnityEngine;
using Verse;

namespace CavesFramework;

public class RatioConfig
{
  public float rate = 0f;
  public bool isCompounding = false;

  protected float maxFactor = -1f;

  private float[] factorCache;
  protected int cachedMapId = -1;

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

  protected void BuildCache(Map map)
  {
    MapGenFloatGrid distFromExit = MapGenerator.FloatGridNamed("DistanceFromExit");
    CellIndices cellIndices = map.cellIndices;
    maxFactor = 0;

    factorCache = new float[cellIndices.NumGridCells];
    foreach (IntVec3 c in map.AllCells)
    {
      float dist = Mathf.Sqrt(distFromExit[c]);
      float factor = isCompounding ? Mathf.Pow(Mathf.Max(1f + rate, 0f), dist) : 1f + rate * dist;
      factor = Mathf.Max(0f, factor);

      if (factor > maxFactor)
      {
        maxFactor = factor;
      }

      factorCache[cellIndices.CellToIndex(c)] = factor;
    }
    cachedMapId = map.uniqueID;
  }
}

public class RatioConfigForRandCell : RatioConfig
{
  public float chanceIncreasePerFailedAttempt = 0.001f;

  private float currentChanceAccumulated = 0f;

  public bool IsTerminating => chanceIncreasePerFailedAttempt > 0f;

  public bool RollBasedOnFactorAtCell(IntVec3 cell, Map map)
  {
    //no weighting configured, so every cell is equally good and maxFactor is never built
    if (rate == 0f)
    {
      return true;
    }

    //has to run before we read maxFactor, it's the call that builds it
    float factor = FactorAtCell(cell, map);

    //every cell clamped to 0 (rate <= -1), nothing left to weight by
    if (maxFactor <= 0f)
    {
      return true;
    }

    bool res = Rand.Chance(factor / maxFactor + currentChanceAccumulated);
    if (res)
    {
      currentChanceAccumulated = 0f;
    }
    else
    {
      currentChanceAccumulated += chanceIncreasePerFailedAttempt;
    }
    return res;
  }
}
