using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CavesFramework;

public abstract class GenStep_CaveDecorScatterer : GenStep_Scatterer
{
  //==========How many==========
  // public int count = -1;
  // public FloatRange countPer10kCellsRange = FloatRange.Zero;
  public IntRange? countRange;

  //==========Where==========
  // public bool nearPlayerStart;
  // public bool nearMapCenter;
  public RatioConfigForRandCell commonalityRatePerCellFromExit = new();

  //==========LocationRules==========
  // public float minSpacing = 10f;
  // public bool spotMustBeStandable;
  // public int minDistToPlayerStart;
  // public float minDistToPlayerStartPct;
  // public int minEdgeDist;
  // public float minEdgeDistPct;
  // public int extraNoBuildEdgeDist;
  // public List<ScattererValidator> validators = new List<ScattererValidator>();
  // public List<ScattererValidator> fallbackValidators = new List<ScattererValidator>();
  // public bool allowFoggedPositions = true;
  // public bool allowRoofed = true;
  public int maxSpacing = int.MaxValue;
  public float maxEdgeDistPct = 1f;
  public int maxEdgeDist = int.MaxValue;
  public IntRange adjacentRockCells = new IntRange(0, 8);
  public List<TerrainDef> allowedTerrain = new();
  public List<TerrainDef> forbiddenTerrain = new();
  public List<ThingDef> allowedNearestWallDefs = new();
  public List<ThingDef> forbiddenNearestWallDefs = new();
  public int minDistFromWall = 0;
  public int maxDistFromWall = int.MaxValue;

  //==========MapRules==========
  // public bool allowInWaterBiome = true;
  // public bool onlyOnStartingMap;
  // public float minPollution;

  //==========InfoOnWhatsScattered==========
  // public bool isJunk;
  public List<Func<Map, float>> factorFunctions = new();

  //==========Misc==========
  // public bool warnOnFail = true;

  //==========IHonestlyDon'tKnowOrCare
  // public bool allowMechanoidDatacoreReadOrLost = true;

  //==========OVERRIDES AND PUBLIC API==========
  public override void Generate(Map map, GenStepParams parms)
  {
    if (
      !allowedTerrain.NullOrEmpty() && !forbiddenTerrain.NullOrEmpty()
      || !allowedNearestWallDefs.NullOrEmpty() && !forbiddenNearestWallDefs.NullOrEmpty()
    )
    {
      Log.Error("CF: Config error. Can't use forbidden terrain/nearestWallDef with allowed terrain/nearestWallDef at the same time.");
      return;
    }

    if (!commonalityRatePerCellFromExit.IsTerminating)
    {
      Log.Error("CF: Config error. chanceIncreasePerFailedAttempt must be greater than 0, the cell search can't finish without it.");
      return;
    }

    base.Generate(map, parms);
  }

  protected override bool TryFindScatterCell(Map map, out IntVec3 result)
  {
    while (true)
    {
      bool found = base.TryFindScatterCell(map, out result);
      if (!found)
      {
        return found;
      }
      if (commonalityRatePerCellFromExit.RollBasedOnFactorAtCell(result, map))
      {
        return found;
      }
    }
  }

  protected override bool CanScatterAt(IntVec3 loc, Map map)
  {
    if (usedSpots.Count > 0 && !NearUsedSpot(loc, CalculateFinalMaxSpacing(map)))
    {
      return false;
    }

    if (maxEdgeDist < int.MaxValue && !loc.CloseToEdge(map, maxEdgeDist))
    {
      return false;
    }

    if (maxEdgeDistPct < 1f && !loc.CloseToEdge(map, (int)(maxEdgeDistPct * (float)Mathf.Min(map.Size.x, map.Size.z))))
    {
      return false;
    }

    MapGenFloatGrid caves = MapGenerator.Caves;
    int neighboringRockCellsCount = CaveGridUtility.NeighborCount(
      loc,
      map,
      countOutOfBoundsCells: true,
      c => CaveGridUtility.IsWorkableRock(caves[c])
    );
    if (neighboringRockCellsCount < adjacentRockCells.min || neighboringRockCellsCount > adjacentRockCells.max)
    {
      return false;
    }

    if (
      !allowedTerrain.NullOrEmpty() && !allowedTerrain.Contains(loc.GetTerrain(map))
      || !forbiddenTerrain.NullOrEmpty() && forbiddenTerrain.Contains(loc.GetTerrain(map))
    )
    {
      return false;
    }

    int distanceFromWall = -1;
    ThingDef nearestSolidCellDef = null;
    map.floodFiller.FloodFill(
      loc,
      c => true,
      (c, d) =>
      {
        if (d > maxDistFromWall)
        {
          return true;
        }
        Thing edifice = c.GetEdifice(map);
        if (edifice != null && edifice.def.Fillage == FillCategory.Full)
        {
          distanceFromWall = d;
          nearestSolidCellDef = edifice.def;
          return true;
        }
        return false;
      }
    );
    if (
      distanceFromWall == -1
      || distanceFromWall > maxDistFromWall
      || distanceFromWall < minDistFromWall
      || !allowedNearestWallDefs.NullOrEmpty() && !allowedNearestWallDefs.Contains(nearestSolidCellDef)
      || !forbiddenNearestWallDefs.NullOrEmpty() && forbiddenNearestWallDefs.Contains(nearestSolidCellDef)
    )
    {
      return false;
    }

    return base.CanScatterAt(loc, map);
  }

  protected override int CalculateFinalCount(Map map)
  {
    if (countRange.HasValue)
    {
      return Mathf.RoundToInt(countRange.Value.RandomInRange * GetPlacementFactor(map));
    }
    return base.CalculateFinalCount(map);
  }

  protected override float GetPlacementFactor(Map map)
  {
    float factor = base.GetPlacementFactor(map);
    foreach (var func in factorFunctions)
    {
      factor *= func(map);
    }
    return factor;
  }

  public virtual float CalculateFinalMaxSpacing(Map map)
  {
    float placementFactor = GetPlacementFactor(map);
    if (placementFactor <= 0f)
    {
      return 0f;
    }

    return maxSpacing * placementFactor;
  }

  //==========IMPLEMENTATION DETAILS==========
  protected bool WillObstructingCellBlockPassage(IntVec3 cell, Map map)
  {
    throw new NotImplementedException();
  }
}
