using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveInfo : CustomMapComponent
{
  public CaveShapeDef caveShapeDef;
  public BiomeDef biomeDef;
  public CaveDef caveDef;
  public List<TileMutatorDef> mutators = new();
  public List<ThingDef> rockDefs = new();
  public CaveEntrance portalIntoCave;

  private Dictionary<string, RandCellsCache> randCellsCacheByKey = new();

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Defs.Look(ref caveShapeDef, "CF_CaveShapeDef");
    Scribe_Defs.Look(ref biomeDef, "CF_BiomeDef");
    Scribe_Defs.Look(ref caveDef, "CF_CaveDef");
    Scribe_Collections.Look(ref mutators, "CF_Mutators", LookMode.Def);
    Scribe_Collections.Look(ref rockDefs, "RockDefs", LookMode.Def);
    Scribe_References.Look(ref portalIntoCave, "portalIntoCave");

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      //fix generatorDef because the defName which vanilla uses to save is a template
      //read CaveMapUtility.cs for more info
      MapGeneratorDef fixedGeneratorDef = CaveMapUtility.BuildMapGeneratorDefFromParts(caveDef, caveShapeDef, biomeDef, mutators);
      if (fixedGeneratorDef == null)
      {
        Log.Warning("CF: failed to retrieve cavern details. Falling back to default values.");
        Log.Message(map.generatorDef.pocketMapProperties.biome);
        return;
      }
      if (portalIntoCave == null)
      {
        Log.Error("CF: Failed to retrieve portal into cave.");
      }

      base.map.generatorDef = fixedGeneratorDef;
    }
  }

  private class RandCellsCache
  {
    private readonly Map map;
    private readonly Predicate<IntVec3> validator;
    private readonly int wrapsUntilStale;

    private readonly List<IntVec3> cells = new();
    private int index;
    private int wrapsLeft;

    public int Count => cells.Count;
    public int Generation { get; private set; }

    public RandCellsCache(Map map, Predicate<IntVec3> validator, int wrapsUntilStale = 10)
    {
      this.map = map;
      this.validator = validator;
      this.wrapsUntilStale = wrapsUntilStale;
      Rebuild();
    }

    public void Rebuild()
    {
      cells.Clear();
      MapCellsInRandomOrder randMapCells = map.cellsInRandomOrder;
      for (int i = 0; i < map.Area; i++)
      {
        IntVec3 cell = randMapCells.Get(i);
        if (validator(cell))
          cells.Add(cell);
      }
      index = 0;
      wrapsLeft = wrapsUntilStale;
      Generation++;
    }

    public bool TryGetCell(bool removeCell, out IntVec3 result)
    {
      while (cells.Count > 0)
      {
        if (index >= cells.Count)
        {
          index = 0;
          wrapsLeft--;
          if (wrapsLeft <= 0)
          {
            Rebuild();
            if (cells.Count == 0)
              break;
          }
        }

        if (!validator(cells[index]))
        {
          cells.RemoveAt(index);
          continue;
        }

        result = cells[index];
        if (removeCell)
        {
          cells.RemoveAt(index);
        }
        else
        {
          index++;
        }
        return true;
      }
      result = IntVec3.Invalid;
      return false;
    }
  }

  public bool TryGetCellCountForRandCellsCache(string cacheKey, Predicate<IntVec3> validator, out int cellCount)
  {
    cellCount = EnsureRandCellsCache(cacheKey, validator).Count;
    return cellCount > 0;
  }

  public int GetRandCellsCacheGeneration(string cacheKey)
  {
    if (randCellsCacheByKey.TryGetValue(cacheKey, out RandCellsCache cache))
    {
      return cache.Generation;
    }
    return -1;
  }

  public void RebuildRandCellsCache(string cacheKey)
  {
    if (randCellsCacheByKey.TryGetValue(cacheKey, out RandCellsCache cache))
    {
      cache.Rebuild();
    }
  }

  public bool TryGetRandomCell(Predicate<IntVec3> validator, string cacheKey, out IntVec3 result, bool removeCellFromCache = false)
  {
    if (cacheKey == null)
    {
      return TryGetRandomCellUncached(validator, out result);
    }
    return EnsureRandCellsCache(cacheKey, validator).TryGetCell(removeCellFromCache, out result);
  }

  public bool TryGetRandomCellUncached(Predicate<IntVec3> validator, out IntVec3 result)
  {
    MapCellsInRandomOrder randMapCells = map.cellsInRandomOrder;
    for (int i = 0; i < map.Area; i++)
    {
      IntVec3 cell = randMapCells.Get(Rand.RangeInclusive(0, map.Area - 1));
      if (!validator(cell))
        continue;
      result = cell;
      return true;
    }
    result = IntVec3.Invalid;
    return false;
  }

  private RandCellsCache EnsureRandCellsCache(string cacheKey, Predicate<IntVec3> validator)
  {
    if (!randCellsCacheByKey.TryGetValue(cacheKey, out RandCellsCache cache))
    {
      cache = new RandCellsCache(map, validator);
      randCellsCacheByKey[cacheKey] = cache;
    }
    return cache;
  }

  public bool NotSolidPredicate(IntVec3 c)
  {
    Building edifice = c.GetEdifice(map);
    if (edifice != null && edifice.def.Fillage == FillCategory.Full)
    {
      return false;
    }
    return true;
  }

  public CaveInfo(Map map)
    : base(map) { }
}
