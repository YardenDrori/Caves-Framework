using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CavesFramework;

public class CompCollapseCaveTimer : CustomMapComponent
{
  public CompCollapseCaveTimer(Map map)
    : base(map) { }

  //exposed state
  protected int spawnTick = -1;
  protected int tickToCollapse = -1;
  protected int initialNonFullCells = -1;
  protected int filledCells = 0;

  //runtime state
  protected List<(Sustainer sustainer, float? mtbHoursToStop)> sustainedSoundWithMtbHoursToStop = new();
  protected HashSet<SoundDef> activeSustainedSounds = new();
  protected int cacheGen = -1;

  //properties
  protected CaveCollapseTimerProperties Props
  {
    get
    {
      _props ??= CaveInfo.caveDef.collapseTimerProps;
      if (_props == null)
      {
        throw new InvalidOperationException("map " + map.uniqueID + " tried accessing CaveCollapseTimerProperties but it lacks that field.");
      }
      return _props;
    }
  }
  protected CaveInfo CaveInfo
  {
    get
    {
      _caveInfo ??= map.GetComponent<CaveInfo>();
      if (_caveInfo == null)
      {
        throw new InvalidOperationException("map " + map.uniqueID + " is missing the CaveInfo map component");
      }
      return _caveInfo;
    }
  }
  protected int CurrStageEndTick
  {
    get
    {
      if (TryGetCurrentStage(out var eff))
      {
        return spawnTick + Mathf.RoundToInt(eff.endAtCollapsePercentage * TicksToLiveTotal);
      }
      //if there is no active stage don't error its prob just floats being funny and having a gap
      return CurrTick + 1; // stall till next iteration
    }
  }
  protected Predicate<IntVec3> VfxCellValidator
  {
    get
    {
      _vfxCellValidator ??= CaveInfo.NotSolidPredicate;
      return _vfxCellValidator;
    }
  }

  protected int TicksPerUpdate
  {
    get
    {
      if (_ticksPerUpdate == -1)
      {
        if (TryGetCurrentStage(out EffectsAtStage stage))
        {
          _ticksPerUpdate = stage.TicksPerUpdateOverride ?? Props.TicksPerUpdate;
        }
        else
        {
          _ticksPerUpdate = Props.TicksPerUpdate;
        }
      }
      return _ticksPerUpdate;
    }
    set { _ticksPerUpdate = value; }
  }

  protected int TicksToLiveTotal => tickToCollapse - spawnTick;
  private int CurrTick => Find.TickManager.TicksGame;

  //private helpers/caches
  private EffectsAtStage _currEffect = null;
  private CaveCollapseTimerProperties _props = null;
  private CaveInfo _caveInfo = null;
  private Predicate<IntVec3> _vfxCellValidator = null;
  protected int _ticksPerUpdate = -1;

  //consts / configs
  protected const string RandCellCacheKey = "NonFullCell";

  public override void FinalizeInit()
  {
    base.FinalizeInit();

    if (spawnTick == -1)
    {
      spawnTick = Find.TickManager.TicksGame;
    }
    if (tickToCollapse == -1)
    {
      tickToCollapse = spawnTick + GenDate.DaysToTicks(Props.daysToCollapse.RandomInRange);
    }
    if (initialNonFullCells == -1)
    {
      int counter = 0;
      foreach (IntVec3 cell in map.AllCells)
      {
        if (CaveInfo.NotSolidPredicate(cell))
        {
          counter++;
        }
      }
      initialNonFullCells = Mathf.Max(1, counter);
    }
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    if (map.IsHashIntervalTick(TicksPerUpdate, 2))
    {
      if (CurrTick > CurrStageEndTick)
      {
        ProgressStage();
      }

      if (ShouldDoCaveIn())
      {
        DoCaveIn();
      }

      if (Find.CurrentMap == map)
      {
        DoEffects();
      }
      else
      {
        KillAllSustainedSounds();
      }
    }
  }

  protected bool TryGetNonFullCellCount(out int cellCount, bool forceCacheRead = false)
  {
    if (filledCells > 0 && !forceCacheRead)
    {
      cellCount = initialNonFullCells - filledCells;
      return cellCount > 0;
    }

    if (CaveInfo.TryGetCellCountForRandCellsCache(RandCellCacheKey, VfxCellValidator, out int remainingCellCount))
    {
      int gen = CaveInfo.GetRandCellsCacheGeneration(RandCellCacheKey);
      if (gen != cacheGen)
      {
        cacheGen = gen;
        filledCells = initialNonFullCells - remainingCellCount;
      }

      cellCount = remainingCellCount;
      return true;
    }
    cellCount = -1;
    return false;
  }

