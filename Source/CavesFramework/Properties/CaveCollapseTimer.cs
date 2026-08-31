using System;
using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public class EffectsAtStage
{
  public class LetterOnStageEntry
  {
    public LetterDef letterDef;
    public string letterLabel;
    public string letterDesc;
  }

  // hook for added behavior and a buncha shit for instance adding visual effects
  public class MapComponentsOnStageEntry
  {
    public Type mapComponent;
    public bool removeOnStageEnd = true;
    public bool logOnRemovalFailure = false;
  }

  public class CaveInConfig
  {
    public float mtbHoursPerTrigger = 1f;
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

  public float startAtCollapsePercentage = 0f;
  public float endAtCollapsePercentage = 1f;

  // rockfall enabled by default set <caveInParams IsNull="True" /> to disable
  public CaveInConfig caveInConfig = new();

  //ambiance
  public ScreenShakeConfig screenShakeConfig;
  public List<EffecterSpawnerConfig> effectsConfigs = new();
  public List<SoundPlayConfig> soundsConfigs = new();

  public LetterOnStageEntry letterOnStageEntry;

  public List<MapComponentsOnStageEntry> mapComponentsToAdd = new();

  public IEnumerable<string> ConfigErrors()
  {
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

    if (letterOnStageEntry != null)
    {
      if (letterOnStageEntry.letterDef == null)
      {
        yield return "letterOnStageEntry's letter def is empty";
      }
      if (letterOnStageEntry.letterLabel.NullOrEmpty())
      {
        yield return "letterOnStageEntry's label is empty";
      }
      if (letterOnStageEntry.letterDesc.NullOrEmpty())
      {
        yield return "letterOnStageEntry's desc is empty";
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
    public class CollapseNotification
    {
      public LetterDef letterDef;
      public string letterLabel;
      public string letterDesc;

      public MessageTypeDef messageTypeDef;
      public string messageToast;

      public bool IsLetter => letterDef != null;
    }

    public CollapseNotification noCasualties = new();
    public CollapseNotification withCasualties = new();
  }

  public FloatRange daysToCollapse = new FloatRange(3, 3);

  public List<EffectsAtStage> effectsAtStages = new();

  public CollapseLetterOrMessage letterOrMessageOnCollapse = new();

  //log errors on boot
  public IEnumerable<string> ConfigErrors()
  {
    CollapseLetterOrMessage.CollapseNotification[] notifications =
    {
      letterOrMessageOnCollapse.noCasualties,
      letterOrMessageOnCollapse.withCasualties,
    };

    if (daysToCollapse.max < daysToCollapse.min)
    {
      yield return "days to collapse's max value is lesser than its min.";
    }
    if (daysToCollapse.min <= 0)
    {
      yield return "days to collapse's min value must be greater than 0.";
    }

    foreach (CollapseLetterOrMessage.CollapseNotification notification in notifications)
    {
      if (notification.letterDef != null && notification.messageTypeDef != null)
      {
        yield return "collapse notification has both letterDef and messageTypeDef, they are mutually exclusive";
      }
      if (notification.letterDef == null && notification.messageTypeDef == null)
      {
        yield return "collapse notification has neither letterDef nor messageTypeDef";
      }
    }

    float prevExitPercentage = -1;
    bool hasMax1 = false;
    bool hasMin0 = false;
    for (int i = 0; i < effectsAtStages.Count; i++)
    {
      EffectsAtStage stage = effectsAtStages[i];
      foreach (string err in stage.ConfigErrors())
      {
        yield return "stage " + (i + 1) + ": " + err;
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
        yield return "stages  " + (i + 1) + "and " + i + " have overlap between the former's exit and the latter's entry percentages.";
        continue;
      }

      //this is to make things like stage 1: 0% - 10% stage 2: 10%-20% work nicely while not sharing a tick
      if (stage.startAtCollapsePercentage == prevExitPercentage)
      {
        stage.startAtCollapsePercentage += 0.01f;
      }

      if ((stage.startAtCollapsePercentage - prevExitPercentage) > 1f)
      {
        yield return "stages  " + (i + 1) + "and " + i + " have a gap between them.";
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
