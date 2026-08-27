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
    public SimpleCurve intervalCurve = new SimpleCurve { { 0f, 12f }, { 1f, 2f } };
    public IntRange countPerTrigger = new IntRange(1, 3);

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

  // rockfall enabled by default set <caveInParams IsNull="True" /> to disable
  public CaveInParams caveInParams = new();

  public LetterOnStageEntry letterOnStageEntry;

  public List<MapComponentsOnStageEntry> mapComponentsToAdd = new();

  //TODO
  public IEnumerable<string> ConfigErrors()
  {
    yield break;
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
