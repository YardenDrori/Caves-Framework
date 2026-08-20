using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public abstract class GenStep_CaveDecorScatterer : GenStep_Scatterer
{
  public override int SeedPart => 855333082;

  //==========How many==========
  // public int count = -1;
  // public FloatRange countPer10kCellsRange = FloatRange.Zero;
  public IntRange? countRange;
  public RatioConfigCellPicker commonalityRatePerCellFromExit = new();
  public int minSuccessCountBeforeAbandon = 0;

  //==========Where==========
  // public bool nearPlayerStart;
  // public bool nearMapCenter;

  //==========LocationRules==========
  // public float minSpacing = 10f;
  // public bool spotMustBeStandable;
  // public int minDistToPlayerStart;
  // public float minDistToPlayerStartPct;
  // public int minEdgeDist;
  // public float minEdgeDistPct;
  // public int extraNoBuildEdgeDist;
  // public List<ScattererValidator> validators = new List<ScattererValidator>();
  // public List<ScattererValidator> fallbackValidators = new List<ScattererValidator>();
  // public bool allowFoggedPositions = true;
  // public bool allowRoofed = true;
  public float maxSpacing = float.MaxValue;
  public int maxEdgeDist;
  public bool mustBeBuriedInRock = false;
  public bool mustBeExposedToAir = false; //guarantees at least one cell, not all cells
  public List<TerrainDef> allowedTerrain = new();
  public List<TerrainDef> forbiddenTerrain = new();
  public float minDistFromWall = 0f;
  public float maxDistFromWall = float.MaxValue;
  public List<IntVec3> relativeDirectionsFromNearestWallAllowed = new();
  public bool canBlockPathways = false;

  //==========MapRules==========
  // public bool allowInWaterBiome = true;
  // public bool onlyOnStartingMap;
  // public float minPollution;

  //==========InfoOnWhatsScattered==========
  // public bool isJunk;

  //==========Misc==========
  // public bool warnOnFail = true;

  //==========FieldsForChildren==========
  public RatioConfig ratioFromExitDist = new();

  //==========IHonestlyDon'tKnowOrCare
  // public bool allowMechanoidDatacoreReadOrLost = true;

  private HashSet<IntVec3> ValidCellCache;
}
