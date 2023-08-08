using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Reflection;

public class InfiniteIslands : IMod
{
    public ModMetadata Metadata => new ModMetadata("Infinite Islands", "Shapez2 Team", "1.0.0");

    public void Init()
    {
        Harmony harmony = new Harmony("InfiniteIslands");
        harmony.PatchAll(typeof(Patch));
        ShapezCallbackExt.OnPostGameStart += OnGameStart;
    }

    private void OnGameStart()
    {
        var propertyInfo = typeof(ResearchChunkLimitManager).GetProperty("CurrentChunkLimit");
        propertyInfo.SetValue(GameCore.G.Research.ChunkLimitManager, Convert.ChangeType(999999, propertyInfo.PropertyType), null);
    }

    private class Patch
    {
        /// <summary>
        /// Patching a method with HarmonyX.
        /// </summary>
        /// <remarks>
        /// This will likely not be supported in future mod versions to account for dependencies and sorting
        /// </remarks>
        [HarmonyPatch(typeof(ResearchChunkLimitManager), "ComputeChunkLimit")]
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix()
        {
            // Do nothing
            return false;
        }
    }

}
