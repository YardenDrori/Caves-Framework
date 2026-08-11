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
        if (
            def.portal is not CavePortalProperties cavePortal
            || cavePortal.pocketMapBiomeDef == null
        )
        {
            Log.Error(
                "CF config error: "
                    + def.defName
                    + " needs <portal Class=\"CavesFramework.CavePortalProperties\"> with a biomeDef."
            );
            return null;
        }
        CaveBiomeExt pocketMapBiome = cavePortal.pocketMapBiomeDef.GetModExtension<CaveBiomeExt>();
        if (pocketMapBiome == null)
        {
            Log.Error(
                "CF config error: biome "
                    + cavePortal.pocketMapBiomeDef.defName
                    + " lacks the CaveBiome modExtension."
            );
            return null;
        }
        List<CaveShapeEntry> allowedCaveShapes;
        if (pocketMapBiome.caveShapes.NullOrEmpty())
        {
            Log.Error("CF config error: defined cave entrance with no cave shapes.");
            return null;
        }
        else
        {
            //copy, so we never hand out (or later mutate) the def's own list
            allowedCaveShapes = pocketMapBiome.caveShapes.FindAll(d =>
            {
                if (d.shape.HasModExtension<CaveShape>())
                {
                    return true;
                }
                else
                {
                    Log.Error("CF config error: defined cave shape with no CaveShape modExtension");
                    return false;
                }
            });
        }

        if (
            !allowedCaveShapes.TryRandomElementByWeight(
                d => d.shapeWeight,
                out CaveShapeEntry chosenCaveShape
            )
        )
        {
            Log.Error("CF: no valid cave shape for biome " + cavePortal.pocketMapBiomeDef.defName);
            return null;
        }
        CaveShape caveShapeParams = chosenCaveShape.shape.GetModExtension<CaveShape>();

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

        return CaveMapUtility.GeneratePocketMap(
            new IntVec3(mapWidth, 1, mapHeight),
            chosenCaveShape.shape,
            GetExtraGenSteps(),
            base.Map,
            cavePortal.pocketMapBiomeDef,
            chosenCaveShape.genStepOverrides
        );
    }
}
