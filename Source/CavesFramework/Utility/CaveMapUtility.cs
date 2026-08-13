using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CavesFramework;

//we need to change one line from Verse/PocketMapUtility to allow the biome to come from us
//rather than the MapGeneratorDef
public static class CaveMapUtility
{
    public static Map GenerateCave(
        IntVec3 size,
        IEnumerable<GenStepWithParams> extraGenStepDefs,
        Map sourceMap,
        BiomeDef biomeDef,
        CaveShapeDef caveShapeDef,
        CaveDef caveDef,
        List<GenStepOverride> overrides,
        List<TileMutatorDef> mutators
    )
    {
        PocketMapParent pocketMapParent = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.PocketMap) as PocketMapParent;
        pocketMapParent.sourceMap = sourceMap;

        //We build our own MapGeneratorDef because the vanilla one is limiting the ways we can mix and match
        //genSteps with biomes and mutators so here we use the defs we defined to build and instance the game expects
        //this instance will sit in memory in the map but will be erased upon game reload so we also need a harmony patch
        //to repopulate so that we don't use any data from the template
        MapGeneratorDef MapGenDefTemplate = DefDatabase<MapGeneratorDef>.GetNamed("CF_CaveMapGenerator", false);
        if (MapGenDefTemplate == null)
        {
            Log.Error("CF: ThingDef CF_CaveMapGenerator not found.");
            return null;
        }
        MapGeneratorDef fixedGeneratorDef = Gen.MemberwiseClone(MapGenDefTemplate);

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
        fixedGeneratorDef.customMapComponents = caveShapeDef.customMapComponents;
        fixedGeneratorDef.ignoreAreaRevealedLetter = true;
        fixedGeneratorDef.disableShadows = caveDef.disableShadows;
        fixedGeneratorDef.disableCallAid = true;

        Map result = MapGenerator.GenerateMap(
            size,
            pocketMapParent,
            fixedGeneratorDef,
            extraGenStepDefs,
            map =>
            {
                //we save the overrides so we can read them from the genStep worker later as gensteps
                //are simpletons we cannot modify them directly without unfavourable behavior
                //we do this here as the data gets cleared beforehand
                MapGenerator.SetVar("genStepOverrides", overrides);
            },
            isPocketMap: true
        );
        Find.World.pocketMaps.Add(pocketMapParent);
        return result;
    }
}
