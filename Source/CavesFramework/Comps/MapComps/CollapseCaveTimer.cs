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

  //runtime state
  List<(Sustainer sustainer, float? mtbHoursToStop)> sustainedSoundWithMtbHoursToStop = new();
  HashSet<SoundDef> activeSustainedSounds = new();

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
  protected int TicksToLiveTotal => tickToCollapse - spawnTick;
  private int CurrTick => Find.TickManager.TicksGame;

  //private helpers/caches
  private EffectsAtStage _currEffect = null;
  private CaveCollapseTimerProperties _props = null;
  private CaveInfo _caveInfo = null;
  private Predicate<IntVec3> _vfxCellValidator = null;

  //consts / configs
  protected const int TicksPerDoEffect = 40;
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
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    //check for stage update every 250 ticks
    if (map.IsHashIntervalTick(250, 0))
    {
      if (CurrTick > CurrStageEndTick)
      {
        ProgressStage();
      }
    }

    if (Find.CurrentMap == map)
    {
      if (map.IsHashIntervalTick(TicksPerDoEffect, 2))
      {
        DoEffects();
      }
    }
    else
    {
      KillAllSustainedSounds();
    }
  }

  protected virtual void DoEffects()
  {
    if (!TryGetCurrentStage(out EffectsAtStage effect))
    {
      return;
    }

    //camera shake
    if (effect.screenShakeConfig != null && Rand.MTBEventOccurs(effect.screenShakeConfig.mtbHoursPerShake, GenDate.TicksPerHour, TicksPerDoEffect))
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
        if (Rand.MTBEventOccurs(sfx.mtbHoursPerPlay.Value, GenDate.TicksPerHour, TicksPerDoEffect))
        {
          sfx.soundDef.PlayOneShotOnCamera(map);
        }
        continue; //saves a non trivial Rand call
      }

      if (!sfx.mtbHoursToStartPlaying.HasValue || Rand.MTBEventOccurs(sfx.mtbHoursToStartPlaying.Value, GenDate.TicksPerHour, TicksPerDoEffect))
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
      if (mtbToStop.HasValue && Rand.MTBEventOccurs(mtbToStop.Value, GenDate.TicksPerHour, TicksPerDoEffect))
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
    bool countAvailable = CaveInfo.TryGetCellCountForRandCellsCache(RandCellCacheKey, VfxCellValidator, out int cellCount);
    foreach (EffecterSpawnerConfig vfx in effect.effectsConfigs)
    {
      if (!countAvailable && vfx.spawnsPerHourPer10kEmptyCells.HasValue)
      {
        continue;
      }

      if (!Rand.MTBEventOccurs(vfx.EffectiveMtbHours(cellCount), GenDate.TicksPerHour, TicksPerDoEffect))
      {
        continue;
      }

      if (!CaveInfo.TryGetRandomCell(VfxCellValidator, RandCellCacheKey, out IntVec3 targetCell))
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
    effect.notificationOnStageExit?.Send(LookTargets.Invalid);
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
    effect.notificationOnStageEntry.Send(LookTargets.Invalid);
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
    return map.mapPawns.AllPawns;
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

    foreach (var sustainer in sustainedSoundWithMtbHoursToStop)
    {
      sustainer.sustainer.End();
    }
    sustainedSoundWithMtbHoursToStop.Clear();
    activeSustainedSounds.Clear();
  }

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
    Scribe_Values.Look(ref tickToCollapse, "tickToCollapse", -1);
  }
}