  protected bool TryGetRandomCell(out IntVec3 cell, bool removeFromCache = false)
  {
    if (CaveInfo.TryGetRandomCell(VfxCellValidator, RandCellCacheKey, out IntVec3 res, removeFromCache))
    {
      if (CaveInfo.GetRandCellsCacheGeneration(RandCellCacheKey) != cacheGen)
      {
        TryGetNonFullCellCount(out _, forceCacheRead: true);
      }

      cell = res;
      return true;
    }
    cell = IntVec3.Invalid;
    return false;
  }

  protected virtual bool ShouldDoCaveIn()
  {
    if (!TryGetCurrentStage(out EffectsAtStage effects))
    {
      return false;
    }

    if (effects.caveInConfig == null)
    {
      return false;
    }

    if (!TryGetNonFullCellCount(out int cellCount))
    {
      return false;
    }

    float remainingFraction = (float)cellCount / initialNonFullCells;
    float filledFraction = 1f - remainingFraction;
    float ratio =
      effects.caveInConfig.mtbFactorOverRemainingEmptyFraction != null
        ? effects.caveInConfig.mtbFactorOverRemainingEmptyFraction.Evaluate(remainingFraction)
        : 1;

    return filledFraction < effects.caveInConfig.maxAirCellsToFillFraction
      && Rand.MTBEventOccurs(effects.caveInConfig.mtbHoursPerTrigger * ratio, GenDate.TicksPerHour, TicksPerUpdate);
  }

  protected virtual void DoCaveIn()
  {
    if (!TryGetCurrentStage(out EffectsAtStage effects))
    {
      return;
    }

    int cellsToFill = effects.caveInConfig.countPerTrigger.RandomInRange;
    int rerollsLeft = 10;
    for (int i = 0; i < cellsToFill; i++)
    {
      if (TryGetRandomCell(out IntVec3 cell))
      {
        if (!CanDoCaveInOnCell(effects, cell))
        {
          if (rerollsLeft > 0)
          {
            rerollsLeft--;
            i--;
          }
          continue;
        }

        if (DoCaveInOnCell(effects, cell))
        {
          filledCells++;
        }
      }
    }
  }

  protected virtual bool CanDoCaveInOnCell(EffectsAtStage effects, IntVec3 cell)
  {
    if (!effects.caveInConfig.canCrushExistingThings && (cell.GetEdifice(map) != null || cell.GetFirstItem(map) != null))
    {
      return false;
    }

    int? minDistFromExit = effects.caveInConfig.minCellDistanceFromExit;
    if (minDistFromExit.HasValue)
    {
      IntVec3 exitPos = CaveInfo.portalIntoCave?.exit.Position ?? IntVec3.Invalid;
      if (exitPos != IntVec3.Invalid && cell.DistanceTo(exitPos) < minDistFromExit.Value)
      {
        return false;
      }
    }

    if (cell.GetFirstPawn(map) != null)
    {
      return false;
    }

    if (effects.caveInConfig.maxDistFromWalls.HasValue)
    {
      bool naturalRockInRadius = false;

      int numCells = GenRadial.NumCellsInRadius(effects.caveInConfig.maxDistFromWalls.Value);
      for (int i = 0; i < numCells; i++)
      {
        IntVec3 candidate = cell + GenRadial.RadialPattern[i];
        if (candidate.InBounds(map) && (candidate.GetEdifice(map)?.def.building?.isNaturalRock ?? false))
        {
          naturalRockInRadius = true;
          break;
        }
      }
      if (!naturalRockInRadius)
      {
        return false;
      }
    }

    return true;
  }

  protected virtual bool DoCaveInOnCell(EffectsAtStage effects, IntVec3 cell)
  {
    ThingDef rockDef = ChooseRockDefForCaveIn(effects, cell);
    if (rockDef == null)
    {
      return false;
    }

    if (!GenSpawn.TrySpawn(rockDef, cell, map, out Thing _, WipeMode.Vanish, canWipeEdifices: true))
    {
      return false;
    }

    effects.caveInConfig.caveInEffecter?.SpawnMaintained(cell, map);
    effects.caveInConfig.caveInSound?.PlayOneShot(SoundInfo.InMap(new TargetInfo(cell, map)));

    return true;
  }

  protected virtual ThingDef ChooseRockDefForCaveIn(EffectsAtStage effects, IntVec3 cell)
  {
    EffectsAtStage.CaveInConfig config = effects.caveInConfig;

    float naturalWeight = config.naturalRockWeight;
    float additionalWeight = config.additionalRockTypes.NullOrEmpty() ? 0f : config.additionalRockTypes.Sum(option => option.weight);

    if (naturalWeight + additionalWeight <= 0f)
    {
      return null;
    }

    if (additionalWeight <= 0f || Rand.Range(0f, naturalWeight + additionalWeight) < naturalWeight)
    {
      return NaturalRockDefNear(cell);
    }
    return config.additionalRockTypes.RandomElementByWeight(option => option.weight).thingDef;
  }

