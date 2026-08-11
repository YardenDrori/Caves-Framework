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
        BiomeDef biome,
        List<GenStepOverride> overrides,
        List<TileMutatorDef> mutators
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
                ///we replace the biome cuase in vanilla the biome is defined via MapGeneratorDef
                ///but we replace it with the biome's mod extension but we have to still provide a biome
                ///in mapgeneratordef to avoid an NRE so we use change it to the actual biome as a callback
                map.pocketTileInfo.PrimaryBiome = biome;

                ///We do the same for mutators
                if (mutators != null)
                {
                    foreach (TileMutatorDef i in mutators)
                    {
                        map.TileInfo.AddMutator(i);
                        i.Worker?.Init(map);
                    }
                }

                ///we save the overrides so we can read them from the genStep worker later as gensteps
                ///are simpletons we cannot modify them directly without unfavourable behavior
                ///we do this here as the data gets cleared beforehand
                MapGenerator.SetVar("genStepOverrides", overrides);
            },
            isPocketMap: true
        );
        Find.World.pocketMaps.Add(pocketMapParent);
        return result;
    }
}
