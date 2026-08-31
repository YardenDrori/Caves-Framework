using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveEffectsHandler : CustomMapComponent
{
  protected List<EffecterSpawnerConfig> effectors = new();
  protected const string RandCellsCacheKey = "NonFullCell";
  private const int TicksPerSpawnAttempt = 40;

  private CaveInfo _caveInfo = null;
  protected CaveInfo CaveInfo =>
    _caveInfo ??= map.GetComponent<CaveInfo>() ?? throw new InvalidOperationException($"CF: map {map.uniqueID} has no CaveInfo.");

  //cached so we don't allocate a delegate on every cell query
  private Predicate<IntVec3> _notSolidPredicate = null;
  protected Predicate<IntVec3> NotSolidPredicate => _notSolidPredicate ??= CaveInfo.NotSolidPredicate;

  public override void FinalizeInit()
  {
    base.FinalizeInit();

    BiomeDef biome = CaveInfo.biomeDef;
    CaveEffects effects = biome.GetModExtension<CaveEffects>();

    effectors.AddRange(effects.effectsToSpawn);
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    if (Find.CurrentMap == map && map.IsHashIntervalTick(TicksPerSpawnAttempt, 1)) // each map comp gets a different offset val
    {
      AddEffects();
    }
  }

  protected virtual void AddEffects()
  {
    //only the size scaled effects need this and it's the same for all of them, so fetch at most once
    int emptyCellCount = -1;

    foreach (EffecterSpawnerConfig effect in effectors)
    {
      if (effect.spawnsPerHourPer10kEmptyCells.HasValue && emptyCellCount < 0)
      {
        CaveInfo.TryGetCellCountForRandCellsCache(RandCellsCacheKey, NotSolidPredicate, out emptyCellCount);
      }

      if (ShouldSpawnEffect(effect, emptyCellCount) && CaveInfo.TryGetRandomCell(NotSolidPredicate, RandCellsCacheKey, out IntVec3 result))
      {
        effect.effecter.SpawnMaintained(result, map);
      }
    }
  }

  protected virtual bool ShouldSpawnEffect(EffecterSpawnerConfig effect, int cellCount)
  {
    //no empty cells means the divisor is 0, which gives an infinite mtb, which never fires
    float effectiveMtbHours = effect.mtbHoursPerSpawn ?? (10000f / (effect.spawnsPerHourPer10kEmptyCells.Value * cellCount));
    return Rand.MTBEventOccurs(effectiveMtbHours, GenDate.TicksPerHour, TicksPerSpawnAttempt);
  }

  public CaveEffectsHandler(Map map)
    : base(map) { }
}