  //get coherent formations as opposed to random confetti
  protected virtual ThingDef NaturalRockDefNear(IntVec3 cell)
  {
    //random start to have variety
    int offset = Rand.Range(0, GenAdj.AdjacentCells.Length);
    for (int i = 0; i < GenAdj.AdjacentCells.Length; i++)
    {
      IntVec3 neighbour = cell + GenAdj.AdjacentCells[(i + offset) % GenAdj.AdjacentCells.Length];
      if (!neighbour.InBounds(map))
      {
        continue;
      }

      Building edifice = neighbour.GetEdifice(map);
      if (edifice != null && edifice.def.building != null && edifice.def.building.isNaturalRock && !edifice.def.IsSmoothed)
      {
        return edifice.def;
      }
    }

    return CaveInfo.rockDefs.NullOrEmpty() ? null : CaveInfo.rockDefs.RandomElement();
  }

  protected virtual void DoEffects()
  {
    if (!TryGetCurrentStage(out EffectsAtStage effect))
    {
      return;
    }

    //camera shake
    if (effect.screenShakeConfig != null && Rand.MTBEventOccurs(effect.screenShakeConfig.mtbHoursPerShake, GenDate.TicksPerHour, TicksPerUpdate))
    {
      Find.CameraDriver.shaker.DoShake(effect.screenShakeConfig.shakeAmount, effect.screenShakeConfig.shakeDurationTicks);
    }

    //play sound effects
    foreach (SoundPlayConfig sfx in effect.soundsConfigs)
    {
      if (activeSustainedSounds.Contains(sfx.soundDef))
      {
        continue;
      }

      if (!sfx.IsSustained)
      {
        if (Rand.MTBEventOccurs(sfx.mtbHoursPerPlay.Value, GenDate.TicksPerHour, TicksPerUpdate))
        {
          sfx.soundDef.PlayOneShotOnCamera(map);
        }
        continue; //saves a non trivial Rand call
      }

      if (!sfx.mtbHoursToStartPlaying.HasValue || Rand.MTBEventOccurs(sfx.mtbHoursToStartPlaying.Value, GenDate.TicksPerHour, TicksPerUpdate))
      {
        Sustainer sustainer = sfx.soundDef.TrySpawnSustainer(SoundInfo.OnCamera(MaintenanceType.PerTickRare)); //can NRE but vanilla logs the important one sooo
        activeSustainedSounds.Add(sfx.soundDef);
        sustainedSoundWithMtbHoursToStop.Add((sustainer, sfx.mtbHoursToStopPlaying));
      }
    }

    //maintain existing sound effects
    for (int i = 0; i < sustainedSoundWithMtbHoursToStop.Count; i++)
    {
      Sustainer sustainer = sustainedSoundWithMtbHoursToStop[i].sustainer;
      float? mtbToStop = sustainedSoundWithMtbHoursToStop[i].mtbHoursToStop;
      if (mtbToStop.HasValue && Rand.MTBEventOccurs(mtbToStop.Value, GenDate.TicksPerHour, TicksPerUpdate))
      {
        sustainedSoundWithMtbHoursToStop.RemoveAt(i);
        activeSustainedSounds.Remove(sustainer.def);
        sustainer.End();
        i--;
        continue;
      }
      sustainer.Maintain();
    }

    //visual effects
    bool countAvailable = TryGetNonFullCellCount(out int cellCount);
    foreach (EffecterSpawnerConfig vfx in effect.effectsConfigs)
    {
      if (!countAvailable && vfx.spawnsPerHourPer10kEmptyCells.HasValue)
      {
        continue;
      }

      if (!Rand.MTBEventOccurs(vfx.EffectiveMtbHours(cellCount), GenDate.TicksPerHour, TicksPerUpdate))
      {
        continue;
      }

      if (!TryGetRandomCell(out IntVec3 targetCell))
      {
        continue;
      }

      vfx.effecter.SpawnMaintained(targetCell, map);
    }
  }

  protected virtual bool TryGetCurrentStage(out EffectsAtStage effect)
  {
    if (_currEffect == null)
    {
      bool res = TryUpdateCurrEffectCache();
      if (!res)
      {
        effect = null;
        return false;
      }
    }
    effect = _currEffect;
    return true;
  }

  protected virtual void ProgressStage()
  {
    if (TryGetCurrentStage(out EffectsAtStage oldStageEffect))
    {
      EvictOldStageEffects(oldStageEffect);
    }

    //we dont care about success here well just try again if it failed
    TryUpdateCurrEffectCache();

    KillAllSustainedSounds();

    //ConfigErrors guarantees that there is a stage that ends at 100%
    CollapseIfShould();

    if (TryGetCurrentStage(out EffectsAtStage NewStageEffect))
    {
      ApplyNewStageEffects(NewStageEffect);
    }
  }

