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
            Log.Message("[Caves Framework] initialized.");
        }
    }
}
