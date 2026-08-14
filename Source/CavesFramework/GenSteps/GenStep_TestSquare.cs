using Verse;

namespace CavesFramework;

public class GenStep_TestSquare : GenStep
{
    public override int SeedPart => 6980085;

    private int width = 15;
    private int height = 30;

    public override void Generate(Map map, GenStepParams parms)
    {
        //check for any overrides later first i wanna get a static genStep working

        IntVec3 center = map.Center;
        for (int i = center.x - width / 2; i < center.x + width / 2; i++)
        {
            for (int j = center.z - height / 2; j < center.z + height / 2; j++)
            {
                //deelte a tile somehow
            }
        }
    }
}
