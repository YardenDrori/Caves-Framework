using System;
using System.Collections.Generic;
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

  private class RandcellsCache
  {
    public List<IntVec3> cells;
    public int index;
    public int wrapsUntilStale;

    public RandcellsCache(List<IntVec3> cells, int wrapsUntilStale = 10)
    {
      this.cells = cells;
      this.wrapsUntilStale = wrapsUntilStale;
      index = 0;
    }
  }

  private Dictionary<string, RandcellsCache> randCellsCacheByKey = new();

  public bool TryGetRandomCell(Predicate<IntVec3> validator, string cacheKey, out IntVec3 result)
  {
    if (cacheKey != null && randCellsCacheByKey.TryGetValue(cacheKey, out RandcellsCache existingCache))
    {
      if (TryGetRandCellFromCache(existingCache, cacheKey, validator, out result))
      {
        return true;
      }
      randCellsCacheByKey.Remove(cacheKey);
    }

    MapCellsInRandomOrder randMapCells = map.cellsInRandomOrder;
    RandcellsCache cache = cacheKey != null ? new(new List<IntVec3>()) : null;
    if (cache != null)
    {
      randCellsCacheByKey.Add(cacheKey, cache);
    }

    for (int i = 0; i < map.Area; i++)
    {
      IntVec3 cell = randMapCells.Get(i);
      if (!validator(cell))
        continue;

      if (cache == null)
      {
        result = cell;
        return true;
      }
      cache.cells.Add(cell);
    }

    //if the cahce is empty then we went through the entire map finding nothing
    if (cache == null)
    {
      result = IntVec3.Invalid;
      return false;
    }
    //if the cache isnt empty but has no values then again we wnt through the entire map finding nothing
    if (cache.cells.Count < 1)
    {
      result = IntVec3.Invalid;
      return false;
    }

    result = cache.cells[cache.index++];
    return true;
  }

  private bool TryGetRandCellFromCache(RandcellsCache cache, string cacheKey, Predicate<IntVec3> validator, out IntVec3 result)
  {
    while (cache.cells.Count > 0)
    {
      if (cache.index >= cache.cells.Count)
      {
        cache.index = 0;
        cache.wrapsUntilStale--;
        if (cache.wrapsUntilStale == 0)
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
