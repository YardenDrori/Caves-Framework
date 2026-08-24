using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveInfo : CustomMapComponent
{
  public CaveShapeDef caveShapeDef;
  public BiomeDef biomeDef;
  public CaveDef caveDef;
  public List<TileMutatorDef> mutators;
  public List<ThingDef> rockDefs;
  public CaveEntrance portalIntoCave;

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Defs.Look(ref caveShapeDef, "CF_CaveShapeDef");
    Scribe_Defs.Look(ref biomeDef, "CF_BiomeDef");
    Scribe_Defs.Look(ref caveDef, "CF_CaveDef");
    Scribe_Collections.Look(ref mutators, "CF_Mutators", LookMode.Def);
    Scribe_References.Look(ref portalIntoCave, "portalIntoCave");

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      //fix generatorDef because the defName which vanilla uses to save is a template
      //read CaveMapUtility.cs for more info
      MapGeneratorDef fixedGeneratorDef = CaveMapUtility.BuildMapGeneratorDefFromParts(caveDef, caveShapeDef, biomeDef, mutators);
      if (fixedGeneratorDef == null)
      {
        Log.Warning("CF: failed to retrieve cavern details. Falling back to default values.");
        Log.Message(map.generatorDef.pocketMapProperties.biome);
        return;
      }
      if (portalIntoCave == null)
      {
        Log.Error("CF: Failed to retrieve portal into cave.");
      }

      base.map.generatorDef = fixedGeneratorDef;
    }
  }

  public CaveInfo(Map map)
    : base(map) { }
}
