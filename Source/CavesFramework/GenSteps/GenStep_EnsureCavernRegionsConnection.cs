using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

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

  public bool rerollTunnelParamatersPerTunnel = true;
  public FloatRange tunnelWidth = new FloatRange(2, 6);
  public FloatRange windiness = new FloatRange(6, 6);
  public FloatRange deadEndBranchChance = new FloatRange(0f, 0f);
  public FloatRange narrowingRate = new FloatRange(0.01f, 0.034f);

  public override void Generate(Map map, GenStepParams parms)
  {
    List<List<IntVec3>> regions = CaveGridUtility.GetCaveRegions(map);
    List<IntVec3> diggableCells = map.AllCells.Except(regions.SelectMany(l => l)).ToList();

    float constWidth = tunnelWidth.RandomInRange;
    MapGenCavesUtility.CaveGenParms caveParmsConst = MapGenCavesUtility.CaveGenParms.Default;
    caveParmsConst.directionChangeSpeed = windiness.RandomInRange;
    caveParmsConst.branchChance = deadEndBranchChance.RandomInRange;
    caveParmsConst.widthOffsetPerCell = narrowingRate.RandomInRange;

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

      float width;
      MapGenCavesUtility.CaveGenParms caveParms;
      if (rerollTunnelParamatersPerTunnel)
      {
        width = tunnelWidth.RandomInRange;
        caveParms = caveParmsConst;
        caveParmsConst.directionChangeSpeed = windiness.RandomInRange;
        caveParmsConst.branchChance = deadEndBranchChance.RandomInRange;
        caveParmsConst.widthOffsetPerCell = narrowingRate.RandomInRange;
      }
      else
      {
        width = constWidth;
        caveParms = caveParmsConst;
      }

      Vector3 vect1 = cellRegion1.ToVector3();
      Vector3 vect2 = cellRegion2.ToVector3();
      float angle = vect1.AngleToFlat(vect2);
      ModuleBase directionNoise = new Perlin(0.00205, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);
      MapGenCavesUtility.Dig(
        cellRegion1,
        angle,
        width,
        diggableCells,
        map,
        closed: false,
        directionNoise,
        caveParms,
        cell =>
        {
          return CaveGridUtility.IsWorkableRock(MapGenerator.Caves[cell]);
        }
      );
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
