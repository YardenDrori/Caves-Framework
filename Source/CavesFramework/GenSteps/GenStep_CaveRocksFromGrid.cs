using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Noise;

namespace CavesFramework;

public class GenStep_CaveRocksFromGrid : GenStep_RocksFromGrid
{
  public List<ThingDef> rockDefsForCave;

  public override void Generate(Map map, GenStepParams parms)
  {
    if (rockDefsForCave.NullOrEmpty())
    {
      base.Generate(map, parms);
      return;
    }

    //we call reset to remove all the vanilla noise maps for the tile's rocks
    RockNoises.Reset();

    //we have to basically rewrite the entire method but one line modified
    //to use our defs over the tile's ones idk if this is better than a transpiler
    //cause both are equally as sensitive to changes in the original code
    RockNoises.rockNoises = new List<RockNoises.RockNoise>();
    foreach (ThingDef item in rockDefsForCave)
    {
      RockNoises.RockNoise rockNoise = new RockNoises.RockNoise();
      rockNoise.rockDef = item;
      rockNoise.noise = new Perlin(0.004999999888241291, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.Medium);
      RockNoises.rockNoises.Add(rockNoise);
      NoiseDebugUI.StoreNoiseRender(rockNoise.noise, rockNoise.rockDef?.ToString() + " score", map.Size.ToIntVec2);
    }

    base.Generate(map, parms);
  }
}
