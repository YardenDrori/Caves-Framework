using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CavesFramework;

//we need to change one line from Verse/PocketMapUtility to allow the biome to come from us
//rather than the MapGeneratorDef
public static class CaveMapUtility
{
  public static Map GenerateCave(
    CaveEntrance CreatorPortal,
    IntVec3 size,
    IEnumerable<GenStepWithParams> extraGenStepDefs,
    Map sourceMap,
    BiomeDef biomeDef,
    CaveShapeDef caveShapeDef,
    CaveDef caveDef,
    List<TileMutatorDef> mutators
  )
  {
    PocketMapParent pocketMapParent = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.PocketMap) as PocketMapParent;
    pocketMapParent.sourceMap = sourceMap;

    MapGeneratorDef fixedGeneratorDef = BuildMapGeneratorDefFromParts(caveDef, caveShapeDef, biomeDef, mutators);
    if (fixedGeneratorDef == null)
    {
      return null;
    }

    Map result = MapGenerator.GenerateMap(
      size,
      pocketMapParent,
      fixedGeneratorDef,
      extraGenStepDefs,
      map =>
      {
        //we tell the comp here the map info so it'll know how to retrieve it on load
        CaveInfo caveInfo = map.GetComponent<CaveInfo>();
        if (caveInfo == null)
        {
          Log.Error("CF: failed to fetch CaveInfo component from newly created cave.");
          return;
        }
        caveInfo.biomeDef = biomeDef;
        caveInfo.caveDef = caveDef;
        caveInfo.caveShapeDef = caveShapeDef;
        caveInfo.mutators = mutators;
        caveInfo.portalIntoCave = CreatorPortal;
      },
      isPocketMap: true
    );
    Find.World.pocketMaps.Add(pocketMapParent);
    return result;
  }

  //We build our own MapGeneratorDef because the vanilla one is limiting the ways we can mix and match
  //genSteps with biomes and mutators so here we use the defs we defined to build and instance the game expects
  //this instance will sit in memory in the map but will be erased upon game reload so we also need a harmony patch
  //to repopulate so that we don't use any data from the template
  public static MapGeneratorDef BuildMapGeneratorDefFromParts(
    CaveDef caveDef,
    CaveShapeDef caveShapeDef,
    BiomeDef biomeDef,
    List<TileMutatorDef> mutators
  )
  {
    if (caveDef == null || caveShapeDef == null || biomeDef == null)
    {
      return null;
    }

    MapGeneratorDef mapGenDefTemplate = DefDatabase<MapGeneratorDef>.GetNamed("CF_CaveMapGenerator", false);
    if (mapGenDefTemplate == null)
    {
      Log.Error("CF: ThingDef CF_CaveMapGenerator not found.");
      return null;
    }
    MapGeneratorDef fixedGeneratorDef = Gen.MemberwiseClone(mapGenDefTemplate);

    //=====Populate the mapGenDef=====
    // coppy the Def info
    fixedGeneratorDef.label = caveDef.label;
    fixedGeneratorDef.description = caveDef.description;
    fixedGeneratorDef.descriptionHyperlinks = caveDef.descriptionHyperlinks;
    fixedGeneratorDef.ignoreConfigErrors = caveDef.ignoreConfigErrors;
    fixedGeneratorDef.ignoreIllegalLabelCharacterConfigError = caveDef.ignoreIllegalLabelCharacterConfigError;
    fixedGeneratorDef.modExtensions = caveDef.modExtensions;

    //we build a sensible pocketMapProperties to feed the MapGeneratorDef
    PocketMapProperties pocketMapProperties = new PocketMapProperties
    {
      biome = biomeDef,
      tileMutators = mutators,
      temperature = biomeDef.constantOutdoorTemperature ?? 15f,
      destroyOnParentMapAbandoned = true,
      preventPrisonerEscape = true,
      canLaunchGravship = false,
      canBeCleaned = false,
    };

    //might wanna allow changing this in the future for more "exotic" caverns for now we keep this
    fixedGeneratorDef.isUnderground = true;
    //this is a dead field in vanilla source code doesn't matter what we put here
    fixedGeneratorDef.forceCaves = false;
    fixedGeneratorDef.genSteps = caveShapeDef.genSteps;
    fixedGeneratorDef.pocketMapProperties = pocketMapProperties;

    List<Type> compsToAdd = new List<Type>();
    compsToAdd.AddRange(fixedGeneratorDef.customMapComponents);
    compsToAdd.AddRange(caveShapeDef.customMapComponents);
    fixedGeneratorDef.customMapComponents = compsToAdd.Distinct().ToList();

    fixedGeneratorDef.ignoreAreaRevealedLetter = true;
    fixedGeneratorDef.disableShadows = caveDef.disableShadows;
    fixedGeneratorDef.disableCallAid = true;

    return fixedGeneratorDef;
  }
}
