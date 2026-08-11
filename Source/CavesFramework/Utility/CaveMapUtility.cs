using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CavesFramework;

//we need to change one line from Verse/PocketMapUtility to allow the biome to come from us
//rather than the MapGeneratorDef
public static class CaveMapUtility
{
    public static Map GeneratePocketMap(
        IntVec3 size,
        MapGeneratorDef generatorDef,
        IEnumerable<GenStepWithParams> extraGenStepDefs,
        Map sourceMap,
        BiomeDef biome
    // List<TileMutatorDef> mutators
    )
    {
        PocketMapParent pocketMapParent =
            WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.PocketMap) as PocketMapParent;
        pocketMapParent.sourceMap = sourceMap;
        pocketMapParent.mapGenerator = generatorDef;
        Map result = MapGenerator.GenerateMap(
            size,
            pocketMapParent,
            generatorDef,
            extraGenStepDefs,
            map =>
            {
                map.pocketTileInfo.PrimaryBiome = biome;
                //future mutator support capability here
                // if (mutators != null)
                // {
                //     foreach (var m in mutators)
                //     {
                //         map.TileInfo.AddMutator(m);
                //         m.Worker?.Init(map);
                //     }
                // }
            }, //THIS line is the magic
            isPocketMap: true
        );
        Find.World.pocketMaps.Add(pocketMapParent);
        return result;
    }
}
