using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CavesFramework;

public class GenStep_EnsureCavernRegionsConnection : GenStep
{
  private enum RegionState
  {
    Pending,
    Visited,
    Unreachable,
  }

  public override int SeedPart => 260373577;

  public int maxDistanceToConnect = int.MaxValue;
  public IntRange tunnelWidth = new IntRange(2, 6);
  public bool rerollTunnelWidthPerTunnel = true;

  public override void Generate(Map map, GenStepParams parms)
  {
    List<List<IntVec3>> regions = CaveGridUtility.GetCaveRegions(map);

    if (regions.NullOrEmpty())
    {
      return;
    }

    // regions is never mutated or reordered after this point, so indices into it
    // stay stable for the whole method - that's what makes distCache safe to reuse
    // across iterations.
    RegionState[] state = new RegionState[regions.Count];
    state[0] = RegionState.Visited;
    int remaining = regions.Count - 1;

    Dictionary<(int, int), (IntVec3, IntVec3, int)> distCache = new();

    bool logged = false; //avoid log spam
    while (remaining > 0)
    {
      int minDist = -1;
      IntVec3 cellRegion1 = new();
      IntVec3 cellRegion2 = new();
      int regionToVisit = -1;

      for (int i = 0; i < regions.Count; i++)
      {
        if (state[i] != RegionState.Visited)
        {
          continue;
        }

        for (int j = 0; j < regions.Count; j++)
        {
          if (state[j] != RegionState.Pending)
          {
            continue;
          }

          IntVec3 c1;
          IntVec3 c2;
          int d;

          if (distCache.TryGetValue((i, j), out var match))
          {
            (c1, c2, d) = match;
          }
          else
          {
            (c1, c2, d) = GetClosestCellsBetweenRegions(regions[i], regions[j]);
            distCache.Add((i, j), (c1, c2, d));
          }

          if (d < minDist || minDist == -1)
          {
            minDist = d;
            cellRegion1 = c1;
            cellRegion2 = c2;
            regionToVisit = j;
          }
        }
      }

      if (Mathf.RoundToInt(Mathf.Sqrt(minDist)) > maxDistanceToConnect || regionToVisit == -1)
      {
        if (!logged)
        {
          Log.Warning("CF: Couldn't connect all seperated cave regions. Try increasing the max distance");
          logged = true;
        }
        state[regionToVisit] = RegionState.Unreachable;
        remaining--;
        continue;
      }
      state[regionToVisit] = RegionState.Visited;
      remaining--;

      //dig here
    }
  }

  //might wanna make this a Util method
  private (IntVec3, IntVec3, int) GetClosestCellsBetweenRegions(List<IntVec3> r1, List<IntVec3> r2)
  {
    int minDistance = -1;
    IntVec3 cellFromRegion1 = new();
    IntVec3 cellFromRegion2 = new();
    foreach (var c1 in r1)
    {
      int currMinimum = -1;
      IntVec3 c = new();
      foreach (var c2 in r2)
      {
        int dist = c2.DistanceToSquared(c1);
        if (dist < currMinimum || currMinimum == -1)
        {
          currMinimum = dist;
          c = c2;
        }
      }
      if (currMinimum < minDistance || minDistance == -1)
      {
        minDistance = currMinimum;
        cellFromRegion1 = c1;
        cellFromRegion2 = c;
      }
    }
    return (cellFromRegion1, cellFromRegion2, minDistance);
  }
}
