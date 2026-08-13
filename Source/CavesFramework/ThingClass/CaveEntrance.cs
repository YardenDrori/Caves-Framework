using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveEntrance : MapPortal
{
    public CaveDef cave = null;

    /// Vanilla picks one hardcoded generator at a fixed square size. We instead
    /// roll a shape (a MapGeneratorDef carrying CaveShape) allowed by our biome,
    /// and take the map dimensions from it.
    protected override Map GeneratePocketMapInt()
    {
        if (cave == null)
        {
            Log.Error("CF: " + def.defName + "'s cave property not filled.");
            return null;
        }

        if (ChooseBiome() is not BiomeDef caveBiomeDef)
        {
            Log.Error("CF: no valid biome for cave " + cave.defName);
            return null;
        }
        var (caveShapeDef, caveShapeOverrides) = ChooseCaveShape();
        if (caveShapeDef == null)
        {
            Log.Error("CF: no valid cave shape for biome " + caveBiomeDef.defName);
            return null;
        }
        //having 0 mutators is valid thus no null check
        List<TileMutatorDef> mutatorsToAdd = ChooseMutators();

        int mapHeight;
        int mapWidth;
        if (caveShapeDef.randomizeHeightAndWidth && Rand.Bool)
        {
            mapHeight = caveShapeDef.width;
            mapWidth = caveShapeDef.height;
        }
        else
        {
            mapHeight = caveShapeDef.height;
            mapWidth = caveShapeDef.width;
        }
        // csharpier-ignore
        return CaveMapUtility.GenerateCave(
            new IntVec3(mapWidth, 1, mapHeight),
            GetExtraGenSteps(),
            base.Map,
            caveBiomeDef,
            caveShapeDef,
            cave,
            caveShapeOverrides,
            mutatorsToAdd
        );
    }

    private BiomeDef ChooseBiome()
    {
        if (!cave.biomes.TryRandomElementByWeight(d => d.biomeWeight, out CaveBiomeEntry biome))
        {
            return null;
        }
        return biome.biome;
    }

    private (CaveShapeDef, List<GenStepOverride>) ChooseCaveShape()
    {
        if (!cave.shapes.TryRandomElementByWeight(d => d.shapeWeight, out CaveShapeEntry shape))
        {
            return (null, null);
        }
        return (shape.shape, shape.genStepsOverrides);
    }

#pragma warning disable IDE0305
#pragma warning disable IDE0028
    private List<TileMutatorDef> ChooseMutators()
    {
        List<TileMutatorDef> mutators = new();
        mutators.AddRange(cave.mutators);
        if (cave.optionalMutators != null)
        {
            float chanceForMut = cave.optionalMutators.chanceForOptionalMutators;
            int mutatorsToAddCount = Math.Min(cave.optionalMutators.maxOptionalMutatorsActive, cave.optionalMutators.optionalMutators.Count);
            List<OptionalMutatorEntry> remainingMutators = cave.optionalMutators.optionalMutators.ToList();

            for (int i = 0; i < mutatorsToAddCount; i++)
            {
                if (!Rand.Chance(chanceForMut))
                {
                    break;
                }
                if (!remainingMutators.TryRandomElementByWeight(d => d.mutatorWeight, out OptionalMutatorEntry chosenMutator))
                {
                    break;
                }
                if (chosenMutator.mutator == null)
                {
                    Log.Error("CF: defined optional mutator in CaveDef " + cave.defName + " with no TileMutatorDef.");
                    continue;
                }
                chanceForMut *= cave.optionalMutators.additionalMutatorChanceMult;
                remainingMutators.Remove(chosenMutator);
                mutators.Add(chosenMutator.mutator);
            }
        }
        return mutators;
    }
}
