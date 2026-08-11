using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Noise;

namespace CavesFramework;

public class CaveEntrance : MapPortal
{
    private List<CaveShapeEntry> GetAllowedCaveShapesForBiome(
        CaveBiomeExt pocketMapBiome,
        CavePortalProperties cavePortal
    )
    {
        if (pocketMapBiome.caveShapes.NullOrEmpty())
        {
            Log.Error("CF config error: defined cave entrance with no cave shapes.");
            return null;
        }
        else
        {
            //copy, so we never hand out (or later mutate) the def's own list
            return pocketMapBiome.caveShapes.FindAll(d =>
            {
                if (d.shape == null)
                {
                    Log.Error(
                        "CF config error: biome "
                            + cavePortal.pocketMapBiomeDef.defName
                            + " lists a caveShapes entry with a missing or unresolved <shape>."
                    );
                    return false;
                }
                if (!d.shape.HasModExtension<CaveShape>())
                {
                    Log.Error(
                        "CF config error: biome "
                            + cavePortal.pocketMapBiomeDef.defName
                            + " lists cave shape "
                            + d.shape.defName
                            + ", which lacks the CaveShape modExtension."
                    );
                    return false;
                }
                return true;
            });
        }
    }

    private List<TileMutatorDef> GetActiveMutatorsForBiome(
        CaveBiomeExt biomeExt,
        CavePortalProperties cavePortal
    )
    {
        List<TileMutatorDef> mutators = new();
        if (!biomeExt.tileMutators.NullOrEmpty())
        {
            foreach (TileMutatorDef mutator in biomeExt.tileMutators)
            {
                if (mutator == null)
                {
                    Log.Error(
                        "CF config error: biome "
                            + cavePortal.pocketMapBiomeDef.defName
                            + " lists a tileMutators entry that is missing or unresolved."
                    );
                    continue;
                }
                mutators.Add(mutator);
            }
        }
        if (
            biomeExt.optionalMutators.chanceForOptionalMutators > 0f
            && biomeExt.optionalMutators.maxOptionalMutatorsActive > 0
            && !biomeExt.optionalMutators.optionalMutators.NullOrEmpty()
        )
        {
            int optMutatorCount = biomeExt.optionalMutators.maxOptionalMutatorsActive;
            if (
                biomeExt.optionalMutators.maxOptionalMutatorsActive
                > biomeExt.optionalMutators.optionalMutators.Count
            )
            {
                optMutatorCount = biomeExt.optionalMutators.optionalMutators.Count;
            }
            List<OptionalMutatorEntry> remainingMutators = new();
            biomeExt.optionalMutators.optionalMutators.CopyToList(remainingMutators);
            float currChance = biomeExt.optionalMutators.chanceForOptionalMutators;
            for (int i = 0; i < optMutatorCount; i++)
            {
                if (Rand.Chance(currChance))
                {
                    if (
                        !remainingMutators.TryRandomElementByWeight(
                            d => d.mutatorWeight,
                            out OptionalMutatorEntry chosenMutator
                        )
                    )
                    {
                        Log.Error(
                            "CF config error: optional mutators for biome "
                                + cavePortal.pocketMapBiomeDef.defName
                                + " don't have weights assigned correctly."
                        );
                        break;
                    }
                    remainingMutators.Remove(chosenMutator);
                    if (chosenMutator.mutator == null)
                    {
                        Log.Error(
                            "CF config error: biome "
                                + cavePortal.pocketMapBiomeDef.defName
                                + " lists an optionalMutators entry with a missing or unresolved <mutator>."
                        );
                        continue;
                    }
                    mutators.Add(chosenMutator.mutator);
                    currChance *= biomeExt.optionalMutators.additionalMutatorChanceMult;
                }
                else
                {
                    break;
                }
            }
        }
        return mutators;
    }

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

        if (
            GetAllowedCaveShapesForBiome(pocketMapBiome, cavePortal)
            is not List<CaveShapeEntry> allowedCaveShapes
        )
        {
            return null;
        }
        if (
            GetActiveMutatorsForBiome(pocketMapBiome, cavePortal)
            is not List<TileMutatorDef> mutatorsToAdd
        )
        {
            return null;
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
            chosenCaveShape.genStepOverrides,
            mutatorsToAdd
        );
    }
}
