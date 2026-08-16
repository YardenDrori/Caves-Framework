namespace CavesFramework;

public static class CaveGridConstants
{
  //solid
  public const float border = -30f;
  public const float rock = -1f;

  //empty
  public const float emptySpace = 1f;

  public static bool IsAnyRock(float caveGridVal)
  {
    return caveGridVal <= 0;
  }

  public static bool IsWorkableRock(float caveGridVal)
  {
    return IsAnyRock(caveGridVal) && caveGridVal != border;
  }
}
