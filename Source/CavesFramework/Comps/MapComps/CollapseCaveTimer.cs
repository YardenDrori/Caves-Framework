using Verse;

namespace CavesFramework;

public class CompCollapseCaveTimer : CustomMapComponent
{
  public CompCollapseCaveTimer(Map map)
    : base(map) { }

  public override void FinalizeInit()
  {
    base.FinalizeInit();
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    if (!map.IsHashIntervalTick(250, 0))
    {
      return;
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
  }
}
