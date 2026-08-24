using System;
using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public class StageTiming
{
  // x = fraction of the way through this stage (0..1), y = seconds between triggers.
  // leave null to sync witn camera shake
  public SimpleCurve intervalCurve;
  public IntRange countPerTrigger = new IntRange(1, 1);
}

public class EffectsAtStage
{
  public class LetterOnStageEntry
  {
    public LetterDef letterDef;
    public string letterLabel;
    public string letterDesc;
  }

  public class MapComponentsOnStageEntry
  {
    public Type mapComponent;
    public bool removeOnStageEnd = true;
    public bool logOnRemovalFailure = false;
  }

  public class CaveInParams
  {
    public StageTiming timing = new StageTiming
    {
      intervalCurve = new SimpleCurve { { 0f, 12f }, { 1f, 2f } },
      countPerTrigger = new IntRange(1, 3),
    };

    // set to 0 to ignore cave
    public float countScalePer10kAirCells = 1f;

    public float maxAirCellFraction = 0.85f;

    public float naturalRockWeight = 1f;
    public List<ThingOption> additionalRockTypes;

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

  public FloatRange screenShakeAmount = new FloatRange(0.1f, 1f);
  public SimpleCurve screenShakeIntervalCurve = new SimpleCurve { { 0f, 10f }, { 1f, 5f } };

  public List<SoundDef> soundsToPlay = new();
  public StageTiming soundTiming = new(); //leave empty to sync with screen shake
  public List<EffecterDef> effectsToPlay = new();
  public StageTiming effectTiming = new(); // leave empty to sync with screen shake x2

  // rockfall enabled by default set <caveInParams IsNull="True" /> to disable
  public CaveInParams caveInParams = new();

  public LetterOnStageEntry letterOnStageEntry;

  public List<MapComponentsOnStageEntry> mapComponentsToAdd = new();
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
  }
}
