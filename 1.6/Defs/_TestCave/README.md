# _TestCave — throwaway test content

Everything needed to actually walk into a cave, in one folder. Delete the whole
folder to remove it; nothing in `Source/` or the other Defs folders points at it,
with one exception noted below.

## What's here

- `GenSteps.xml` — a `GenStepDef` wrapper per framework `GenStep` class, with the
  `order` values that make the pipeline run in the right sequence.
- `Cave.xml` — `CFTest_Biome_Cavern` (BiomeDef), `CFTest_Shape_Cavern`
  (CaveShapeDef: map size + genStep list), `CF_Cave_Test` (CaveDef: the shape and
  biome pools an entrance rolls from).
- `Entrance.xml` — `CFTest_CaveEntrance`, buildable in Architect > Misc for 1 steel.

## How to test

1. Build a "test cave entrance" from Architect > Misc (or dev mode
   "Try place direct thing").
2. Select it, tell a pawn to enter. The pocket map generates on first entry.
3. Useful while looking: dev mode > "View map gen float grids" shows the `Caves`
   and `DistanceFromExit` grids.

## The one dangling reference

`Defs/Buildings/Building_Misc.xml` already ships `CF_CaveEntranceExample`, which
points at `<cave>CF_Cave_Test</cave>`. That CaveDef is defined here, so deleting
this folder leaves that example def with an unresolved cross-reference (it is
already unresolved on `main` today). Either delete the example def too, or move
the `CF_Cave_Test` CaveDef out of this folder.

## Pipeline order

| order | step | what it does |
|-------|------|--------------|
| 10 | `ElevationFertility` (vanilla) | elevation = 1 everywhere (underground), fertility noise |
| 40 | `CFTest_DigRandomly` | random ~54% of cells set to open space |
| 50 | `CFTest_SetMapEdgesToRock` | border value, which the automata skips and tunnels can't pierce |
| 60 | `CFTest_CellularAutomata` | 4-5 smoothing rule, noise -> caverns |
| 70 | `CFTest_CloseIsolatedPockets` | fills pockets under 40 cells |
| 80 | `CFTest_EnsureCavernRegionsConnection` | tunnels the survivors together |
| 200 | `CFTest_CaveRocksFromGrid` | spawns rock + thick roof from the grid |
| 210 | `CFTest_CaveTerrain` | floor terrain (`useBiomeTerrain false` -> rock floors) |
| 400 | `CFTest_CaveExit` | CaveExit + player start, builds `DistanceFromExit` |
| 410 | `CFTest_ScatterOreCavern` | ore, weighted richer the deeper you go |
| 970 | `RockChunks` (vanilla) | chunk dressing |
| 1500 | `Fog` (vanilla) | fog of war |

`GenStep_CaveDecorScatterer` is abstract with no concrete subclass yet, so it
isn't in the pipeline.