  protected virtual void EvictOldStageEffects(EffectsAtStage effect)
  {
    foreach (var mapComp in effect.mapComponentsToAdd)
    {
      if (mapComp.removeOnStageEnd)
      {
        map.components.RemoveWhere(comp => comp.GetType() == mapComp.mapComponent);
      }
    }
    effect.notificationOnStageExit?.Send(new LookTargets(map.Center, map));
    TicksPerUpdate = -1;
  }

  protected virtual void ApplyNewStageEffects(EffectsAtStage effect)
  {
    foreach (var mapComp in effect.mapComponentsToAdd)
    {
      try
      {
        MapComponent compToAdd = (MapComponent)Activator.CreateInstance(mapComp.mapComponent, map);
        map.components.Add(compToAdd);
      }
      catch (Exception ex)
      {
        Log.Error("Could not instantiate a MapComponent of type " + mapComp.mapComponent?.ToString() + ": " + ex);
      }
    }
    effect.notificationOnStageEntry?.Send(new LookTargets(map.Center, map));
  }

  protected virtual void CollapseIfShould()
  {
    if (CurrTick < tickToCollapse)
    {
      return;
    }

    //best var name ever dont even @ me
    List<Pawn> pawnsToBrutallyCrushToDeath = GetAllPawnsInCave(out List<Pawn> colonyPawns);
    if (colonyPawns.NullOrEmpty())
    {
      //no casualties :(
      Props.letterOrMessageOnCollapse.noCasualties.Send(new LookTargets(CaveInfo.portalIntoCave));
    }
    else
    {
      //casualties ^-^
      Props.letterOrMessageOnCollapse.withCasualties.Send(new LookTargets(CaveInfo.portalIntoCave), BuildCommaSeperatedPawnNamesTString(colonyPawns));
    }

    DamageInfo damageInfo = new DamageInfo(DamageDefOf.Crush, 99999f, 999f);
    foreach (Pawn poorInnocentCreature in pawnsToBrutallyCrushToDeath)
    {
      poorInnocentCreature.TakeDamage(damageInfo);
      if (!poorInnocentCreature.Dead)
      {
        poorInnocentCreature.Kill(damageInfo);
      }
    }

    //we spawn on the position instead of thing cause were gonna kill the entrance
    Thing caveEntrance = CaveInfo.portalIntoCave;
    Props.caveEntranceEffecterDefOnCollapse?.SpawnMaintained(caveEntrance.Position, caveEntrance.Map);
    Props.soundDefOnCollapse?.PlayOneShot(SoundInfo.InMap(new TargetInfo(caveEntrance.Position, caveEntrance.Map)));

    //kill the entrance
    Thing.allowDestroyNonDestroyable = true;
    caveEntrance.Destroy(DestroyMode.Deconstruct);
    Thing.allowDestroyNonDestroyable = false;

    PocketMapUtility.DestroyPocketMap(map);
  }

  protected TaggedString BuildCommaSeperatedPawnNamesTString(List<Pawn> pawns)
  {
    TaggedString pawnNames = string.Join(", ", pawns.Select(p => p.NameShortColored));
    pawnNames += ".";
    return pawnNames;
  }

  protected List<Pawn> GetAllPawnsInCave(out List<Pawn> colonyPawns)
  {
    colonyPawns = new(map.mapPawns.FreeColonistsAndPrisoners);
    return new List<Pawn>(map.mapPawns.AllPawns);
  }

  protected virtual bool TryUpdateCurrEffectCache()
  {
    int currTick = CurrTick;
    foreach (EffectsAtStage stage in Props.effectsAtStages)
    {
      //filter future stages
      if (spawnTick + Mathf.RoundToInt(stage.startAtCollapsePercentage * TicksToLiveTotal) > currTick)
      {
        continue;
      }

      //filter already done stages
      if (spawnTick + Mathf.RoundToInt(stage.endAtCollapsePercentage * TicksToLiveTotal) <= currTick)
      {
        continue;
      }

      //what remains is the stage to activate due to configErrors guaranteing up to 1 active stage at a time
      _currEffect = stage;
      return true;
    }
    _currEffect = null;
    return false;
  }

  protected void KillAllSustainedSounds()
  {
    if (sustainedSoundWithMtbHoursToStop.Count == 0)
    {
      return;
    }

    foreach (var entry in sustainedSoundWithMtbHoursToStop)
    {
      entry.sustainer.End();
    }
    sustainedSoundWithMtbHoursToStop.Clear();
    activeSustainedSounds.Clear();
  }

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
    Scribe_Values.Look(ref tickToCollapse, "tickToCollapse", -1);
    Scribe_Values.Look(ref initialNonFullCells, "initialNonFullCells", -1);
    Scribe_Values.Look(ref filledCells, "filledCells", 0);
  }
}
