using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveEntrance : MapPortal
{
  private CaveDef Cave => (def.portal as CavePortalProperties)?.cave;

  /// Vanilla picks one hardcoded generator at a fixed square size. We instead
  /// roll a shape (a MapGeneratorDef carrying CaveShape) allowed by our biome,
  /// and take the map dimensions from it.
  protected override Map GeneratePocketMapInt()
  {
    if (Cave == null)
    {
      Log.Error("CF: " + def.defName + "'s cave property not filled.");
      return null;
    }

    if (ChooseBiome() is not BiomeDef caveBiomeDef)
    {
      Log.Error("CF: no valid biome for cave " + Cave.defName);
      return null;
    }

    if (ChooseCaveShape() is not CaveShapeDef caveShapeDef)
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
            Cave,
            mutatorsToAdd
        );
  }

  private BiomeDef ChooseBiome()
  {
    if (!Cave.biomes.TryRandomElementByWeight(d => d.biomeWeight, out CaveBiomeEntry biome))
    {
      return null;
    }
    return biome.biome;
  }

  private CaveShapeDef ChooseCaveShape()
  {
    if (!Cave.shapes.TryRandomElementByWeight(d => d.shapeWeight, out CaveShapeEntry shape))
    {
      return null;
    }
    return shape.shape;
  }

#pragma warning disable IDE0305
  private List<TileMutatorDef> ChooseMutators()
  {
    List<TileMutatorDef> mutators = new();
    mutators.AddRange(Cave.mutators);
    if (Cave.optionalMutators != null)
    {
      float chanceForMut = Cave.optionalMutators.chanceForOptionalMutators;
      int mutatorsToAddCount = Math.Min(Cave.optionalMutators.maxOptionalMutatorsActive, Cave.optionalMutators.optionalMutators.Count);
      List<OptionalMutatorEntry> remainingMutators = Cave.optionalMutators.optionalMutators.ToList();

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
          Log.Error("CF: defined optional mutator in CaveDef " + Cave.defName + " with no TileMutatorDef.");
          continue;
        }
        chanceForMut *= Cave.optionalMutators.additionalMutatorChanceMult;
        remainingMutators.Remove(chosenMutator);
        mutators.Add(chosenMutator.mutator);
      }
    }
    return mutators;
  }
}
