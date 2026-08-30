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
  public List<TileMutatorDef> mutators;
  public List<ThingDef> rockDefs;
  public CaveEntrance portalIntoCave;

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Defs.Look(ref caveShapeDef, "CF_CaveShapeDef");
    Scribe_Defs.Look(ref biomeDef, "CF_BiomeDef");
    Scribe_Defs.Look(ref caveDef, "CF_CaveDef");
    Scribe_Collections.Look(ref mutators, "CF_Mutators", LookMode.Def);
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
    public List<IntVec3> cells;
    public int index;
    public int wrapsUntilStale;

    public RandCellsCache(List<IntVec3> cells, int wrapsUntilStale = 10)
    {
      this.cells = cells;
      this.wrapsUntilStale = wrapsUntilStale;
      index = 0;
    }
  }

  private Dictionary<string, RandCellsCache> randCellsCacheByKey = new();

  public bool TryGetCellCountForRandCellsCache(string cacheKey, Predicate<IntVec3> validator, out int cellCount)
  {
    RandCellsCache cache = EnsureRandCellsCache(cacheKey, validator);
    cellCount = cache.cells.Count;
    return cellCount > 0;
  }

  public bool TryGetRandomCell(Predicate<IntVec3> validator, string cacheKey, out IntVec3 result)
  {
    if (cacheKey != null && randCellsCacheByKey.TryGetValue(cacheKey, out RandCellsCache existingCache))
    {
      if (TryGetRandCellFromCache(existingCache, cacheKey, validator, out result))
        return true;

      randCellsCacheByKey.Remove(cacheKey);
    }

    if (cacheKey == null)
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

    RandCellsCache cache = EnsureRandCellsCache(cacheKey, validator);
    if (cache.cells.Count < 1)
    {
      result = IntVec3.Invalid;
      randCellsCacheByKey.Remove(cacheKey);
      return false;
    }

    result = cache.cells[cache.index++];
    return true;
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

  private RandCellsCache EnsureRandCellsCache(string cacheKey, Predicate<IntVec3> validator)
  {
    if (randCellsCacheByKey.TryGetValue(cacheKey, out RandCellsCache cache))
      return cache;

    MapCellsInRandomOrder randMapCells = map.cellsInRandomOrder;
    List<IntVec3> cells = new List<IntVec3>();
    for (int i = 0; i < map.Area; i++)
    {
      IntVec3 cell = randMapCells.Get(i);
      if (validator(cell))
        cells.Add(cell);
    }

    cache = new RandCellsCache(cells);
    randCellsCacheByKey[cacheKey] = cache;
    return cache;
  }

  private bool TryGetRandCellFromCache(RandCellsCache cache, string cacheKey, Predicate<IntVec3> validator, out IntVec3 result)
  {
    while (cache.cells.Count > 0)
    {
      if (cache.index >= cache.cells.Count)
      {
        cache.index = 0;
        cache.wrapsUntilStale--;
        if (cache.wrapsUntilStale <= 0)
        {
          randCellsCacheByKey.Remove(cacheKey);
          result = IntVec3.Invalid;
          return false;
        }
      }

      if (!validator(cache.cells[cache.index]))
      {
        cache.cells.RemoveAt(cache.index);
        continue;
      }

      result = cache.cells[cache.index];
      cache.index++;
      return true;
    }
    result = IntVec3.Invalid;
    return false;
  }

  public CaveInfo(Map map)
    : base(map) { }
}
