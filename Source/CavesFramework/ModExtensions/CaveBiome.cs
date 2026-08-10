using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveBiomeExt : DefModExtension
{
    //if both are empty all maps types are allowed
    public List<MapGeneratorDef> whitelistedCaveShapes = null;
    public List<MapGeneratorDef> blacklistedCaveShapes = null;

    public List<TileMutatorDef> tileMutators = new();

    public List<GenStepOverride> genStepOverrides = new();

    public float selectionWeight = 1;
}
