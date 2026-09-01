using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CavesFramework;

public class EffectsAtStage
{
  // hook for added behavior and a buncha shit for instance adding visual effects
  public class MapComponentsOnStageEntry
  {
    public Type mapComponent;
    public bool removeOnStageEnd = true;
  }

  public class CaveInConfig
  {
    public float mtbHoursPerTrigger = 1f;
    public SimpleCurve mtbFactorOverRemainingEmptyFraction;

    public IntRange countPerTrigger = new IntRange(1, 3);

    public float maxAirCellsToFillFraction = 0.85f;

    public float naturalRockWeight = 1f;
    public List<ThingOption> additionalRockTypes = new();

    public IntRange distanceFromWalls = new IntRange(1, 2);
    public int minCellDistanceFromExit = 15;

    public bool canBlockPathwayToExit = true;

    //NOTE: doesnt apply to pawns cause instakill != fun
    public bool canCrushExistingThings = true;

    //applies per ROCK not per caveIn call
    public EffecterDef caveInEffecter;
    public SoundDef caveInSound;
  }

  public int? TicksPerUpdateOverride;

  public float startAtCollapsePercentage = 0f;
  public float endAtCollapsePercentage = 1f;

  // rockfall enabled by default set <caveInParams IsNull="True" /> to disable
  public CaveInConfig caveInConfig = new();

  //ambiance
  public ScreenShakeConfig screenShakeConfig;
  public List<EffecterSpawnerConfig> effectsConfigs = new();
  public List<SoundPlayConfig> soundsConfigs = new();

  public NotificationConfig notificationOnStageEntry;
  public NotificationConfig notificationOnStageExit;

  public List<MapComponentsOnStageEntry> mapComponentsToAdd = new();

  public IEnumerable<string> ConfigErrors()
  {
    if (TicksPerUpdateOverride.HasValue && TicksPerUpdateOverride.Value < 1)
    {
      yield return "TicksPerUpdateOverride has a non positive value.";
    }

    if (startAtCollapsePercentage >= endAtCollapsePercentage)
    {
      yield return "start collapse percentage is not lesser than end at collapse percentage.";
    }
    if (startAtCollapsePercentage < 0)
    {
      yield return "start at collapse percentage is a negative value.";
    }
    if (endAtCollapsePercentage > 1)
    {
      yield return "end at collapse percentage is a value greater than 1. 1 is equivalent to 100%";
    }

    foreach (var comp in mapComponentsToAdd)
    {
      if (comp.mapComponent == null)
      {
        yield return "a map component to add has an empty or invalid entry.";
        continue;
      }
      if (!typeof(MapComponent).IsAssignableFrom(comp.mapComponent))
      {
        yield return "a map component to add has an empty or invalid entry.";
      }
    }

    if (notificationOnStageEntry != null)
    {
      foreach (string err in notificationOnStageEntry.ConfigErrors())
      {
        yield return "notificationOnStageEntry: " + err;
      }
    }
    if (notificationOnStageExit != null)
    {
      foreach (string err in notificationOnStageExit.ConfigErrors())
      {
        yield return "notificationOnStageExit: " + err;
      }
    }

    if (caveInConfig != null)
    {
      if (caveInConfig.countPerTrigger.max < caveInConfig.countPerTrigger.min)
      {
        yield return "caveInParams.countPerTrigger's max value is lesser than its min.";
      }
      if (caveInConfig.countPerTrigger.min < 1)
      {
        yield return "caveInParams.countPerTrigger's min value is lesser than 1.";
      }

      foreach (
        string err in caveInConfig.mtbFactorOverRemainingEmptyFraction?.ConfigErrors("factor over remaining empty cells")
          ?? Enumerable.Empty<string>()
      )
      {
        yield return err;
      }

      if (caveInConfig.distanceFromWalls.max < caveInConfig.distanceFromWalls.min)
      {
        yield return "caveInParams.distanceFromWalls's max value is lesser than its min.";
      }
      if (caveInConfig.distanceFromWalls.min < 0)
      {
        yield return "caveInParams.distanceFromWalls's min value cannot be negative.";
      }

      if (caveInConfig.maxAirCellsToFillFraction <= 0)
      {
        yield return "caveInParams.maxAirCellsToFillFraction's value is lesser than 0, the range is 0~1.";
      }
      if (caveInConfig.maxAirCellsToFillFraction > 1)
      {
        yield return "caveInParams.maxAirCellsToFillFraction's value is greater than 1, the range is 0~1.";
      }

      if (caveInConfig.minCellDistanceFromExit < 0)
      {
        yield return "caveInParams.minCellDistanceFromExit's value cannot be negative.";
      }
      if (caveInConfig.mtbHoursPerTrigger <= 0)
      {
        yield return "caveInParams.mtbHoursPerTrigger's value must be greater than 0.";
      }
      if (caveInConfig.naturalRockWeight < 0)
      {
        yield return "caveInParams.naturalRockWeight's value cannot be negative.";
      }
    }
    if (screenShakeConfig != null)
    {
      foreach (string err in screenShakeConfig.ConfigErrors())
      {
        yield return err;
      }
    }
    foreach (EffecterSpawnerConfig conf in effectsConfigs)
    {
      foreach (string err in conf.ConfigErrors())
      {
        yield return err;
      }
    }
    foreach (SoundPlayConfig conf in soundsConfigs)
    {
      foreach (string err in conf.ConfigErrors())
      {
        yield return err;
      }
    }
  }
}

