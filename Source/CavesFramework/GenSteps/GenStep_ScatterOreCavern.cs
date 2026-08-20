using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CavesFramework;

public class OreWhitelistEntry
{
  public ThingDef oreDef;

  // default state means fallback to vanilla's def commonality as the weight
  public float weight = 0f;
}

public class GenStep_ScatterOreCavern : GenStep_ScatterLumpsMineable
{
  public IntRange? veinSizeOverride;
  public FloatRange? veinSizeMultiplier;

  public RatioConfigForRandCell commonalityRatePerCellFromExit = new();
  public RatioConfig veinSizeMultPerCellFromExit = new();

  public bool mustBeBuriedInRock = false;
  public bool mustBeExposedToAir = false; //guarantees at least one cell, not all cells
  public bool allowVeinsNearMapEdge = false;
  public float minSpacingBetweenVeins = 1.5f;

  public List<OreWhitelistEntry> oreWhitelist = new();

  public override void Generate(Map map, GenStepParams parms)
  {
    if (mustBeBuriedInRock && mustBeExposedToAir)
    {
      Log.Error("CF: config error. both mustBeBurriedInRock and mustBeExposedToAir are set to true, they are mutually exclusive.");
      return;
    }

    if (!commonalityRatePerCellFromExit.IsTerminating)
    {
      Log.Error("CF: config error. chanceIncreasePerFailedAttempt must be greater than 0, the cell search can't finish without it.");
      return;
    }

    usedSpots.Clear();
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

  protected override ThingDef ChooseThingDef()
  {
    if (forcedDefToScatter != null)
    {
      if (oreWhitelist.Count == 0)
      {
        //we modify the def but this is fine as we're essentially just converting
        //the incorrect xml to the expected format
        oreWhitelist.Add(new OreWhitelistEntry { oreDef = forcedDefToScatter });
        forcedDefToScatter = null;
      }
      else
      {
        Log.Error("CF: use of forcedDefToScatter and oreWhitelist is not valid. Falling back to oreWhitelist");
        forcedDefToScatter = null;
      }
    }

    if (
      !oreWhitelist.NullOrEmpty()
      && oreWhitelist.TryRandomElementByWeight(
        d =>
        {
          if (d.weight != 0)
          {
            return d.weight;
          }
          else
          {
            return (d.oreDef == null || d.oreDef.building == null || d.oreDef.building.mineableThing == null)
              ? 0f
              : d.oreDef.building.mineableScatterCommonality;
          }
        },
        out OreWhitelistEntry entry
      )
    )
    {
      return entry.oreDef;
    }

    ThingDef returnDef = base.ChooseThingDef();
    return returnDef;
  }

  protected override bool CanScatterAt(IntVec3 c, Map map)
  {
    if (mustBeBuriedInRock && !IsSurroundedByRock(c, map, allowVeinsNearMapEdge))
    {
      return false;
    }
    if (mustBeExposedToAir && IsSurroundedByRock(c, map, outOfBoundsCountsAsRock: true))
    {
      return false;
    }

    return base.CanScatterAt(c, map);
  }

  //we have to re-implement this method to modify the validator
  protected override void ScatterAt(IntVec3 c, Map map, GenStepParams parms, int stackCount = 1)
  {
    var (thingDef, numCells) = GetLumpDefAndSize(c, map);
    if (thingDef == null || numCells == 0)
    {
      return;
    }

    recentLumpCells.Clear();
    List<CellRect> usedRects = MapGenerator.GetOrGenerateVar<List<CellRect>>("UsedRects");
    MapGenFloatGrid caves = MapGenerator.Caves;

    foreach (IntVec3 cell in GridShapeMaker.IrregularLump(c, map, numCells, Validator))
    {
      GenSpawn.Spawn(thingDef, cell, map);
      caves[cell] = CaveGridUtility.ore;
      recentLumpCells.Add(cell);
      usedSpots.Add(cell);
    }

    bool Validator(IntVec3 cell)
    {
      if (!usedRects.Any((CellRect x) => x.Contains(cell)))
      {
        if (Current.ProgramState == ProgramState.MapInitializing)
        {
          //we don't check mustBeExposedToAir because we only care about some of it being
          //exposed which we check via the root of the lump doing the check here as well
          //will generate flat wide ore veins which is bad
          if (mustBeBuriedInRock && !IsSurroundedByRock(cell, map, allowVeinsNearMapEdge))
          {
            return false;
          }

          return CaveGridUtility.IsWorkableRock(caves[cell]);
        }
        return true;
      }
      return false;
    }
  }

  public override float CalculateFinalMinSpacing(Map map)
  {
    base.minSpacing = minSpacingBetweenVeins;
    return base.CalculateFinalMinSpacing(map);
  }

  private bool IsSurroundedByRock(IntVec3 cell, Map map, bool outOfBoundsCountsAsRock)
  {
    MapGenFloatGrid caves = MapGenerator.Caves;
    IntVec3[] adjacentCells = GenAdj.AdjacentCells; //we care about the corners

    foreach (var offset in adjacentCells)
    {
      IntVec3 adjCell = cell + offset;

      if (!adjCell.InBounds(map))
      {
        if (outOfBoundsCountsAsRock)
          continue;
        return false;
      }

      if (!CaveGridUtility.IsAnyRock(caves[adjCell]))
      {
        return false;
      }
    }
    return true;
  }

  private (ThingDef, int) GetLumpDefAndSize(IntVec3 cell, Map map)
  {
    float ratio = veinSizeMultPerCellFromExit.FactorAtCell(cell, map);
    ThingDef oreDef = ChooseThingDef();
    if (oreDef == null)
    {
      return (null, 0);
    }

    if (forcedLumpSize != 0)
    {
      if (veinSizeOverride.HasValue)
      {
        Log.Error("CF: config error. both veinSizeOverride and forcedLumpSize have been set, they are mutually exclusive.");
      }
      else if (veinSizeMultiplier.HasValue)
      {
        Log.Error("CF: config error. both veinSizeMultiplier and forcedLumpSize have been set, they are mutually exclusive.");
      }
      else
      {
        //while it isnt the best way to force a lump size (our IntRange is)
        //this doesnt cause any issues so no reason to log
        return (oreDef, Mathf.Max(Mathf.RoundToInt(forcedLumpSize * ratio), 1));
      }
      forcedLumpSize = 0;
    }

    if (veinSizeOverride.HasValue)
    {
      return (oreDef, Mathf.Max(Mathf.RoundToInt(veinSizeOverride.Value.RandomInRange), 1));
    }

    int num = oreDef.building.mineableScatterLumpSizeRange.RandomInRange;
    if (!veinSizeMultiplier.HasValue)
    {
      return (oreDef, num);
    }

    float mult = veinSizeMultiplier.Value.RandomInRange;
    return (oreDef, Math.Max(Mathf.RoundToInt(num * mult * ratio), 1));
  }
}
