using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveEntrance : MapPortal
{
    /// Vanilla picks one hardcoded generator at a fixed square size. We instead
    /// roll a shape (a MapGeneratorDef carrying CaveShape) allowed by our biome,
    /// and take the map dimensions from it.
    protected override Map GeneratePocketMapInt()
    {
        if (def.portal is not CavePortalProperties cavePortal || cavePortal.pocketMapBiomeDef == null)
        {
            Log.Error(
                "CF config error: "
                    + def.defName
                    + " needs <portal Class=\"CavesFramework.CavePortalProperties\"> with a biomeDef."
            );
            return null;
        }
        CaveBiome pocketMapBiome = cavePortal.pocketMapBiomeDef.GetModExtension<CaveBiome>();
        if (pocketMapBiome == null)
        {
            Log.Error(
                "CF config error: biome "
                    + cavePortal.pocketMapBiomeDef.defName
                    + " lacks the CaveBiome modExtension."
            );
            return null;
        }
        List<MapGeneratorDef> allowedCaveShapes;
        if (
            pocketMapBiome.blacklistedCaveShapes != null
            && pocketMapBiome.whitelistedCaveShapes != null
        )
        {
            Log.Error(
                "CE config error: defined cave entrance both whitelisted and blacklisted cave shapes."
            );
            return null;
        }
        else if (pocketMapBiome.whitelistedCaveShapes != null)
        {
            //copy, so we never hand out (or later mutate) the def's own list
            allowedCaveShapes = pocketMapBiome.whitelistedCaveShapes.FindAll(d =>
                d.HasModExtension<CaveShape>()
            );
        }
        else
        {
            //blacklist null too == everything allowed
            allowedCaveShapes = DefDatabase<MapGeneratorDef>.AllDefsListForReading.FindAll(d =>
                d.HasModExtension<CaveShape>()
                && (
                    pocketMapBiome.blacklistedCaveShapes == null
                    || !pocketMapBiome.blacklistedCaveShapes.Contains(d)
                )
            );
        }

        if (
            !allowedCaveShapes.TryRandomElementByWeight(
                d => d.GetModExtension<CaveShape>().selectionWeight,
                out MapGeneratorDef chosenCaveShape
            )
        )
        {
            Log.Error("CF: no valid cave shape for biome " + cavePortal.pocketMapBiomeDef.defName);
            return null;
        }

        CaveShape caveShapeParams = chosenCaveShape.GetModExtension<CaveShape>();
        if (caveShapeParams == null)
        {
            Log.Error("CE config error: defined cave shape def lacking CaveShape modExtension.");
            return null;
        }

        int mapHeight;
        int mapWidth;
        if (caveShapeParams.randomizeHeightAndWidth && Rand.Bool)
        {
            mapHeight = caveShapeParams.mapWidth;
            mapWidth = caveShapeParams.mapHeight;
        }
        else
        {
            mapHeight = caveShapeParams.mapHeight;
            mapWidth = caveShapeParams.mapWidth;
        }

        return PocketMapUtility.GeneratePocketMap(
            new IntVec3(mapWidth, 1, mapHeight),
            chosenCaveShape,
            GetExtraGenSteps(),
            base.Map
        );
    }
}
