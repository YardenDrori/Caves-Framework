using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveShapeEntry
{
    public MapGeneratorDef shape;
    public float shapeWeight = 1f;
    public List<GenStepOverride> genStepOverrides = new();
}

public class OptionalMutatorEntry
{
    public TileMutatorDef mutator;
    public float mutatorWeight = 1f;
}

public class OptionalMutators
{
    public List<OptionalMutatorEntry> optionalMutators = new();

    public int maxOptionalMutatorsActive = 1;
    public float chanceForOptionalMutators = 0f;
    public float additionalMutatorChanceMult = 0.5f;
}

public class CaveBiomeExt : DefModExtension
{
    public List<CaveShapeEntry> caveShapes = new();

    public List<TileMutatorDef> tileMutators = new();
    public OptionalMutators optionalMutators = new();

    public float biomeWeight = 1;
}
