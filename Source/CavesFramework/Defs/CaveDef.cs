using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class CaveShapeEntry
{
    public CaveShapeDef shape;
    public float shapeWeight = 1f;
}

public class CaveBiomeEntry
{
    public BiomeDef biome;
    public float biomeWeight = 1f;
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

public class CaveDef : Def
{
    public List<CaveShapeEntry> shapes = new();
    public List<CaveBiomeEntry> biomes = new();

    public List<TileMutatorDef> mutators = new();
    public OptionalMutators optionalMutators;

    public bool disableShadows = true;
}
