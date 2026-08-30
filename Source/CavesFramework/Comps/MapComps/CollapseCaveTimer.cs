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

  //getters
  public int TicksPassedSinceSpawn => Find.TickManager.TicksGame - tickSpawned;
  public int TickToCollapse => tickSpawned + ticksCaveWillBeAlive;
  private CaveCollapseTimerProperties _cachedProps;
  public CaveCollapseTimerProperties Props => _cachedProps ??= map.GetComponent<CaveInfo>()?.caveDef?.collapseTimerProps;

  //shit we expose
  protected int tickSpawned = -1;
  protected int ticksCaveWillBeAlive = -1;
  protected List<TicksPerStage> ticksPerStage = new();

  //shit we derive on load
  protected MapPortal overworldPortal;
  protected MapPortal cavePortal;
  protected List<EffectsAtStage> activeStages = new();

  //utils
  public EffectsAtStage GetEffectFromIndex(int index) => Props.effectsAtStages[index];

  private List<IntVec3> emptyCellsCache;
  private int emptyCellCursor;

  protected List<IntVec3> EmptyCells
  {
    get
    {
      if (emptyCellsCache == null)
      {
        emptyCellsCache = new List<IntVec3>();
        foreach (IntVec3 cell in map.cellsInRandomOrder.GetAll())
        {
          if (CellIsEmpty(cell))
          {
            emptyCellsCache.Add(cell);
          }
        }
      }
      return emptyCellsCache;
    }
  }

  private bool CellIsEmpty(IntVec3 cell)
  {
    Building edifice = cell.GetEdifice(map);
    return edifice == null || edifice.def.Fillage != FillCategory.Full;
  }

  protected IntVec3 NextEmptyCell()
  {
    List<IntVec3> cells = EmptyCells;

    // we still check incase the cache is stale
    for (int i = 0; i < cells.Count; i++)
    {
      if (emptyCellCursor >= cells.Count)
      {
        emptyCellCursor = 0;
      }
      IntVec3 cell = cells[emptyCellCursor++];
      if (CellIsEmpty(cell))
      {
        return cell;
      }
    }
    return IntVec3.Invalid;
  }

  public CompCollapseCaveTimer(Map map)
    : base(map) { }

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

    CaveInfo mapInfo = map.GetComponent<CaveInfo>();
    if (mapInfo == null)
    {
      Log.Error("CF: cave map does not have cave info mod extension.");
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

    HandleCaveIn();

    if (map.IsHashIntervalTick(250, 0))
    {
      EnterAndExitStages();
    }
  }

  //TODO: implement rockfall using Props.effectsAtStages[..].caveInParams
  protected virtual void HandleCaveIn() { }

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
    CaveCollapseTimerProperties.CollapseLetterOrMessage.CollapseNotification notification = casualties
      ? Props.letterOrMessageOnCollapse.withCasualties
      : Props.letterOrMessageOnCollapse.noCasualties;

    if (notification.IsLetter)
    {
      TaggedString desc = new TaggedString(notification.letterDesc);
      if (casualties)
      {
        desc += BuildPawnNamesTaggedString(deadPawns);
      }
      Find.LetterStack.ReceiveLetter(notification.letterLabel, desc, notification.letterDef, new LookTargets(overworldPortal));
      return;
    }

    Messages.Message(notification.messageToast, new LookTargets(overworldPortal), notification.messageTypeDef, true);
  }

  protected TaggedString BuildPawnNamesTaggedString(List<Pawn> pawns)
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
}
