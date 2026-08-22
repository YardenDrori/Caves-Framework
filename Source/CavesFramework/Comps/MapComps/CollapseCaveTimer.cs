using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CavesFramework;

public class CompCollapseCaveTimer : CustomMapComponent
{
  protected class TicksPerStage : IExposable
  {
    public int stageIndex;
    public int tickToEnter;
    public int tickToLeave;

    //we leave these false to be populated by the tick fucntion
    public bool entered = false;
    public bool exited = false;

    public TicksPerStage() { }

    public TicksPerStage(int stageIndex, int tickToEnter, int tickToLeave)
    {
      this.stageIndex = stageIndex;
      this.tickToEnter = tickToEnter;
      this.tickToLeave = tickToLeave;
    }

    public void ExposeData()
    {
      Scribe_Values.Look(ref stageIndex, "stageIndex", -1);
      Scribe_Values.Look(ref tickToEnter, "tickToEnter");
      Scribe_Values.Look(ref tickToLeave, "tickToLeave");
      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        int currTick = Find.TickManager.TicksGame;
        entered = currTick >= tickToEnter;
        exited = currTick >= tickToLeave;
      }
    }
  }

  private CaveCollapseTimerProperties _cachedProps;
  public CaveCollapseTimerProperties Props => _cachedProps ??= map.GetComponent<CaveInfo>()?.caveDef?.collapseTimerProps;

  public int TicksPassedSinceSpawn => Find.TickManager.TicksGame - tickSpawned;
  public int TickToCollapse => tickSpawned + ticksCaveWillBeAlive;

  public EffectsAtStage GetEffectFromIndex(int index) => Props.effectsAtStages[index];

  protected int tickSpawned = -1;
  protected int ticksCaveWillBeAlive = -1;
  protected List<TicksPerStage> ticksPerStage = new();

  protected MapPortal overworldPortal;
  protected MapPortal cavePortal;

  protected List<EffectsAtStage> activeStages = new();

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    if (Props == null)
    {
      Log.Error("CF: Failed to find CaveCollapseTimerProperties for map: " + map.uniqueID);
      return;
    }

    if (tickSpawned == -1)
    {
      tickSpawned = Find.TickManager.TicksGame;
    }

    if (ticksCaveWillBeAlive == -1)
    {
      ticksCaveWillBeAlive = Mathf.RoundToInt(Props.daysToCollapse.RandomInRange * 60000);
    }

    if (ticksPerStage.NullOrEmpty())
    {
      for (int i = 0; i < Props.effectsAtStages.Count; i++)
      {
        EffectsAtStage stage = Props.effectsAtStages[i];

        float stagePercentage = stage.startAtCollapsePercentage.RandomInRange;
        int tickOffset = Mathf.RoundToInt(stagePercentage * ticksCaveWillBeAlive);
        int tickToStart = tickSpawned + tickOffset;

        stagePercentage = stage.endAtCollapsePercentage.RandomInRange;
        tickOffset = Mathf.RoundToInt(stagePercentage * ticksCaveWillBeAlive);
        int tickToEnd = tickSpawned + tickOffset;

        ticksPerStage.Add(new TicksPerStage(i, tickToStart, tickToEnd));
      }
    }

    for (int i = 0; i < ticksPerStage.Count; i++)
    {
      if (!ticksPerStage[i].entered && !ticksPerStage[i].exited)
      {
        activeStages.Add(GetEffectFromIndex(ticksPerStage[i].stageIndex));
      }
    }

    if (
      Props.letterOrMessageOnCollapse.NoCasualties.letterDef != null && Props.letterOrMessageOnCollapse.NoCasualties.messageTypeDef != null
      || Props.letterOrMessageOnCollapse.WithCasualties.letterDef != null && Props.letterOrMessageOnCollapse.WithCasualties.messageTypeDef != null
    )
    {
      Log.Warning("CF: Both letterDef and messageTypeDef have been set but they are mutually exclusive. Please choose one.");
    }

    CaveInfo mapInfo = map.GetComponent<CaveInfo>();
    if (mapInfo == null)
    {
      Log.Error("CF: cave map does not have cave info map component.");
    }
    else
    {
      overworldPortal = mapInfo.portalIntoCave;
      cavePortal = overworldPortal.exit;
    }
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    HandleEffects();

    //only tick once per TickRare for perf we choose we add the unqiue map id to prevent accidentally
    //batching all of the components using a similar technique onto one tick and causing lag spikes
    if ((long)(Find.TickManager.TicksGame + map.uniqueID * 7) % 250 != 0)
    {
      return;
    }

    EnterAndExitStages();
  }

  protected virtual void HandleEffects()
  {
    throw new NotImplementedException();
  }

  protected virtual void EnterAndExitStages()
  {
    for (int i = 0; i < ticksPerStage.Count; i++)
    {
      TicksPerStage stage = ticksPerStage[i];
      if (!stage.entered && Find.TickManager.TicksGame > stage.tickToEnter)
      {
        EnterStage(GetEffectFromIndex(stage.stageIndex));
        stage.entered = true;
      }
      if (!stage.exited && Find.TickManager.TicksGame > stage.tickToLeave)
      {
        ExitStage(GetEffectFromIndex(stage.stageIndex));
        stage.exited = true;
      }
    }

    if (Find.TickManager.TicksGame >= TickToCollapse)
    {
      Collapse();
    }
  }

  private void EnterStage(EffectsAtStage stage)
  {
    activeStages.Add(stage);

    foreach (var comp in stage.mapComponentsToAdd)
    {
      MapComponent item = (MapComponent)Activator.CreateInstance(comp.mapComponent, map);
      map.components.Add(item);
    }

    if (stage.letterOnStageEntry != null)
    {
      Find.LetterStack.ReceiveLetter(
        stage.letterOnStageEntry.letterLabel,
        stage.letterOnStageEntry.letterDesc,
        stage.letterOnStageEntry.letterDef,
        new TargetInfo(map.Center, map)
      );
    }
  }

  private void ExitStage(EffectsAtStage stage)
  {
    activeStages.Remove(stage);

    foreach (var comp in stage.mapComponentsToAdd)
    {
      if (comp.removeOnStageEnd)
      {
        MapComponent item = map.GetComponent(comp.mapComponent);
        if (item == null)
        {
          if (comp.logOnRemovalFailure)
          {
            Log.Error("CF: Failed to remove mapComponent" + comp.mapComponent.Name);
          }
          return;
        }
        map.components.Remove(item);
      }
    }
  }

  protected virtual void Collapse()
  {
    List<Pawn> pawnsToKill = GetPawnsInCavern();
    KillPawns(pawnsToKill);

    //TODO: delete pocket map do a buncha sfx and vfx and add a delay until letter pops up

    SpawnLetterOrMessage(pawnsToKill);
  }

  protected void SpawnLetterOrMessage(List<Pawn> deadPawns)
  {
    bool casualties = !deadPawns.NullOrEmpty();
    if (casualties)
    {
      if (Props.letterOrMessageOnCollapse.WithCasualties.letterDef != null)
      {
        TaggedString desc = new TaggedString(Props.letterOrMessageOnCollapse.WithCasualties.letterDesc);
        desc += buildPawnNamesTaggedString(deadPawns);
        Find.LetterStack.ReceiveLetter(
          Props.letterOrMessageOnCollapse.WithCasualties.letterLabel,
          desc,
          Props.letterOrMessageOnCollapse.WithCasualties.letterDef,
          new LookTargets(overworldPortal)
        );
        return;
      }
      Messages.Message(
        Props.letterOrMessageOnCollapse.WithCasualties.messageToast,
        new LookTargets(overworldPortal),
        Props.letterOrMessageOnCollapse.WithCasualties.messageTypeDef,
        true
      );
      return;
    }

    if (Props.letterOrMessageOnCollapse.NoCasualties.letterDef != null)
    {
      Find.LetterStack.ReceiveLetter(
        Props.letterOrMessageOnCollapse.NoCasualties.letterLabel,
        Props.letterOrMessageOnCollapse.NoCasualties.letterDesc,
        Props.letterOrMessageOnCollapse.NoCasualties.letterDef,
        new LookTargets(overworldPortal)
      );
      return;
    }
    Messages.Message(
      Props.letterOrMessageOnCollapse.NoCasualties.messageToast,
      new LookTargets(overworldPortal),
      Props.letterOrMessageOnCollapse.NoCasualties.messageTypeDef,
      true
    );
  }

  protected TaggedString buildPawnNamesTaggedString(List<Pawn> pawns)
  {
    TaggedString names = new TaggedString();
    for (int i = 0; i < pawns.Count; i++)
    {
      Pawn pawn = pawns[i];
      names += pawn.NameShortColored;
      if (i < pawns.Count - 1)
      {
        names += ", ";
      }
    }
    return names;
  }

  protected void KillPawns(List<Pawn> pawnsToBrutallyMurderLol)
  {
    DamageInfo murderWith = new DamageInfo(DamageDefOf.Crush, 9999f, 9999f);
    foreach (Pawn pawn in pawnsToBrutallyMurderLol)
    {
      pawn.Kill(murderWith);
    }
  }

  protected virtual List<Pawn> GetPawnsInCavern()
  {
    List<Pawn> pawns = new();
    foreach (var pawn in map.mapPawns.AllPawns)
    {
      if (!pawn.IsColonist && !pawn.IsPrisonerOfColony)
      {
        continue;
      }
      pawns.Add(pawn);
    }
    return pawns;
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref tickSpawned, "tickSpawned", -1);
    Scribe_Values.Look(ref ticksCaveWillBeAlive, "ticksCaveWillBeAlive", -1);
    Scribe_Collections.Look(ref ticksPerStage, "ticksPerStage", LookMode.Deep);

    // if we load and the save data isnt found this prevents an nre
    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      ticksPerStage ??= new();
    }
  }

  public CompCollapseCaveTimer(Map map)
    : base(map) { }
}
