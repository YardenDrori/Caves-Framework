using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveShapeEntry
{
    public MapGeneratorDef shape;
    public float shapeWeight;
    public List<GenStepOverride> genStepOverrides = new();
}

public class CaveBiomeExt : DefModExtension
{
    public List<CaveShapeEntry> caveShapes = new();

    public List<TileMutatorDef> tileMutators = new();

    public List<GenStepOverride> genStepOverrides = new();

    public float biomeWeight = 1;
}
