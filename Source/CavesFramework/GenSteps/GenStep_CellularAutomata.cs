using Verse;

namespace CavesFramework;

public class GenStep_CellularAutomata : GenStep
{
  public IntRange iterations = new IntRange(4, 5);
  public IntRange neighborsForEmptyCellToBeWall = new IntRange(5, 5);
  public IntRange neighborsForFullCellToBeStay = new IntRange(4, 4);
  public bool rerandomizePerIteration = false;

  public override int SeedPart => 146047867;

  public override void Generate(Map map, GenStepParams parms)
  {
    int iterationsI = iterations.RandomInRange;
    int neighborsForEmptyCellToBeWallI = neighborsForEmptyCellToBeWall.RandomInRange;
    int neighborsForFullCellToBeStayI = neighborsForFullCellToBeStay.RandomInRange;
    for (int i = 0; i < iterationsI; i++)
    {
      if (rerandomizePerIteration && i != 0)
      {
        neighborsForEmptyCellToBeWallI = neighborsForEmptyCellToBeWall.RandomInRange;
        neighborsForFullCellToBeStayI = neighborsForFullCellToBeStay.RandomInRange;
      }

      //copy by value
      using MapGenFloatGrid caveGridSnapshot = CaveGridUtility.CloneGrid(MapGenerator.Caves, map);
      MapGenFloatGrid caveGridLive = MapGenerator.Caves;

      foreach (IntVec3 allcell in map.AllCells)
      {
        if (CaveGridUtility.border == caveGridSnapshot[allcell])
        {
          continue;
        }

        //we count cells that are a wall as a neighbor
        int neighbors = CaveGridUtility.NeighborCount(allcell, map, countOutOfBoundsCells: true, c => CaveGridUtility.IsAnyRock(caveGridSnapshot[c]));

        if (CaveGridUtility.IsAnyRock(caveGridSnapshot[allcell]))
        {
          if (neighbors < neighborsForFullCellToBeStayI)
          {
            caveGridLive[allcell] = CaveGridUtility.emptySpace;
          }
        }
        else if (neighbors >= neighborsForEmptyCellToBeWallI)
        {
          caveGridLive[allcell] = CaveGridUtility.rock;
        }
      }
    }
  }
}
