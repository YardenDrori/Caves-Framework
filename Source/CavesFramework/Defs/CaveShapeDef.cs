using System;
using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public class CaveShapeDef : Def
{
    public List<GenStepDef> genSteps = new();

    public List<Type> customMapComponents = new();

    public int width = 100;
    public int height = 100;
    public bool randomizeHeightAndWidth = true;
}
