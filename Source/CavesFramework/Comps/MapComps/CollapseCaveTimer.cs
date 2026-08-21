using System;
using System.Collections.Generic;
using System.IO;
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
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    //only tick once per TickRare for perf
    if (Find.TickManager.TicksGame % 250 != 0)
    {
      return;
    }

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

  protected void Collapse()
  {
    //TODO:
    //spawn letter
    throw new NotImplementedException();
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