public class CaveCollapseTimerProperties
{
  public class CollapseLetterOrMessage
  {
    public NotificationConfig noCasualties = new();
    public NotificationConfig withCasualties = new();
  }

  public FloatRange daysToCollapse = new FloatRange(3, 3);

  public List<EffectsAtStage> effectsAtStages = new();

  public CollapseLetterOrMessage letterOrMessageOnCollapse = new();

  public SoundDef soundDefOnCollapse;
  public EffecterDef caveEntranceEffecterDefOnCollapse;

  public int TicksPerUpdate = 45;

  //log errors on boot
  public IEnumerable<string> ConfigErrors()
  {
    if (TicksPerUpdate < 1)
    {
      yield return "tickIntervalForEffectsAndCaveIn has a non positive value.";
    }

    NotificationConfig[] notifications = { letterOrMessageOnCollapse.noCasualties, letterOrMessageOnCollapse.withCasualties };

    if (soundDefOnCollapse?.sustain ?? false)
    {
      yield return "soundDefOnCollapse must be a oneshot.";
    }

    if (daysToCollapse.max < daysToCollapse.min)
    {
      yield return "days to collapse's max value is lesser than its min.";
    }
    if (daysToCollapse.min <= 0)
    {
      yield return "days to collapse's min value must be greater than 0.";
    }

    foreach (NotificationConfig notification in notifications)
    {
      foreach (string err in notification.ConfigErrors())
      {
        yield return "letterOrMessageOnCollapse: " + err;
      }
    }

    float prevExitPercentage = -1;
    bool hasMax1 = false;
    bool hasMin0 = false;
    List<EffectsAtStage> sortedEffects = new(effectsAtStages);
    sortedEffects.Sort((a, b) => a.endAtCollapsePercentage.CompareTo(b.endAtCollapsePercentage));
    for (int i = 0; i < effectsAtStages.Count; i++)
    {
      EffectsAtStage stage = sortedEffects[i];
      foreach (string err in stage.ConfigErrors())
      {
        yield return $"stage {stage.startAtCollapsePercentage}-{stage.endAtCollapsePercentage}: {err}";
      }

      if (stage.endAtCollapsePercentage == 1f)
      {
        hasMax1 = true;
      }
      if (stage.startAtCollapsePercentage == 0f)
      {
        hasMin0 = true;
      }

      if (prevExitPercentage == -1)
      {
        prevExitPercentage = stage.endAtCollapsePercentage;
        continue;
      }

      if (stage.startAtCollapsePercentage < prevExitPercentage)
      {
        yield return $"stage {stage.startAtCollapsePercentage}-{stage.endAtCollapsePercentage}: starts before the previous stage ends at {prevExitPercentage}, stages may not overlap.";
        continue;
      }

      if ((stage.startAtCollapsePercentage - prevExitPercentage) > Mathf.Epsilon)
      {
        yield return $"stage {stage.startAtCollapsePercentage}-{stage.endAtCollapsePercentage}: leaves a gap after the previous stage ends at {prevExitPercentage}, stages must be contiguous.";
      }

      prevExitPercentage = stage.endAtCollapsePercentage;
    }
    if (!hasMax1)
    {
      yield return "no stage has an end percentage of 100% (1f).";
    }
    if (!hasMin0)
    {
      yield return "no stage has a start percentage of 0% (0f).";
    }
  }
}
