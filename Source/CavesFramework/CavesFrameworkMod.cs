using HarmonyLib;
using Verse;

namespace CavesFramework
{
    [StaticConstructorOnStartup]
    public static class CavesFrameworkMod
    {
        public const string HarmonyId = "blacksparrow.cavesframework";

        static CavesFrameworkMod()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll();
            FillLabelOnlyGenerators();
            Log.Message("[Caves Framework] initialized.");
        }

        /// <summary>
        /// MapPortal hardcodes def.portal.pocketMapGenerator.label into the "View X"
        /// gizmo and PocketMapExit's "Exit X" string, with no virtual seam to override,
        /// and NREs outright if the field is null. We never generate from it - the shape
        /// is rolled per-cave - so we quietly point every cave entrance at a label-only
        /// def here rather than making def authors write a field that does nothing.
        /// </summary>
        private static void FillLabelOnlyGenerators()
        {
            MapGeneratorDef labelOnly = DefDatabase<MapGeneratorDef>.GetNamedSilentFail(
                "LabelAndDescFakeMapGen"
            );
            if (labelOnly == null)
            {
                Log.Error("[Caves Framework] missing LabelAndDescFakeMapGen.");
                return;
            }
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (
                    thingDef.portal is CavePortalProperties cavePortal
                    && cavePortal.pocketMapGenerator == null
                )
                {
                    cavePortal.pocketMapGenerator = labelOnly;
                }
            }
        }
    }
}
