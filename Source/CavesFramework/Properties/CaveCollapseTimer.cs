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

  public class CaveInParams
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

  public FloatRange startAtCollapsePercentage = new FloatRange(0.0f, 0.0f);
  public FloatRange endAtCollapsePercentage = new FloatRange(1f, 1f);

  // rockfall enabled by default set <caveInParams IsNull="True" /> to disable
  public CaveInParams caveInParams = new();

  public LetterOnStageEntry letterOnStageEntry;

  public List<MapComponentsOnStageEntry> mapComponentsToAdd = new();

  public IEnumerable<string> ConfigErrors()
  {
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

    if (startAtCollapsePercentage.max < startAtCollapsePercentage.min)
    {
      yield return "startAtCollapsePercentage's max value is lesser than its min.";
    }
    if (startAtCollapsePercentage.max > 1f)
    {
      yield return "startAtCollapsePercentage has precentages values greater than 1 the range is 0~1";
    }
    if (startAtCollapsePercentage.min < 0f)
    {
      yield return "startAtCollapsePercentage has precentages values lesser than 0 the range is 0~1";
    }

    if (endAtCollapsePercentage.max < endAtCollapsePercentage.min)
    {
      yield return "endAtCollapsePercentage's max value is lesser than its min.";
    }
    if (endAtCollapsePercentage.max > 1f)
    {
      yield return "endAtCollapsePercentage has precentages values greater 1 the range is 0~1";
    }
    if (endAtCollapsePercentage.min < 0f)
    {
      yield return "endAtCollapsePercentage has precentages values lesser 0 the range is 0~1";
    }

    if (startAtCollapsePercentage.max > endAtCollapsePercentage.min)
    {
      yield return "startAtCollapsePercentage's max and endAtCollapsePercentage's min value intersect.";
    }

    if (caveInParams != null)
    {
      if (caveInParams.countPerTrigger.max < caveInParams.countPerTrigger.min)
      {
        yield return "caveInParams.countPerTrigger's max value is lesser than its min.";
      }
      if (caveInParams.countPerTrigger.min < 1)
      {
        yield return "caveInParams.countPerTrigger's min value is lesser than 1.";
      }

      if (caveInParams.distanceFromWalls.max < caveInParams.distanceFromWalls.min)
      {
        yield return "caveInParams.distanceFromWalls's max value is lesser than its min.";
      }
      if (caveInParams.distanceFromWalls.min < 0)
      {
        yield return "caveInParams.distanceFromWalls's min value cannot be negative.";
      }

      if (caveInParams.maxAirCellsToFillFraction <= 0)
      {
        yield return "caveInParams.maxAirCellsToFillFraction's value is lesser than 0, the range is 0~1.";
      }
      if (caveInParams.maxAirCellsToFillFraction > 1)
      {
        yield return "caveInParams.maxAirCellsToFillFraction's value is greater than 1, the range is 0~1.";
      }

      if (caveInParams.minCellDistanceFromExit < 0)
      {
        yield return "caveInParams.minCellDistanceFromExit's value cannot be negative.";
      }
      if (caveInParams.mtbHoursPerTrigger <= 0)
      {
        yield return "caveInParams.mtbHoursPerTrigger's value must be greater than 0.";
      }
      if (caveInParams.naturalRockWeight < 0)
      {
        yield return "caveInParams.naturalRockWeight's value cannot be negative.";
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

    for (int i = 0; i < effectsAtStages.Count; i++)
    {
      EffectsAtStage stage = effectsAtStages[i];
      foreach (string err in stage.ConfigErrors())
      {
        yield return "stage " + (i + 1) + ": " + err;
      }
    }
  }
}
