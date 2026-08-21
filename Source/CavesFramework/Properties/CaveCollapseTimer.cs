using System.Collections.Generic;
using CavesFramework.Comps.MapComps;
using Verse;

public class CaveCollapseTimerProperties : CompProperties
{
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
      public CustomMapComponent mapComponent;
      public bool removeOnStageEnd = true;
      public bool logOnRemovalFailure = false;
    }

    public FloatRange startAtCollapsePercentage = new FloatRange(0.0f, 0.0f);
    public FloatRange? endAtCollapsePercentage = new FloatRange(1f, 1f);

    public FloatRange screenShakeAmount = new FloatRange(0.1f, 1f);

    // x = fraction of the way through THIS stage (0..1), y = seconds between triggers.
    public SimpleCurve screenShakeIntervalCurve = new SimpleCurve { { 0f, 10f }, { 1f, 5f } };

    public List<SoundDef> soundsToPlay = new();
    public SimpleCurve soundsToPlayIntervalCurve; //leave null to sync with screen shake

    public List<EffecterDef> effectsToPlay = new();
    public SimpleCurve effectsToPlayIntervalCurve; //leave null to sync with screen shake

    public LetterOnStageEntry letterOnStageEntry;

    public List<MapComponentsOnStageEntry> mapComponentsToAdd;
  }

  public List<EffectsAtStage> effectsAtStages = new();

  public FloatRange daysToCollapse = new FloatRange(3, 3);

  public CaveCollapseTimerProperties()
  {
    compClass = typeof(CompCollapseCaveTimer);
  }
}
