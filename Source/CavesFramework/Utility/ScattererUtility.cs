using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CavesFramework;

public class RatioConfig
{
  public float rate = 0f;
  public bool isCompounding = false;
}

public class ExitDistanceWeighting : RatioConfig
{
  //mutates cache
  public bool TryPick(Map map, HashSet<IntVec3> candidatesCache, Predicate<IntVec3, Map> validator, out IntVec3 result)
  {
    result = IntVec3.Invalid;
    for (int i = 0; i < 1000; i++)
    {
      bool res = candidatesCache.TryRandomElementByWeight(c => Mathf.Max(0f, ScattererUtil.FactorAtCell(c, this)), out result);
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

public static class ScattererUtil
{
  public static float FactorAtCell(IntVec3 cell, RatioConfig conf)
  {
    MapGenFloatGrid distFromExit = MapGenerator.FloatGridNamed("DistanceFromExit");
    float dist = Mathf.Sqrt(distFromExit[cell]);

    if (!conf.isCompounding)
    {
      return 1f + conf.rate * dist;
    }
    else
    {
      return Mathf.Pow(Mathf.Max(1f + conf.rate, 0f), dist);
    }
  }
}
