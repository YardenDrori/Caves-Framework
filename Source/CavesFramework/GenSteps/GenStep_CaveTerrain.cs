using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CavesFramework;

public class GenStep_CaveTerrain : GenStep
{
  public override int SeedPart => 784425029;

  public bool useBiomeTerrain = true;

  public override void Generate(Map map, GenStepParams parms)
  {
    TerrainGrid terrainGrid = map.terrainGrid;
    List<IntVec3> list = new List<IntVec3>();
    using (map.pathing.DisableIncrementalScope())
    {
      foreach (IntVec3 allCell in map.AllCells)
      {
        Building edifice = allCell.GetEdifice(map);
        TerrainDef naturalTerrainAt = GetNaturalTerrainAtHome(allCell, map);
        if (!naturalTerrainAt.supportsRock && edifice != null)
        {
          list.Add(edifice.Position);
          edifice.Destroy();
        }
        terrainGrid.SetTerrain(allCell, naturalTerrainAt);
      }
      RoofCollapseCellsFinder.RemoveBulkCollapsingRoofs(list, map);
      foreach (TerrainPatchMaker terrainPatchMaker in map.Biome.terrainPatchMakers)
      {
        terrainPatchMaker.Cleanup();
      }
    }
  }

  // Mom! can I have a MapGenerator.GetNaturalTerrain???
  // No. We have GetNaturalTerrain at home.
  // GetNaturalTerrain at home:
  private TerrainDef GetNaturalTerrainAtHome(IntVec3 cell, Map map)
  {
    float elevation = useBiomeTerrain ? 0f : 1f;
    MapGenFloatGrid fertility = MapGenerator.Fertility;
    return MapGenUtility.TerrainFrom(cell, map, elevation, fertility[cell], preferRock: false);
  }
}
